using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Tvl.VisualStudio.JustMyCodeToggle.Managers;
namespace Tvl.VisualStudio.JustMyCodeToggle.Commands
{
    [Command(PackageIds.JMCNativeCodeBtn)]
    internal class NativeDebuggingToggleCmd : BaseCommand<NativeDebuggingToggleCmd>
    {
        protected const string cpsProfileStr = "nativeDebugging";
        protected const string nonCpsPropertyStr = "EnableUnmanagedDebugging";

        private StartupProjectManager _startupProjectManager;
        private LaunchProfileManager _launchProfileManager;

        public NativeDebuggingToggleCmd()
        {

        }

        protected override async Task InitializeCompletedAsync()
        {
            _startupProjectManager = Package.GetTypedService<StartupProjectManager>();
            _launchProfileManager = Package.GetTypedService<LaunchProfileManager>();
            await UpdateCheckedState();
            _startupProjectManager.StartupProjectChanged += (_, _) => UpdateCheckedState();
            await base.InitializeCompletedAsync();
        }

        protected async Task SetValue(bool value)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (_startupProjectManager.ActiveProjectSupportsCPSProfiles)
            {
                await _launchProfileManager.UpdateLaunchProfileSetting(cpsProfileStr, value);
            }
            else
            {
                var startupProject = await _startupProjectManager.GetStartupProject();
                var configName = startupProject == null ? null : await startupProject.GetCurrentConfigurationName();
                Command.Enabled = startupProject != null && configName != null;
                if (Command.Enabled)
                {

                    var buildStorage = startupProject as IVsBuildPropertyStorage;
                    var cmdres = buildStorage.SetPropertyValue(nonCpsPropertyStr, configName, (uint)_PersistStorageType.PST_USER_FILE, value ? "true" : "false");

                    await startupProject.SaveProject(force: true);//must use force or else it is as if the change wasn't detected.  Also cannot figure out how to just save the user file I believe we must save both
                                                                  //note while this changes our .user file it doesn't update the active native debugging plan for launch

                    var dteProj = await startupProject.GetDTEProjectFromIvsProject();
                    try
                    {

                        var itm = dteProj.ConfigurationManager?.ActiveConfiguration?.Properties?.Item("EnableUnmanagedDebugging"); //this updates the actual launch project in memory object
                        if (itm != null)
                            itm.Value = value ? "true" : "false";

                    }
                    catch { }

                }
            }
        }

        private async Task UpdateCheckedState()
        {
            Command.Enabled = _startupProjectManager.HasStartupProject;
            if (!Command.Enabled)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (_startupProjectManager.ActiveProjectSupportsCPSProfiles)
            {
                var launchProfile = await _launchProfileManager.GetLaunchProfile();
                if (launchProfile == null)
                {
                    Debug.WriteLine($"JMC NativeDebuggingToggleCmd: in theory project supports CPS profiles but no launch profile found??");
                    Command.Enabled = false;
                }
                else
                {
                    var curVal = launchProfile.OtherSettings.FirstOrDefault(a => a.Key.Equals(cpsProfileStr, StringComparison.CurrentCultureIgnoreCase));
                    Command.Checked = curVal.Key != default && ((bool)curVal.Value == true);
                }
            }
            else
            {
                var startupProject = await _startupProjectManager.GetStartupProject();
                var configName = await startupProject.GetCurrentConfigurationName();
                Command.Enabled = startupProject != null && configName != null;
                if (Command.Enabled)
                {

                    var buildStorage = startupProject as IVsBuildPropertyStorage;
                    var cmdres = buildStorage.GetPropertyValue(nonCpsPropertyStr, configName, (uint)_PersistStorageType.PST_USER_FILE, out var existing);
                    if (cmdres != VSConstants.S_OK || String.IsNullOrWhiteSpace(existing))
                        buildStorage.GetPropertyValue(nonCpsPropertyStr, configName, (uint)_PersistStorageType.PST_PROJECT_FILE, out existing);//user file will override project file but if user file is blank fallback to project


                    Command.Checked = existing == "true";

                }

            }
        }
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await UpdateCheckedState();
            await SetValue(!Command.Checked);
            await UpdateCheckedState();
            await base.ExecuteAsync(e);
        }
    }
}
