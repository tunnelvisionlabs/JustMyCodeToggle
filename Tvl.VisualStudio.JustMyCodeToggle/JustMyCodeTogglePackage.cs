// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell.Interop;
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
        private object _asyncServiceProvider;

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
            return SOk;
        }

        int IAsyncLoadablePackageInitialize.Initialize(
            object pServiceProvider,
            object pProfferService,
            object pProgressCallback,
            out IVsTask ppTask)
        {
            ppTask = null;

            _asyncServiceProvider = pServiceProvider;

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
            _asyncServiceProvider = null;
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

        private static object GetAsyncQueryResult(object asyncQueryResult)
        {
            if (asyncQueryResult == null)
            {
                return null;
            }

            IVsTask vsTask = asyncQueryResult as IVsTask;
            if (vsTask != null)
            {
                int hr = vsTask.GetResult(out object service);
                return IsFailure(hr) ? null : service;
            }

            Task task = asyncQueryResult as Task;
            if (task != null)
            {
                task.GetAwaiter().GetResult();

                PropertyInfo resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task, null);
            }

            if (TryGetVsTaskResult(asyncQueryResult, out object result))
            {
                return result;
            }

            return asyncQueryResult;
        }

        private static bool TryGetVsTaskResult(object asyncQueryResult, out object result)
        {
            result = null;

            MethodInfo[] methods = asyncQueryResult.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != "GetResult"
                    && !method.Name.EndsWith(".GetResult", StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1 || !parameters[0].ParameterType.IsByRef)
                {
                    continue;
                }

                object[] arguments = new object[1];
                object returnValue = method.Invoke(asyncQueryResult, arguments);
                if (method.ReturnType == typeof(int) && IsFailure((int)returnValue))
                {
                    return false;
                }

                result = arguments[0];
                return true;
            }

            return false;
        }

        private static object QueryAsyncServiceProvider(object asyncServiceProvider, Guid serviceGuid)
        {
            IAsyncServiceProvider localAsyncServiceProvider = asyncServiceProvider as IAsyncServiceProvider;
            if (localAsyncServiceProvider != null)
            {
                object service = QueryLocalAsyncServiceProvider(localAsyncServiceProvider, serviceGuid);
                if (service != null)
                {
                    return service;
                }
            }

            MethodInfo queryServiceAsync = GetQueryServiceAsyncMethod(asyncServiceProvider.GetType());
            if (queryServiceAsync == null)
            {
                return null;
            }

            ParameterInfo[] parameters = queryServiceAsync.GetParameters();
            object[] arguments = new object[parameters.Length];
            arguments[0] = serviceGuid;
            if (parameters.Length == 2)
            {
                arguments[1] = true;
            }

            object asyncQueryResult = queryServiceAsync.Invoke(asyncServiceProvider, arguments);
            return GetAsyncQueryResult(asyncQueryResult);
        }

        private static MethodInfo GetQueryServiceAsyncMethod(Type asyncServiceProviderType)
        {
            MethodInfo[] methods = asyncServiceProviderType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo method in methods)
            {
                if (method.Name != "QueryServiceAsync"
                    && !method.Name.EndsWith(".QueryServiceAsync", StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1 && parameters.Length != 2)
                {
                    continue;
                }

                Type serviceGuidParameterType = parameters[0].ParameterType;
                if (serviceGuidParameterType.IsByRef)
                {
                    serviceGuidParameterType = serviceGuidParameterType.GetElementType();
                }

                if (serviceGuidParameterType != typeof(Guid))
                {
                    continue;
                }

                if (parameters.Length == 2 && parameters[1].ParameterType != typeof(bool))
                {
                    continue;
                }

                return method;
            }

            return null;
        }

        private static object QueryLocalAsyncServiceProvider(IAsyncServiceProvider asyncServiceProvider, Guid serviceGuid)
        {
            int hr = asyncServiceProvider.QueryServiceAsync(ref serviceGuid, out IVsTask serviceTask);
            if (IsFailure(hr) || serviceTask == null)
            {
                return null;
            }

            hr = serviceTask.GetResult(out object service);
            return IsFailure(hr) ? null : service;
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

            object asyncServiceProvider = _asyncServiceProvider;
            if (asyncServiceProvider == null)
            {
                return null;
            }

            try
            {
                return QueryAsyncServiceProvider(asyncServiceProvider, serviceGuid);
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
                return null;
            }
        }
    }
}
