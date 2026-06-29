// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class UnexpectedModalDialogMonitor : IDisposable
    {
        private const int MaxWindowTextLength = 1024;
        private const int IDOK = 1;
        private const int BM_CLICK = 0x00F5;
        private const int GW_OWNER = 4;
        private const int WM_CLOSE = 0x0010;
        private const int WM_COMMAND = 0x0111;

        private readonly IntPtr mainWindow;
        private readonly int processId;
        private readonly string operationStackTrace;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly TaskCompletionSource<Exception> failureSource;
        private readonly Task monitorTask;
        private Exception failure;

        private UnexpectedModalDialogMonitor(IntPtr mainWindow, string operationStackTrace)
            : this(mainWindow, GetProcessId(mainWindow), operationStackTrace)
        {
        }

        private UnexpectedModalDialogMonitor(IntPtr mainWindow, int processId, string operationStackTrace)
        {
            this.mainWindow = mainWindow;
            this.processId = processId;
            this.operationStackTrace = operationStackTrace;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.failureSource = new TaskCompletionSource<Exception>();

            this.monitorTask = Task.Run(() => this.MonitorAsync(this.cancellationTokenSource.Token));
        }

        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        public Task<Exception> FailureTask
        {
            get
            {
                return this.failureSource.Task;
            }
        }

        public static UnexpectedModalDialogMonitor Start(IntPtr mainWindow, string operationStackTrace)
        {
            return new UnexpectedModalDialogMonitor(mainWindow, operationStackTrace);
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
        }

        public void ThrowIfDialogDetected()
        {
            Exception currentFailure = this.failure;
            if (currentFailure != null)
            {
                throw currentFailure;
            }
        }

        private async Task MonitorAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ModalDialogInfo dialog = this.FindUnexpectedModalDialog();
                    if (dialog != null)
                    {
                        bool debuggerAttached = Debugger.IsAttached;
                        if (!debuggerAttached)
                        {
                            DismissDialog(dialog.Handle);
                        }

                        this.SetFailure(
                            new InvalidOperationException(
                                GetDialogDetectedMessage(debuggerAttached) + Environment.NewLine
                                + Environment.NewLine
                                + dialog
                                + Environment.NewLine
                                + "Operation stack trace:" + Environment.NewLine
                                + this.operationStackTrace));

                        return;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.SetFailure(new InvalidOperationException("Unexpected modal dialog monitor failed.", ex));
            }
        }

        private static string GetDialogDetectedMessage(bool debuggerAttached)
        {
            return debuggerAttached
                ? "Unexpected modal dialog detected and left open because the debugger is attached."
                : "Unexpected modal dialog detected and dismissed.";
        }

        private void SetFailure(Exception exception)
        {
            this.failure = exception;
            this.failureSource.TrySetResult(exception);
        }

        private ModalDialogInfo FindUnexpectedModalDialog()
        {
            ModalDialogInfo result = null;
            EnumWindows(
                (window, parameter) =>
                {
                    if (this.IsUnexpectedModalDialog(window))
                    {
                        result = CaptureDialogInfo(window);
                        return false;
                    }

                    return true;
                },
                IntPtr.Zero);

            return result;
        }

        private bool IsUnexpectedModalDialog(IntPtr window)
        {
            if (window == IntPtr.Zero)
            {
                return false;
            }

            if (this.mainWindow != IntPtr.Zero && window == this.mainWindow)
            {
                return false;
            }

            GetWindowThreadProcessId(window, out int windowProcessId);
            if (windowProcessId != this.processId)
            {
                return false;
            }

            if (!IsWindowVisible(window) || !IsWindowEnabled(window))
            {
                return false;
            }

            string className = GetClassName(window);
            IntPtr owner = GetWindow(window, GW_OWNER);
            return string.Equals(className, "#32770", StringComparison.Ordinal)
                || (this.mainWindow != IntPtr.Zero && owner == this.mainWindow);
        }

        private static int GetProcessId(IntPtr window)
        {
            GetWindowThreadProcessId(window, out int processId);
            return processId;
        }

        private static ModalDialogInfo CaptureDialogInfo(IntPtr dialog)
        {
            string title = GetWindowText(dialog);
            string className = GetClassName(dialog);
            var childText = new List<string>();

            EnumChildWindows(
                dialog,
                (child, parameter) =>
                {
                    string text = GetWindowText(child);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        childText.Add(text);
                    }

                    return true;
                },
                IntPtr.Zero);

            return new ModalDialogInfo(dialog, title, className, childText);
        }

        private static void DismissDialog(IntPtr dialog)
        {
            IntPtr okButton = FindButton(dialog, "OK");
            if (okButton != IntPtr.Zero)
            {
                PostMessage(okButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            }

            PostMessage(dialog, WM_COMMAND, new IntPtr(IDOK), IntPtr.Zero);
            PostMessage(dialog, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        private static IntPtr FindButton(IntPtr dialog, string buttonText)
        {
            IntPtr result = IntPtr.Zero;
            EnumChildWindows(
                dialog,
                (child, parameter) =>
                {
                    if (string.Equals(GetClassName(child), "Button", StringComparison.Ordinal)
                        && string.Equals(GetWindowText(child), buttonText, StringComparison.Ordinal))
                    {
                        result = child;
                        return false;
                    }

                    return true;
                },
                IntPtr.Zero);

            return result;
        }

        private static string GetClassName(IntPtr window)
        {
            var builder = new StringBuilder(MaxWindowTextLength);
            GetClassName(window, builder, builder.Capacity);
            return builder.ToString();
        }

        private static string GetWindowText(IntPtr window)
        {
            var builder = new StringBuilder(MaxWindowTextLength);
            GetWindowText(window, builder, builder.Capacity);
            return builder.ToString();
        }

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr window, StringBuilder className, int maxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr window, int command);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        private sealed class ModalDialogInfo
        {
            private readonly IReadOnlyList<string> childText;

            public ModalDialogInfo(IntPtr handle, string title, string className, IReadOnlyList<string> childText)
            {
                this.Handle = handle;
                this.Title = title;
                this.ClassName = className;
                this.childText = childText;
            }

            public IntPtr Handle
            {
                get;
            }

            public string Title
            {
                get;
            }

            public string ClassName
            {
                get;
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.AppendLine("Dialog:");
                builder.AppendLine("  Handle: " + this.Handle);
                builder.AppendLine("  Title: " + this.Title);
                builder.AppendLine("  Class: " + this.ClassName);
                builder.AppendLine("  Text:");

                foreach (string text in this.childText)
                {
                    builder.AppendLine("    " + text);
                }

                return builder.ToString();
            }
        }
    }
}
