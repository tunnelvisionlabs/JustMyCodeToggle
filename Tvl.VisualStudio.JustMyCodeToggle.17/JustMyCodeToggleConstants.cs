// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using Guid = System.Guid;

    internal static class JustMyCodeToggleConstants
    {
        public const string GuidJustMyCodeTogglePackageString = "79B6220F-7F5A-4ECE-B5C4-2CA3D7D5FA36";

        public const string GuidJustMyCodeToggleCommandSetString = "B1317E30-CFDA-47CA-90A0-95E894B150A0";
        public static readonly Guid GuidJustMyCodeToggleCommandSet = new Guid("{" + GuidJustMyCodeToggleCommandSetString + "}");

        public static readonly int CmdidJustMyCodeToggle = 0x0100;
    }
}
