using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;


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

        protected override Task<ISetSettingInterface<bool>[]> GetSetters()
        {
            ISetSettingInterface<bool> setter = ProjectExtensions.IsVS2026 ? dteSetter : settingStoreSetter;
            return Task.FromResult<ISetSettingInterface<bool>[]>([setter]);
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
        }

    }
}
