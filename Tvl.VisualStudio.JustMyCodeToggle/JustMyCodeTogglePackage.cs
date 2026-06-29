// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell.Interop;
    using Tvl.VisualStudio.JustMyCodeToggle.Lightup;
    using OleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(JustMyCodeToggleConstants.GuidJustMyCodeTogglePackageString)]
    public sealed class JustMyCodeTogglePackage : IVsPackage, IOleCommandTarget, IAsyncLoadablePackageInitialize
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

        private DteApplicationWrapper ApplicationObject
        {
            get
            {
                return DteApplicationWrapper.FromObject(QueryService(DteServiceGuid, IidIUnknown));
            }
        }

        public int SetSite(OleServiceProvider psp)
        {
            _serviceProvider = psp;
            return SOk;
        }

        int IAsyncLoadablePackageInitialize.Initialize(
            object pServiceProvider,
            object pProfferService,
            object pProgressCallback,
            out IVsTask ppTask)
        {
            ppTask = null;

            return SOk;
        }

        public int QueryClose(out int pfCanClose)
        {
            pfCanClose = 1;
            return SOk;
        }

        public int Close()
        {
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
                DtePropertyWrapper enableJustMyCode = GetJustMyCodeProperty();
                if (!enableJustMyCode.IsDefault)
                {
                    object value = enableJustMyCode.Value;
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
                DtePropertyWrapper enableJustMyCode = GetJustMyCodeProperty();
                if (!enableJustMyCode.IsDefault)
                {
                    object value = enableJustMyCode.Value;
                    if (TryGetBooleanValue(value, out bool enabled))
                    {
                        enableJustMyCode.Value = !enabled;
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

        private DtePropertyWrapper GetJustMyCodeProperty()
        {
            DteApplicationWrapper applicationObject = ApplicationObject;
            if (applicationObject.IsDefault)
            {
                return default(DtePropertyWrapper);
            }

            DtePropertiesWrapper properties = applicationObject.get_Properties("Debugging", "General");
            if (properties.IsDefault)
            {
                return default(DtePropertyWrapper);
            }

            return properties.Item("EnableJustMyCode");
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
            if (serviceProvider != null)
            {
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

            return null;
        }
    }
}
