using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Tvl.VisualStudio.JustMyCodeToggle.Managers;
using kvp = System.Collections.Generic.KeyValuePair<string, string>;
namespace Tvl.VisualStudio.JustMyCodeToggle.Commands
{
    internal class ExtensibilityBoolToValSettingSetter<SETTING_TYPE> : ExtensibilitySettingSetter<SETTING_TYPE>, ISetSettingInterface<bool>
    {
        internal SETTING_TYPE trueVal;
        internal SETTING_TYPE falseVal;

        public ExtensibilityBoolToValSettingSetter(string propertyName, SETTING_TYPE trueVal, SETTING_TYPE falseVal) : base(propertyName)
        {
            this.trueVal = trueVal;
            this.falseVal = falseVal;
        }
        Task ISetSettingInterface<bool>.SetSetting(bool val)
        {
            return this.SetSetting(val ? trueVal : falseVal);
        }

        async Task<bool> ISetSettingInterface<bool>.GetSetting()
        {
            return EqualityComparer<SETTING_TYPE>.Default.Equals(await this.GetSetting(),trueVal);
        }
    }
    internal class ExtensibilitySettingSetter<SETTING_TYPE> : ISetSettingInterface<SETTING_TYPE>
    {
        public async Task<SETTING_TYPE> GetSetting() {
            if (! _settingsManager.IsCompleted)
                return default;
            return await (await _settingsManager).GetSetting<SETTING_TYPE>(propertyName);
        }
        public async Task SetSetting(SETTING_TYPE val)
        {
            // if settings manager is not ready we silently do nothing,  waiting for it will cause deadlock
            if (! _settingsManager.IsCompleted)
                return;
            await (await _settingsManager).SetSetting(propertyName, val);
        }
        protected readonly string propertyName;
        protected Task<ExtensibilitySettingManager> _settingsManager => ExtensibilitySettingManager.Instance;
        public async Task<IDisposable> WatchSetting<SETTING_TYPE>(Action<SettingValue<SETTING_TYPE>> onChange) => await (await _settingsManager).WatchSetting(propertyName, onChange);
        public ExtensibilitySettingSetter(string propertyName)
        {
            this.propertyName = propertyName;
        }
    }
    /// <summary>
    /// for bool values use strings true/false
    /// </summary>
    internal class BuildStoreSetter<SETTING_TYPE> : ISetSettingInterface<SETTING_TYPE>
    {
        public async Task<SETTING_TYPE> GetSetting()
        {
            if (!await SyncToCurStartProj())
                return default;

            var buildStorage = curStartupProject as IVsBuildPropertyStorage;

            var cmdres = buildStorage.GetPropertyValue(propertyName, curConfigName, (uint)_PersistStorageType.PST_USER_FILE, out var existing);
            if (cmdres != VSConstants.S_OK || String.IsNullOrWhiteSpace(existing))
                buildStorage.GetPropertyValue(propertyName, curConfigName, (uint)_PersistStorageType.PST_PROJECT_FILE, out existing);//user file will override project file but if user file is blank fallback to project


            return strToVal(existing);


        }
        public async Task<bool> SyncToCurStartProj()
        {
            curStartupProject = await _settingsManager.Value.GetStartupProject();
            curConfigName = curStartupProject == null ? null : await curStartupProject.GetCurrentConfigurationName();
            return curStartupProject != null && curConfigName != null;
        }
        protected virtual string valToStr(SETTING_TYPE val)
        {
            if (val is bool bval)
                return bval ? "true" : "false";
            else if (val is int ival)
                return ival.ToString();
            return val.ToString();
        }
        protected virtual SETTING_TYPE strToVal(string str)
        {
            if (typeof(SETTING_TYPE) == typeof(bool))
            {
                if (Boolean.TryParse(str, out var bval))
                    return (SETTING_TYPE)(object)bval;
                else
                    return default;

            }
            else if (typeof(SETTING_TYPE) == typeof(int))
            {
                if (Int32.TryParse(str, out var ival))
                    return (SETTING_TYPE)(object)ival;
                else
                    return default;
            }
            return (SETTING_TYPE)(object)str;
        }
        public async Task SetSetting(SETTING_TYPE _val)
        {

            if (!await SyncToCurStartProj())
                return;
            var val = valToStr(_val);

            var buildStorage = curStartupProject as IVsBuildPropertyStorage;
            var cmdres = buildStorage.SetPropertyValue(propertyName, curConfigName, (uint)_PersistStorageType.PST_USER_FILE, val);

            await curStartupProject.SaveProject(force: true);//must use force or else it is as if the change wasn't detected.  Also cannot figure out how to just save the user file I believe we must save both
                                                             //note while this changes our .user file it doesn't update the active native debugging plan for launch

            var dteProj = await curStartupProject.GetDTEProjectFromIvsProject();
            try
            {

                var itm = dteProj.ConfigurationManager?.ActiveConfiguration?.Properties?.Item(propertyName); //this updates the actual launch project in memory object
                if (itm != null)
                    itm.Value = val;

            }
            catch { }



        }


