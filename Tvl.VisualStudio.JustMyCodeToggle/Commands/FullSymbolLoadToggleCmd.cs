using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Extensibility.Commands;
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
        protected DteManager dteManager;

    }

    internal class FullSymbolLoadCmd : ToggleSettingDynamicSetterCmd<bool>
    {
        private SettingsStoreSetter<bool> settingStoreSetter;
        private ExtensibilityBoolToValSettingSetter<string> unifiedSetter;
        private Task<IDisposable> watched;
        public FullSymbolLoadCmd() : base(true, false)
        {
            this.settingStoreSetter = new("Debugger", "SymbolUseExcludeList");
            this.unifiedSetter = new ExtensibilityBoolToValSettingSetter<string>(@"debugging.symbols.load.moduleFilterMode", "loadAllButExcluded", "loadOnlyIncluded");
            if (ProjectExtensions.IsVS2026)
            {
                watched = this.unifiedSetter.WatchSetting<string>( (sv) => this.SetCheckedToMatch(sv.Value == this.unifiedSetter.trueVal) );
            }
            else
            {
                setter = settingStoreSetter;
            }
            SetUsOnUnifiedSetterReady();
        }

        private async void SetUsOnUnifiedSetterReady()
        {
            await watched;
            await SyncCheckedToCurVal();
        }

        protected override Task<ISetSettingInterface<bool>[]> GetSetters()
        {
            ISetSettingInterface<bool> setter = ProjectExtensions.IsVS2026 ? unifiedSetter : settingStoreSetter;
            return Task.FromResult<ISetSettingInterface<bool>[]>([setter]);
        }
    }


}
