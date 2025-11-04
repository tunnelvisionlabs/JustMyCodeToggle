using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Tvl.VisualStudio.JustMyCodeToggle.Managers
{

    /// <summary>
    /// Manages DTE-based settings access for Visual Studio 2026+
    /// </summary>
    public class DteManager
    {
        public T GetDteSetting<T>(string category, string page, string propertyName)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var props = dte.Properties[category, page];
                if (props != null)
                {
                    var prop = props.Item(propertyName);
                    if (prop != null)
                    {
                        var value = prop.Value;

                        // Convert to requested type
                        if (typeof(T) == typeof(int) && value is bool)
                        {
                            return (T)(object)(((bool)value) ? 1 : 0);
                        }
                        else if (typeof(T) == typeof(bool) && value is int)
                        {
                            return (T)(object)((int)value == 1);
                        }
                        return (T)value;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDteSetting failed: {ex.Message}");
            }
            return default;
        }

        public void SetDteSetting<T>(string category, string page, string propertyName, T value)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var props = dte.Properties[category, page];

                if (props != null)
                {
                    var prop = props.Item(propertyName);
                    if (prop != null)
                    {


                        // Set through DTE - this automatically updates settings.json and notifies all VS components
                        prop.Value = value;

                        System.Diagnostics.Debug.WriteLine($"{propertyName} setting changed via DTE to: {value}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetDteSetting failed: {ex.Message}");
            }
        }
        protected async Task<EnvDTE.DTE> GetDteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            return dte;
        }
        public async Task ExecuteCommandAsync(string commandName, string args = "")
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = await GetDteAsync();
            dte.ExecuteCommand(commandName, args);
        }
        

        public async void DumpAllSettings()
        {

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

            try
            {
                System.Diagnostics.Debug.WriteLine("=== All DTE Property Pages ===");

                // DTE doesn't provide enumeration of all categories/pages, so try known ones
                var knownPages = new System.Collections.Generic.Dictionary<string, string[]>
{
    { "Debugging", new[] { "General", "Native","EditAndContinue","JustInTime" } },
    {"Debugger",["General","JIT","Symbols","VisibilityCmdUIContexts","symbols","symbols.load",] },
    { "Environment", new[] { "General", "Documents", "Keyboard", "Startup","Profiles","AutoRecover", "TaskList", "TabsAndWindows", "ProjectsandSolution", "FindAndReplace", "RoamingSettings", "WebBrowser", "Import and Export Settings", "ProductUpdates", "JavaScript Specific", "AddinMacrosSecurity", "ExtensionManager" } },
	//    { "TextEditor", new[] { "AllLanguages", "C#", "C/C++", "Basic", "F#", "XML", "Basic-Specific", "C/C++ Specific", "CSS", "CSharp", "CSharp-Specific", "CoffeeScript", "General", "HTML", "HTML Specific", "HTMLX", "JSON", "JavaScript", "JavaScript Specific", "LESS", "PlainText", "ResJSON Resource", "SCSS", "SQL Server Tools", "T-SQL90", "TypeScript", "TypeScript Specific", "XAML", "XOML", "WebForms", "WebForms Specific", "FSharp", "Razor", "Rest", "CSS Specific", "JScript Specific", "Jade", "YAML" } },
	{ "Projects", new[] { "General", "VBDefaults", "VCGeneral" } }, // , "Build and Run" works but causes a fatal crash
	{ "WindowsFormsDesigner", new[] { "General" } },
    { "Source Control", ["General" ]},
    { "XAML Designer", new[] { "Artboard", "General" } },
    { "SQL Server Tools", new[] { "Database Errors and Warnings", "General", "Online Editing" } },
    { "PkgdefLanguage", new[] { "Advanced" } },
    {"Search",["CommandScopes"] },
};

                foreach (var category in knownPages)
                {
                    System.Diagnostics.Debug.WriteLine($"\n[Category: {category.Key}]");
                    foreach (var page in category.Value)
                    {
                        try
                        {
                            var categoryProps = dte.Properties[category.Key, page];
                            if (categoryProps != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"  [Page: {page}]");
                                foreach (EnvDTE.Property prop in categoryProps)
                                {
                                    try
                                    {
                                        System.Diagnostics.Debug.WriteLine($"    {prop.Name} = {prop.Value}");
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"    {prop.Name} = <error: {ex.Message}>");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"  [Page: {page}] - Not accessible: {ex.Message}");
                        }
                    }
                }
            }
            catch { }

        }
    }
}
