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
    [VisualStudioContribution]
    public class ExtensibilitySettingManager : IExtensibilitySettingManager
    {
        [VisualStudioContribution]
        public static BrokeredServiceConfiguration BrokeredServiceConfiguration
            => new(IExtensibilitySettingManager.Configuration.ServiceName, IExtensibilitySettingManager.Configuration.ServiceVersion, typeof(ExtensibilitySettingManager))
            {
                ServiceAudience = BrokeredServiceAudience.Local,
                
            };

        public ExtensibilitySettingManager(VisualStudioExtensibility extensibility)
        {
            this.Extensibility = extensibility;
            tcs.TrySetResult(this);
            Inited = true;
            //JustMyCodeTogglePackage.instance?.RegisterService(this);
        }

        public static bool Inited { get; private set; }
        public VisualStudioExtensibility Extensibility { get; }
        public static Task<ExtensibilitySettingManager> Instance => tcs.Task;
        private static TaskCompletionSource<ExtensibilitySettingManager> tcs = new();

        public async Task<T> GetSetting<T>(string propertyName)
        {
            SettingIdentifier<T> ident = propertyName;


            var ret = await Extensibility.Settings().ReadEffectiveValueAsync<T>(ident, CancellationToken.None);

            return ret.Value;
        }
        public async Task SetSetting<T>(string propertyName, T val)
        {
            SettingIdentifier<T> ident = propertyName;
            await this.Extensibility.Settings().WriteAsync(batch =>
            {
                batch.WriteSetting(ident, val);
            }, $"JMC Updating {ident}", CancellationToken.None);

        }
        public async Task<IDisposable> WatchSetting<T>(string propertyName, Action<SettingValue<T>> onChange)
        {
            SettingIdentifier<T> ident = propertyName;
            return await Extensibility.Settings().SubscribeAsync(ident, default, changeHandler: onChange);
        }

        public Task<VisualStudioExtensibility> GetExtensibility() => Task.FromResult(this.Extensibility);
    }
}
