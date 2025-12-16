using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;

namespace Tvl.VisualStudio.JustMyCodeToggle.Managers
{
    /// <summary>
    /// Handles launch profile configuration and management
    /// </summary>
    public class LaunchProfileManager
    {
        private readonly StartupProjectManager _startupProjectManager;

        public LaunchProfileManager(StartupProjectManager startupProjectManager)
        {
            _startupProjectManager = startupProjectManager;
        }
        public async Task<string> GetProfileEnvVar(string varName)
        {
           return (await GetLaunchProfile())?.EnvironmentVariables.FirstOrDefault(a => a.Key == varName).Value;
        }
        public bool SupportsLaunchProfiles => _startupProjectManager.ActiveProjectSupportsCPSProfiles;
        public Task UpdateLaunchProfileENVVars(bool isDelete, params KeyValuePair<string, string>[] vars)
        {
            if (! SupportsLaunchProfiles)
                return Task.CompletedTask;

            return TryModifyActiveProfile((profile) =>
            {
                if (isDelete)
                {
                    foreach (var var in vars)
                        profile.EnvironmentVariables.Remove(var.Key);
                }
                else
                {
                    foreach (var var in vars)
                        profile.EnvironmentVariables[var.Key] = var.Value;
                }
            });
        }

        public Task UpdateLaunchProfileSetting(string key, object value)
        {
            return TryModifyActiveProfile((profile) => profile.OtherSettings[key] = value);
        }

        private async Task TryModifyActiveProfile(Action<IWritableLaunchProfile> onEditProfile)
        {
            var launchProvider = await GetLaunchProvider();
            var activeProfile = launchProvider.ActiveProfile?.Name;
            if (activeProfile == null)
                return;
            await launchProvider.TryUpdateProfileAsync(activeProfile, onEditProfile);
        }

        private async Task<ILaunchSettingsProvider3> GetLaunchProvider()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var proj = await _startupProjectManager.GetStartupProject();
            var provider = proj.AsUnconfiguredProject()?.Services.ActiveConfiguredProjectProvider?.ActiveConfiguredProject?.Services.ExportProvider;
            return provider?.GetService<ILaunchSettingsProvider3>();
        }

        public async Task<ILaunchProfile2> GetLaunchProfile()
        {
            var launchProvider = await GetLaunchProvider();
            return launchProvider?.ActiveProfile as ILaunchProfile2;
        }
    }


}
