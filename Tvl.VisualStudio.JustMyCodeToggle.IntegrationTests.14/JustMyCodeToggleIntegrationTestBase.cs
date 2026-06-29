// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Extensibility.Testing;
    using Microsoft.VisualStudio.Shell.Interop;

    public abstract class JustMyCodeToggleIntegrationTestBase : AbstractIdeIntegrationTest
    {
        protected async Task RunWithUnexpectedModalDialogDetectionAsync(Func<Task> operation)
        {
            string operationStackTrace = new StackTrace(skipFrames: 1, fNeedFileInfo: true).ToString();
            IVsUIShell uiShell = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell>(HangMitigatingCancellationToken);
            ErrorHandler.ThrowOnFailure(uiShell.GetDialogOwnerHwnd(out IntPtr mainWindow));

            using (UnexpectedModalDialogMonitor monitor = UnexpectedModalDialogMonitor.Start(mainWindow, operationStackTrace))
            {
                Task operationTask = operation();
                Task completedTask = await Task.WhenAny(operationTask, monitor.FailureTask).ConfigureAwait(false);

                if (completedTask == monitor.FailureTask)
                {
                    await Task.WhenAny(operationTask, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
                    throw await monitor.FailureTask.ConfigureAwait(false);
                }

                try
                {
                    await operationTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (monitor.FailureTask.IsCompleted)
                    {
                        throw new AggregateException(await monitor.FailureTask.ConfigureAwait(false), ex);
                    }

                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw;
                }

                monitor.ThrowIfDialogDetected();
            }
        }
    }
}
