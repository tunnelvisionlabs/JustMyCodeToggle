using System;
using System.Collections.Generic;
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
    [Command(PackageGuids.guidJustMyCodeTogglePackageCmdSetString,PackageIds.JMCNativeCodeBtn)]
    internal class OleNativeDebuggingBtn : OurOLEButton<NativeDebuggingCmd, OleNativeDebuggingBtn> {

        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            return base.ExecuteAsync(e);
        }
    }
    internal class NativeDebuggingCmd : ToggleSettingDynamicSetterCmd<bool>
    {
        private StartupProfileSetter<bool> profileSetter;
        private BuildStoreSetter<bool> storageSetter;

        public NativeDebuggingCmd() : base(true, false)
        {
            this.profileSetter = new("nativeDebugging");
            this.storageSetter = new("EnableUnmanagedDebugging");
            profileSetter.StartManager.StartupProjectChanged += (_, _) => this.SyncCheckedToCurVal();
        }
        protected override Task<ISetSettingInterface<bool>[]> GetSetters()
        {
            ISetSettingInterface<bool> setter = profileSetter.SupportsLaunchProfiles ? profileSetter : storageSetter;
            return Task.FromResult<ISetSettingInterface<bool>[]>([setter]);
        }
    }
}
