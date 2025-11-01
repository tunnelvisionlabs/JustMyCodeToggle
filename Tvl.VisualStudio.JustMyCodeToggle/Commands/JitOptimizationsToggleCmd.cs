using System.Collections.Generic;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;

namespace Tvl.VisualStudio.JustMyCodeToggle.Commands
{
    [Command(PackageGuids.guidJustMyCodeTogglePackageCmdSetString,PackageIds.JMCDisableJitOptmizationsBtn)]
    internal class JitOptimizationsBtn : OurOLEButton<JitOptimizationsCommand,JitOptimizationsBtn>
    {
    }

    internal class JitOptimizationsCommand : ToggleSettingCmd<bool>, ISetSettingInterface<bool>
    {
        protected StartupProfileEnvVarSetter profileSetter;

        private const string primaryCheckVar = "COMPlus_ZapDisable";
        public JitOptimizationsCommand() : base(null, true, false)
        {
            setter = this;
            profileSetter = new(primaryCheckVar, "0");
            var prefixes = new string[] { "DOTNET_", "COMPlus_" };
            var envs = new List<KeyValuePair<string, string>>();
            var baseVars = new Dictionary<string, string>(){ {"ZapDisable","1" },
                        {"ReadyToRun","0" },
                        {"TieredCompilation","0" },
                        {"JITMinOpts","1" }};
            foreach (var kvp in baseVars)
            {
                foreach (var prefix in prefixes)
                {
                    var key = prefix + kvp.Key;
                    if (key != primaryCheckVar)
                        envs.Add(new(key, kvp.Value));
                }
            }
            this.ENVVars = envs.ToArray();
            profileSetter.StartManager.StartupProjectChanged += (_,_) => this.SyncCheckedToCurVal();
            profileSetter.StartManager.ActiveProjectSupportsCPSProfilesChanged += (_, _) => SyncEnabled();
        }

       
        private void SyncEnabled()
        {
            Native.SetEnabled( profileSetter.StartManager.ActiveProjectSupportsCPSProfiles);
            
        }

        public KeyValuePair<string, string>[] ENVVars { get; }

        public async Task<bool> GetSetting()
        {
            return await profileSetter.GetSetting() == "1";
        }

        public Task SetSetting(bool val)
        {

            return profileSetter.SetSettingAddl(val ? "1" : "0", this.ENVVars);
        }
    }
}
