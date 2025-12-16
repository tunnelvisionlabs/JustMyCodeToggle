using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Tvl.VisualStudio.JustMyCodeToggle.Managers
{
    /// <summary>
    /// Manages Visual Studio settings storage and retrieval
    /// </summary>
    public class SettingsStoreManager
    {
        private readonly IVsSettingsManager _settingsManager;

        public SettingsStoreManager(IVsSettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        

        public T GetSetting<T>(string collectionPath, string propertyName)
        {
            // Legacy settings store for VS 2022 and earlier
            var store = GetSettingsStore();
            object ret = null;
            int res = 0;

            if (typeof(T) == typeof(string))
            {
                res = store.GetString(collectionPath, propertyName, out var val);
                ret = val;
            }
            else if (typeof(T) == typeof(int))
            {
                res = store.GetInt(collectionPath, propertyName, out var val);
                ret = val;
            }
            else if (typeof(T) == typeof(bool))
            {
                res = store.GetBool(collectionPath, propertyName, out var val);
                ret = val == 1;
            }
            else
                throw new NotImplementedException($"Type {typeof(T)} is not supported for settings");

            return res != 0 ? default : (T)ret;
        }

        public void SetSetting<T>(string collectionPath, string propertyName, T value)
        {
            var store = (IVsWritableSettingsStore)GetSettingsStore(true);
            int res = 0;

            switch (value)
            {
                case int v:
                    res = store.SetInt(collectionPath, propertyName, v);
                    break;
                case bool v:
                    res = store.SetBool(collectionPath, propertyName, v ? 1 : 0);
                    break;
                case string v:
                    res = store.SetString(collectionPath, propertyName, v);
                    break;
                default:
                    throw new NotImplementedException($"Type {typeof(T)} is not supported for settings");
            }

            if (res != 0)
                throw new Win32Exception();
        }

        private IVsSettingsStore GetSettingsStore(bool writable = false)
        {
            IVsSettingsStore ret;
            int result;

            if (!writable)
                result = _settingsManager.GetReadOnlySettingsStore((uint)SettingsScope.UserSettings, out ret);
            else
            {
                result = _settingsManager.GetWritableSettingsStore((uint)SettingsScope.UserSettings, out var wret);
                ret = wret;
            }

            if (result != 0)
                throw new Win32Exception();
            return ret;
        }
    }
}
