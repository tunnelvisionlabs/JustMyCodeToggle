// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
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
    [Guid("0B98EAB8-00BB-45D0-AE2F-3DE35CD68235")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsTask
    {
    }
}
