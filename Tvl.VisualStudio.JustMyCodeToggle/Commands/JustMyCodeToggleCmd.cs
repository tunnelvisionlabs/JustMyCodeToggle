using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Extensibility.Settings;


namespace Tvl.VisualStudio.JustMyCodeToggle.Commands
{
    [Command(PackageGuids.guidJustMyCodeTogglePackageCmdSetString,PackageIds.JMCMenuController)]
    internal class MenuBtnCmd : BaseCommand<MenuBtnCmd>
    {
        public MenuBtnCmd() => instance = this;
        internal static MenuBtnCmd instance;
    }

    [Command(PackageGuids.guidJustMyCodeTogglePackageCmdSetString,PackageIds.JMCJustMyCodeBtn)]
    internal class JustMyCodeBtn : OurOLEButton<JustMyCodeCmd, JustMyCodeBtn>
    {
    }

    internal class JustMyCodeCmd : ToggleSettingDynamicSetterCmd<bool>
    {
        private SettingsStoreSetter<bool> settingStoreSetter;
        private DteStoreSetter<bool> dteSetter;
        public static JustMyCodeCmd instance;
        private ExtensibilityBoolToValSettingSetter<bool> unifiedSetter; //only for notifications
        private Task<IDisposable> watched;

        public override Task Execute()
        {
            return base.Execute();
        }
        protected override Task<ISetSettingInterface<bool>[]> GetSetters()
        {
            ISetSettingInterface<bool> setter = ProjectExtensions.IsVS2026 ? dteSetter : settingStoreSetter;
            if (ProjectExtensions.IsVS2026)
                AddWatcher();


            return Task.FromResult<ISetSettingInterface<bool>[]>([setter]);
        }

        private void AddWatcher()
        {
            // We could use the new extensibility /unified settings manager but right now that only works after load so instead we will use dte mostly but get notifications from unified
            this.unifiedSetter = new ExtensibilityBoolToValSettingSetter<bool>(@"debugging.general.justMyCode", true, false);
            if (ProjectExtensions.IsVS2026 )
                this.watched = this.unifiedSetter.WatchSetting<bool>((sv) => SetCheckedToMatch(sv.Value));
        }


        override protected void SetCheckedToMatch(bool cur)
        {
            base.SetCheckedToMatch(cur);
            if (MenuBtnCmd.instance != null)
                MenuBtnCmd.instance.Command.Checked = Native.IsChecked(); //keep menu in sync with us
        }

        public JustMyCodeCmd() : base(true, false)
        {
            this.settingStoreSetter = new("Debugger", "JustMyCode");
            this.dteSetter = new("Debugging", "General", "EnableJustMyCode");
            instance = this;
        }

    }
}
