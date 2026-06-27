// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using System;
    using System.Runtime.InteropServices;

    [ComVisible(true)]
    [Guid("3EC4D7F6-4036-4406-A393-2FFF7B2E78A1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAsyncLoadablePackageInitialize
    {
        [PreserveSig]
        int Initialize(
            [MarshalAs(UnmanagedType.IUnknown)] object pServiceProvider,
            [MarshalAs(UnmanagedType.IUnknown)] object pProfferService,
            [MarshalAs(UnmanagedType.IUnknown)] object pProgressCallback,
            out IVsTask ppTask);
    }

    [ComImport]
    [Guid("257B63FA-8388-4FEB-9DB8-3DB22F4405DE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAsyncServiceProvider
    {
        [PreserveSig]
        int QueryServiceAsync(ref Guid guidService, out IVsTask ppTask);
    }

    [ComImport]
    [Guid("0B98EAB8-00BB-45D0-AE2F-3DE35CD68235")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsTask
    {
        [PreserveSig]
        int ContinueWith(uint context, IntPtr pTaskBody, out IVsTask ppTask);

        [PreserveSig]
        int ContinueWithEx(
            uint context,
            uint options,
            IntPtr pTaskBody,
            [MarshalAs(UnmanagedType.Struct)] object pAsyncState,
            out IVsTask ppTask);

        [PreserveSig]
        int Start();

        [PreserveSig]
        int Cancel();

        [PreserveSig]
        int GetResult([MarshalAs(UnmanagedType.Struct)] out object pResult);
    }
}