        protected readonly string propertyName;
        protected Lazy<StartupProjectManager> _settingsManager;
        private IVsProject curStartupProject;
        private string curConfigName;

        public BuildStoreSetter(string propertyName)
        {
            this.propertyName = propertyName;
            _settingsManager = new(JustMyCodeTogglePackage.instance.GetTypedService<StartupProjectManager>);
        }
    }
    internal class DteStoreSetter<SETTING_TYPE> : ISetSettingInterface<SETTING_TYPE>
    {
        public Task<SETTING_TYPE> GetSetting() => Task.FromResult(_settingsManager.Value.GetDteSetting<SETTING_TYPE>(category, page, propertyName));

        public Task SetSetting(SETTING_TYPE val)
        {
            _settingsManager.Value.SetDteSetting(category, page, propertyName, val);
            return Task.CompletedTask;
        }

        protected readonly string category;
        private readonly string page;
        protected readonly string propertyName;
        protected Lazy<DteManager> _settingsManager;

        public DteStoreSetter(string category, string page, string propertyName)
        {

            this.category = category;
            this.page = page;
            this.propertyName = propertyName;
            _settingsManager = new(JustMyCodeTogglePackage.instance.GetTypedService<DteManager>);
        }
    }
    internal class StartupProfileEnvVarSetter : StartupProfileSetter<string>
    {
        private string deleteOnVal;
        override public async Task<string> GetSetting()
        {
            return await _settingsManager.Value.GetProfileEnvVar(propertyName) ?? deleteOnVal;
        }

        public virtual Task SetSetting(string val) => _settingsManager.Value.UpdateLaunchProfileENVVars(val == deleteOnVal, new kvp(propertyName, val));
        public virtual Task SetSettingAddl(string val, params kvp[] addl) => _settingsManager.Value.UpdateLaunchProfileENVVars(val == deleteOnVal, [new kvp(propertyName, val), .. addl]);
        public StartupProfileEnvVarSetter(string propertyName, string deleteOnVal) : base(propertyName)
        {
            this.deleteOnVal = deleteOnVal;
        }
    }
    internal class StartupProfileSetter<SETTING_TYPE> : ISetSettingInterface<SETTING_TYPE>
    {
        public virtual async Task<SETTING_TYPE> GetSetting()
        {
            var launchProfile = await _settingsManager.Value.GetLaunchProfile();
            if (launchProfile == null)
            {
                Debug.WriteLine($"JMC: in theory project supports CPS profiles but no launch profile found??");
                return default;
            }
            else
            {
                var curVal = launchProfile.OtherSettings.FirstOrDefault(a => a.Key.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase));
                return (SETTING_TYPE)curVal.Value;
            }
        }

        public virtual Task SetSetting(SETTING_TYPE val) => _settingsManager.Value.UpdateLaunchProfileSetting(propertyName, val);

        protected readonly string propertyName;
        protected Lazy<LaunchProfileManager> _settingsManager;
        protected Lazy<StartupProjectManager> _startManager;

        public StartupProjectManager StartManager => _startManager.Value;
        public bool SupportsLaunchProfiles => _settingsManager.Value.SupportsLaunchProfiles;

        public StartupProfileSetter(string propertyName)
        {
            this.propertyName = propertyName;
            _settingsManager = new(JustMyCodeTogglePackage.instance.GetTypedService<LaunchProfileManager>);
            _startManager = new(JustMyCodeTogglePackage.instance.GetTypedService<StartupProjectManager>);
        }
    }

    internal class SettingsStoreSetter<SETTING_TYPE> : ISetSettingInterface<SETTING_TYPE>
    {
        public Task<SETTING_TYPE> GetSetting() => Task.FromResult(_settingsManager.Value.GetSetting<SETTING_TYPE>(collectionPath, propertyName));

        public Task SetSetting(SETTING_TYPE val)
        {
            _settingsManager.Value.SetSetting(collectionPath, propertyName, val);
            return Task.CompletedTask;
        }

        protected readonly string collectionPath;
        protected readonly string propertyName;
        protected Lazy<SettingsStoreManager> _settingsManager;

        public SettingsStoreSetter(string collectionPath, string propertyName)
        {

            this.collectionPath = collectionPath;
            this.propertyName = propertyName;
            _settingsManager = new(JustMyCodeTogglePackage.instance.GetTypedService<SettingsStoreManager>);
        }

    }
    internal interface ISetSettingInterface<SETTING_TYPE>
    {
        Task SetSetting(SETTING_TYPE val);
        Task<SETTING_TYPE> GetSetting();
    }
}
