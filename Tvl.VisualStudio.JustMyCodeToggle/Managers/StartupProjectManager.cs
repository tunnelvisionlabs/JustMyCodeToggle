using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Tvl.VisualStudio.JustMyCodeToggle.Managers
{
    /// <summary>
    /// Manages startup project state and notifications
    /// </summary>
    public class StartupProjectManager
    {
        
        public bool ActiveProjectSupportsCPSProfiles
        {
            get; private set
            {
                if (field == value)
                    return;
                field = value;
                ActiveProjectSupportsCPSProfilesChanged?.Invoke(null, EventArgs.Empty);
            }
        }
        public event EventHandler StartupProjectChanged;
        public event EventHandler ActiveProjectSupportsCPSProfilesChanged;
        private string lastStartupProject;
        public bool HasStartupProject => !string.IsNullOrWhiteSpace(lastStartupProject);

        public async Task<IVsProject> GetStartupProject()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                var bm = await VS.Services.GetSolutionBuildManagerAsync();
                if (bm?.get_StartupProject(out var hstartupProject) == VSConstants.S_OK && hstartupProject != null)
                    return (IVsProject)hstartupProject;
            }
            catch { } // Some project types can throw with get_startupproject
            return null;
        }

        public async void CheckStartupProjectChanged(bool forceEvent = false, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                await Task.Delay(50); // Make sure it has actually updated first
                var project = await GetStartupProject();
                var hProject = project as IVsHierarchy;

                string startupProject = null;
                if (project != null)
                {
                    if (project.GetMkDocument((uint)VSConstants.VSITEMID.Root, out startupProject) != VSConstants.S_OK)
                        if (hProject.GetCanonicalName((uint)VSConstants.VSITEMID.Root, out startupProject) != VSConstants.S_OK)
                            startupProject = null;
                }

                var oldVal = lastStartupProject;
                lastStartupProject = startupProject;
                ActiveProjectSupportsCPSProfiles = hProject?.IsCapabilityMatch("LaunchProfiles") == true;

                if (oldVal != lastStartupProject || forceEvent)
                {
                    ActiveProjectSupportsCPSProfilesChanged?.Invoke(null, EventArgs.Empty);
                    StartupProjectChanged?.Invoke(null, EventArgs.Empty);
                }
            }
            catch
            {
                ActiveProjectSupportsCPSProfiles = false;
            }
        }
    }


}
