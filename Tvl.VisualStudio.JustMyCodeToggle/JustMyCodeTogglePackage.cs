// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell.Interop;
    using OleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    [ComVisible(true)]
    [Guid(JustMyCodeToggleConstants.GuidJustMyCodeTogglePackageString)]
    public sealed class JustMyCodeTogglePackage : IVsPackage, IOleCommandTarget
    {
        private const int ENotImpl = unchecked((int)0x80004001);
        private const int EPointer = unchecked((int)0x80004003);
        private const int EFail = unchecked((int)0x80004005);
        private const int OleCmdErrENotSupported = unchecked((int)0x80040100);
        private const int OleCmdErrEUnknownGroup = unchecked((int)0x80040104);
        private const int SOk = 0;

        private static readonly Guid DteServiceGuid = new Guid("04A72314-32E9-48E2-9B87-A63603454F3E");
        private static readonly Guid IidIUnknown = new Guid("00000000-0000-0000-C000-000000000046");

        private OleServiceProvider _serviceProvider;
        private IVsRegisterPriorityCommandTarget _registerPriorityCommandTarget;
        private uint _priorityCommandTargetCookie;

        private object ApplicationObject
        {
            get
            {
                return QueryService(DteServiceGuid, IidIUnknown);
            }
        }

        public int SetSite(OleServiceProvider psp)
        {
            _serviceProvider = psp;
            RegisterCommandTarget();
            return SOk;
        }

        public int QueryClose(out int pfCanClose)
        {
            pfCanClose = 1;
            return SOk;
        }

        public int Close()
        {
            UnregisterCommandTarget();
            _serviceProvider = null;
            return SOk;
        }

        public int GetAutomationObject(string pszPropName, out object ppDisp)
        {
            ppDisp = null;
            return ENotImpl;
        }

        public int CreateTool(ref Guid rguidPersistenceSlot)
        {
            return ENotImpl;
        }

        public int ResetDefaults(uint grfFlags)
        {
            return SOk;
        }

        public int GetPropertyPage(ref Guid rguidPage, VSPROPSHEETPAGE[] ppage)
        {
            return ENotImpl;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup != JustMyCodeToggleConstants.GuidJustMyCodeToggleCommandSet)
            {
                return OleCmdErrEUnknownGroup;
            }

            if (prgCmds == null)
            {
                return EPointer;
            }

            uint commandCount = Math.Min(cCmds, (uint)prgCmds.Length);
            for (int i = 0; i < commandCount; i++)
            {
                if (prgCmds[i].cmdID != JustMyCodeToggleConstants.CmdidJustMyCodeToggle)
                {
                    continue;
                }

                uint commandFlags = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                if (TryGetJustMyCode(out bool enabled) && enabled)
                {
                    commandFlags |= (uint)OLECMDF.OLECMDF_LATCHED;
                }

                prgCmds[i].cmdf = commandFlags;
            }

            return SOk;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup != JustMyCodeToggleConstants.GuidJustMyCodeToggleCommandSet)
            {
                return OleCmdErrEUnknownGroup;
            }

            if (nCmdID != JustMyCodeToggleConstants.CmdidJustMyCodeToggle)
            {
                return OleCmdErrENotSupported;
            }

            return ToggleJustMyCode() ? SOk : EFail;
        }

        private static bool IsCriticalException(Exception exception)
        {
            return exception is AccessViolationException
                || exception is AppDomainUnloadedException
                || exception is OutOfMemoryException
                || exception is ThreadAbortException;
        }

        private static bool IsFailure(int hr)
        {
            return hr < 0;
        }

        private static object InvokeComMember(object instance, string memberName, BindingFlags flags, params object[] arguments)
        {
            return instance.GetType().InvokeMember(
                memberName,
                BindingFlags.Instance | BindingFlags.Public | flags,
                null,
                instance,
                arguments,
                CultureInfo.InvariantCulture);
        }

        private static bool TryGetBooleanValue(object value, out bool result)
        {
            if (value is bool boolValue)
            {
                result = boolValue;
                return true;
            }

            if (value is int intValue)
            {
                result = intValue != 0;
                return true;
            }

            result = false;
            return false;
        }

        private bool TryGetJustMyCode(out bool enabled)
        {
            enabled = false;

            try
            {
                object enableJustMyCode = GetJustMyCodeProperty();
                if (enableJustMyCode != null)
                {
                    object value = InvokeComMember(enableJustMyCode, "Value", BindingFlags.GetProperty);
                    return TryGetBooleanValue(value, out enabled);
                }
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
            }

            return false;
        }

        private bool ToggleJustMyCode()
        {
            try
            {
                object enableJustMyCode = GetJustMyCodeProperty();
                if (enableJustMyCode != null)
                {
                    object value = InvokeComMember(enableJustMyCode, "Value", BindingFlags.GetProperty);
                    if (TryGetBooleanValue(value, out bool enabled))
                    {
                        InvokeComMember(enableJustMyCode, "Value", BindingFlags.SetProperty, !enabled);
                        UpdateCommandUI();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
            }

            return false;
        }

        private object GetJustMyCodeProperty()
        {
            object applicationObject = ApplicationObject;
            if (applicationObject == null)
            {
                return null;
            }

            object properties = InvokeComMember(
                applicationObject,
                "Properties",
                BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                "Debugging",
                "General");

            return InvokeComMember(
                properties,
                "Item",
                BindingFlags.GetProperty | BindingFlags.InvokeMethod,
                "EnableJustMyCode");
        }

        private void RegisterCommandTarget()
        {
            if (_priorityCommandTargetCookie != 0)
            {
                return;
            }

            try
            {
                IVsRegisterPriorityCommandTarget registrar =
                    QueryService<IVsRegisterPriorityCommandTarget>(typeof(SVsRegisterPriorityCommandTarget).GUID);
                if (registrar == null)
                {
                    return;
                }

                int hr = registrar.RegisterPriorityCommandTarget(0, this, out uint cookie);
                if (!IsFailure(hr) && cookie != 0)
                {
                    _registerPriorityCommandTarget = registrar;
                    _priorityCommandTargetCookie = cookie;
                }
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
            }
        }

        private void UnregisterCommandTarget()
        {
            uint cookie = _priorityCommandTargetCookie;
            IVsRegisterPriorityCommandTarget registrar = _registerPriorityCommandTarget;

            _priorityCommandTargetCookie = 0;
            _registerPriorityCommandTarget = null;

            if (cookie == 0 || registrar == null)
            {
                return;
            }

            try
            {
                registrar.UnregisterPriorityCommandTarget(cookie);
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
            }
        }

        private void UpdateCommandUI()
        {
            try
            {
                IVsUIShell uiShell = QueryService<IVsUIShell>(typeof(SVsUIShell).GUID);
                uiShell?.UpdateCommandUI(0);
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
            }
        }

        private T QueryService<T>(Guid serviceGuid)
            where T : class
        {
            Guid interfaceGuid = typeof(T).GUID;
            return QueryService(serviceGuid, interfaceGuid) as T;
        }

        private object QueryService(Guid serviceGuid, Guid interfaceGuid)
        {
            OleServiceProvider serviceProvider = _serviceProvider;
            if (serviceProvider == null)
            {
                return null;
            }

            IntPtr servicePointer;
            int hr = serviceProvider.QueryService(ref serviceGuid, ref interfaceGuid, out servicePointer);
            if (IsFailure(hr) || servicePointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.GetObjectForIUnknown(servicePointer);
            }
            finally
            {
                Marshal.Release(servicePointer);
            }
        }
    }
}
