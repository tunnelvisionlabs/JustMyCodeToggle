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
    using Project = EnvDTE.Project;
    using Property = EnvDTE.Property;

    public class JustMyCodeToggleIntegrationTest : AbstractIdeIntegrationTest
    {
        private const string CommandName = "DebuggerContextMenus.CallStackWindow.Debug.JustMyCodeToggle";

        [IdeFact(MinVersion = VisualStudioVersion.VS2015)]
        public async Task CommandButtonTogglesJustMyCodeAsync()
        {
            DTE dte = await TestServices.Shell.GetRequiredGlobalServiceAsync<_DTE, DTE>(HangMitigatingCancellationToken);
            Assert.NotNull(dte);

            CreateTestProject(dte, nameof(CommandButtonTogglesJustMyCodeAsync));
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
                await TestServices.Shell.ExecuteCommandAsync(commandId, HangMitigatingCancellationToken);
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
                    dte.Solution.Close(SaveFirst: false);
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

        private static void CreateTestProject(DTE dte, string testName)
        {
            dte.Solution.Close(SaveFirst: false);

            string testDirectory = Path.Combine(Path.GetTempPath(), "JustMyCodeToggleTests", testName);
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }

            Directory.CreateDirectory(testDirectory);

            string projectFile = Path.Combine(testDirectory, "JustMyCodeToggleTestProject.csproj");
            string sourceFile = Path.Combine(testDirectory, "Program.cs");
            string solutionFile = Path.Combine(testDirectory, testName + ".sln");

            File.WriteAllText(projectFile, CreateProjectFile());
            File.WriteAllText(sourceFile, CreateProgramFile());

            dte.Solution.Create(testDirectory, testName);
            Project project = dte.Solution.AddFromFile(projectFile, Exclusive: false);
            Assert.NotNull(project);
            dte.Solution.SaveAs(solutionFile);
        }

        private static string CreateProjectFile()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{540EB831-C542-45A1-8FA2-A3098277B1D9}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <RootNamespace>JustMyCodeToggleTestProject</RootNamespace>
    <AssemblyName>JustMyCodeToggleTestProject</AssemblyName>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Program.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
";
        }

        private static string CreateProgramFile()
        {
            return @"namespace JustMyCodeToggleTestProject
{
    internal static class Program
    {
        private static void Main()
        {
        }
    }
}
";
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
