// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using System;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Extensibility.Testing;
    using Microsoft.VisualStudio.Shell.Interop;
    using Tvl.VisualStudio.JustMyCodeToggle;
    using Xunit;
    using _DTE = EnvDTE._DTE;
    using DTE = EnvDTE.DTE;
    using Property = EnvDTE.Property;

    public class JustMyCodeToggleIntegrationTest : JustMyCodeToggleIntegrationTestBase
    {
        private const string CommandName = "DebuggerContextMenus.CallStackWindow.Debug.JustMyCodeToggle";

        [IdeFact(MinVersion = VisualStudioVersion.VS2015)]
        public async Task CommandButtonTogglesJustMyCodeAsync()
        {
            DTE dte = await TestServices.Shell.GetRequiredGlobalServiceAsync<_DTE, DTE>(HangMitigatingCancellationToken);
            Assert.NotNull(dte);

            await RunWithUnexpectedModalDialogDetectionAsync(
                () => CreateTestProjectAsync(nameof(CommandButtonTogglesJustMyCodeAsync)));
            var commandId = new CommandID(
                JustMyCodeToggleConstants.GuidJustMyCodeToggleCommandSet,
                JustMyCodeToggleConstants.CmdidJustMyCodeToggle);
            string commandName = await GetCommandNameAsync(commandId);
            if (!StringComparer.Ordinal.Equals(CommandName, commandName))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Expected command name '{0}' for command {1}:{2}, but found '{3}'.",
                        CommandName,
                        commandId.Guid,
                        commandId.ID,
                        commandName ?? "<null>"));
            }

            bool originalValue = GetJustMyCodeValue(dte);

            try
            {
                await RunWithUnexpectedModalDialogDetectionAsync(
                    () => TestServices.Shell.ExecuteCommandAsync(commandId, HangMitigatingCancellationToken));
                Assert.Equal(!originalValue, GetJustMyCodeValue(dte));
            }
            finally
            {
                try
                {
                    SetJustMyCodeValue(dte, originalValue);
                }
                finally
                {
                    await TestServices.SolutionExplorer.CloseSolutionAsync(HangMitigatingCancellationToken);
                }
            }
        }

        private async Task<string> GetCommandNameAsync(CommandID commandId)
        {
            IVsCmdNameMapping commandNameMapping = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsCmdNameMapping, IVsCmdNameMapping>(HangMitigatingCancellationToken);
            Guid commandSet = commandId.Guid;
            int hr = commandNameMapping.MapGUIDIDToName(ref commandSet, (uint)commandId.ID, VSCMDNAMEOPTS.CNO_GETENU, out string commandName);
            if (hr < 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "MapGUIDIDToName failed with HRESULT 0x{0:X8}.",
                        hr));
            }

            return commandName;
        }

        private Task CreateTestProjectAsync(string testName)
        {
            string testDirectory = Path.Combine(Path.GetTempPath(), "JustMyCodeToggleTests", testName);
            return TestServices.SolutionExplorer.CreateCSharpConsoleApplicationAsync(
                testDirectory,
                testName,
                "JustMyCodeToggleTestProject",
                HangMitigatingCancellationToken);
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
