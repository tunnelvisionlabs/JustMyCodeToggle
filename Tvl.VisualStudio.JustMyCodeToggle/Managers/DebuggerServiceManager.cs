using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE100;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Tvl.VisualStudio.JustMyCodeToggle.Managers
{
    /// <summary>
    /// Not currently used, hope was for Debugger5 interface to be able to set the loading state for VS2026 but it did not work, no error just no change.
    /// </summary>
    public class DebuggerServiceManager
    {

        public bool GetLoadAllModules()
        {
            try
            {
                return GetDebugger5().OnlyLoadSymbolsManually == false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while getting symbol load mode: {ex.Message}");
            }
            return false;
        }

        protected Debugger5 GetDebugger5()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            
            if (dte == null)
                throw new Exception("DTE2 service not found.");

            var debugger = dte.Debugger as Debugger5;
            if (debugger == null)
                throw new Exception("Debugger5 interface not available.");
            return debugger;
        }

        public async Task SetLoadAllModulesAsync(bool loadAllModulesUnlessExcluded)
        {
            try
            {
                var debugger = GetDebugger5();
                bool onlyLoadSymbolsManually = !loadAllModulesUnlessExcluded;
                //ThreadHelper.ThrowIfNotOnUIThread();

                debugger.SetSymbolSettings(debugger.SymbolPath, debugger.SymbolPathState, debugger.SymbolCachePath, onlyLoadSymbolsManually, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while setting symbol load mode: {ex.Message}");
            }
        }
    }
}
