using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Tvl.VisualStudio.JustMyCodeToggle
{

    /// <summary>
    /// Extension methods for Visual Studio project types
    /// </summary>
    public static class ProjectExtensions
    {
        public static bool IsVS2026 => _isVS2026.Value;
        private static Lazy<bool> _isVS2026 = new ( () => 
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var shell = (IVsShell)Package.GetGlobalService(typeof(SVsShell));
                if (shell != null)
                {
                    shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out object version);
                    if (version is string versionString)
                    {
                        // VS 2026 is version 18.x
                        if (versionString.StartsWith("18.") || versionString.StartsWith("19."))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // If we can't detect version, assume older version
            }
            return false;
        });
        public static T GetTypedService<T>(this AsyncPackage package) where T : class => package.GetService<T, T>();
        public static async Task<EnvDTE.Project> GetDTEProjectFromIvsProject(this IVsProject proj)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ((IVsHierarchy)proj).GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object prop);
            return prop as EnvDTE.Project;
        }

        public static async Task<string> GetCurrentConfigurationName(this IVsProject project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var hProject = project as IVsHierarchy;
            hProject.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out var projGuid);
            var bm = await VS.Services.GetSolutionBuildManagerAsync() as IVsSolutionBuildManager5;
            bm.FindActiveProjectCfgName(ref projGuid, out var currentConfigName);
            return currentConfigName;
        }

        public static async Task SaveProject(this IVsProject project, bool force)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var solution = await VS.Services.GetSolutionAsync();
            solution.SaveSolutionElement(
                force ? (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave : (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty,
                project as IVsHierarchy,
                0);
        }
    }


}
