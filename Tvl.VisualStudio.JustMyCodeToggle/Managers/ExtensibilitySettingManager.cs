using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

namespace Tvl.VisualStudio.JustMyCodeToggle.Managers
{
    internal class ExtensibilitySettingManager
    {
        public ExtensibilitySettingManager(VisualStudioExtensibility extensibility)
        {
            this.Extensibility = extensibility;
            Inited = true;
        }

        public static bool Inited {get; private set;}
        public VisualStudioExtensibility Extensibility { get; }

        public async Task<T> GetSetting<T>(string propertyName)
        {
            SettingIdentifier<T> ident = propertyName;
            var ret =  await Extensibility.Settings().ReadEffectiveValueAsync<T>(ident, CancellationToken.None);
            return ret.Value;
        }
        public async Task SetSetting<T>(string propertyName, T val)
        {
            SettingIdentifier<T> ident = propertyName;// "debugging.symbols.load.moduleFilterMode";
            await this.Extensibility.Settings().WriteAsync(batch =>
            {
                batch.WriteSetting(ident, val);
            }, $"JMC Updating {ident}", CancellationToken.None);

        }
    }
}
