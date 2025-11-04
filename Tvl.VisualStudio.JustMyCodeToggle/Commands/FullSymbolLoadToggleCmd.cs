using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;
using Tvl.VisualStudio.JustMyCodeToggle.Managers;

namespace Tvl.VisualStudio.JustMyCodeToggle.Commands
{
    internal class DebuggerSymbolLoadSetter : ISetSettingInterface<bool>

    {
        private DebuggerServiceManager _settingsManager;

        public DebuggerSymbolLoadSetter()
        {
            _settingsManager = JustMyCodeTogglePackage.instance.GetTypedService<DebuggerServiceManager>();
        }
        public Task<bool> GetSetting() => Task.FromResult(_settingsManager.GetLoadAllModules());

        public async Task SetSetting(bool val)
        {
            await _settingsManager.SetLoadAllModulesAsync(val);
        }
    }
    [Command(PackageGuids.guidJustMyCodeTogglePackageCmdSetString, PackageIds.JMCSymbolLoadBtn)]
    internal class FullSymbolLoadBtn : OurOLEButton<FullSymbolLoadCmd, FullSymbolLoadBtn>
    {

        protected override void BeforeQueryStatus(EventArgs e)
        {
            var useCmd = !ProjectExtensions.IsVS2026;
            useCmd = true;

            this.Command.Visible = useCmd;

            //this.Command.Visible = true;

            //this.Command.Enabled = useCmd;
            //this.Command.Supported = useCmd;


            //this.Command.Enabled = useCmd;
            //this.Command.Supported = useCmd;

            base.BeforeQueryStatus(e);
        }
        protected override Task InitializeCompletedAsync()
        {

            return base.InitializeCompletedAsync(); ;
        }

        protected DteManager dteManager;
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            dteManager ??= JustMyCodeTogglePackage.instance.GetTypedService<DteManager>();
            await dteManager.ExecuteCommandAsync("Tools.Options", "Debugging.Symbols");


            //return base.ExecuteAsync(e);
        }
    }

    [VisualStudioContribution]
    internal class FullSymbolLoadExtensibilityBtn : ToggleCommand //  OurExtensibilityToggleButton<FullSymbolLoadCmd>
    {

        public override Task InitializeAsync(CancellationToken cancellationToken)
        {
            //JustMyCodeTogglePackage.instance.RegisterService(new ExtensibilitySettingManager(Extensibility));
            return base.InitializeAsync(cancellationToken);
        }
        public override CommandConfiguration CommandConfiguration => new("Toggle Full Symbol Loading2")
        {
            // Use this object initializer to set optional parameters for the command. The required parameter,
            // displayName, is set above. DisplayName is localized and references an entry in .vsextension\string-resources.json.
            Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
            Flags = CommandFlags.CanToggle,

            //VsctCommandMapping = new VsctId(new(PackageGuids.guidJustMyCodeTogglePackageCmdSetString),PackageIds.JMCSymbolLoadBtn),
            Placements = [
            CommandPlacement.KnownPlacements.ExtensionsMenu,
                //CommandPlacement.VsctParent(new(PackageGuids.guidJustMyCodeTogglePackageCmdSetString),PackageIds.JMCToolbarGroup, priority: 0x124),

            ],
        };

        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            Debug.WriteLine("DONE WITH IT");
        }
    }

    internal class FullSymbolLoadCmd : ToggleSettingDynamicSetterCmd<bool>
    {
        private SettingsStoreSetter<bool> settingStoreSetter;
        private ExtensibilityBoolToValSettingSetter<string> unifiedSetter;

        public FullSymbolLoadCmd() : base(true, false)
        {
            this.settingStoreSetter = new("Debugger", "SymbolUseExcludeList");
            this.unifiedSetter = new ExtensibilityBoolToValSettingSetter<string>(@"debugging.symbols.load.moduleFilterMode", "loadAllButExcluded", "loadOnlyIncluded");

        }

        protected override Task<ISetSettingInterface<bool>[]> GetSetters()
        {
            ISetSettingInterface<bool> setter = ProjectExtensions.IsVS2026 && ExtensibilitySettingManager.Inited ? unifiedSetter : settingStoreSetter;
            return Task.FromResult<ISetSettingInterface<bool>[]>([setter]);
        }
    }


}
