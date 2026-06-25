// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using System;
    using System.Globalization;
    using Xunit;
    using _DTE = EnvDTE._DTE;
    using DTE = EnvDTE.DTE;
    using Property = EnvDTE.Property;
    using ServiceProvider = Microsoft.VisualStudio.Shell.ServiceProvider;

    public class JustMyCodeToggleIntegrationTest
    {
        private const string CommandName = "Debug.JustMyCodeToggle";

        [IdeFact(MinVersion = VisualStudioVersion.VS2015)]
        public void CommandButtonTogglesJustMyCode()
        {
            var dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(_DTE));
            Assert.NotNull(dte);

            bool originalValue = GetJustMyCodeValue(dte);

            try
            {
                dte.ExecuteCommand(CommandName);
                Assert.Equal(!originalValue, GetJustMyCodeValue(dte));
            }
            finally
            {
                SetJustMyCodeValue(dte, originalValue);
            }
        }

        private static Property GetJustMyCodeProperty(DTE dte)
        {
            return dte.get_Properties("Debugging", "General").Item("EnableJustMyCode");
        }

        private static bool GetJustMyCodeValue(DTE dte)
        {
            return Convert.ToBoolean(GetJustMyCodeProperty(dte).Value, CultureInfo.InvariantCulture);
        }

        private static void SetJustMyCodeValue(DTE dte, bool value)
        {
            Property property = GetJustMyCodeProperty(dte);
            if (Convert.ToBoolean(property.Value, CultureInfo.InvariantCulture) != value)
            {
                property.Value = value;
            }
        }
    }
}
