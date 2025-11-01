using System;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;
using Tvl.VisualStudio.JustMyCodeToggle.Managers;


namespace Tvl.VisualStudio.JustMyCodeToggle.Commands
{


    /// <summary>
    /// Allows using multiple/dynamic setters IE depending on VS version, or if project supports CPS or not,  if multiple setters are returned from GetSetters the first is used for GetSetting
    /// </summary>
    /// <typeparam name="SETTING_TYPE"></typeparam>
    /// <typeparam name="OUR_CLASS"></typeparam>
    internal abstract class ToggleSettingDynamicSetterCmd<SETTING_TYPE> : ToggleSettingCmd<SETTING_TYPE>, ISetSettingInterface<SETTING_TYPE>
    {



        protected delegate SETTING_TYPE TransformValDelegate(ISetSettingInterface<SETTING_TYPE> setter, SETTING_TYPE val, bool forGetSetting);
        protected ToggleSettingDynamicSetterCmd(SETTING_TYPE enabled_val, SETTING_TYPE disabled_val, Func<ISetSettingInterface<SETTING_TYPE>, SETTING_TYPE, SETTING_TYPE> TransformVal = null) : base(null, enabled_val, disabled_val)
        {
            this.setter = this;
            this.TransformVal = TransformVal;
        }

        protected abstract Task<ISetSettingInterface<SETTING_TYPE>[]> GetSetters();
        public Func<ISetSettingInterface<SETTING_TYPE>, SETTING_TYPE, SETTING_TYPE> TransformVal { get; }

        public async Task<SETTING_TYPE> GetSetting()
        {
            var setters = await GetSetters();
            var ret = await setters[0].GetSetting();
            if (TransformVal != null)
                ret = TransformVal(setters[0], ret);
            return ret;
        }

        public async Task SetSetting(SETTING_TYPE val)
        {
            var setters = await GetSetters();
            foreach (var setter in setters)
            {
                var toSet = val;
                if (TransformVal != null)
                    toSet = TransformVal(setter, val);
                await setter.SetSetting(toSet);

            }
        }
    }
    public interface IOurCommand
    {
        Task Execute();
        Task Initialize();
        IButtonNative Native { set; }
    }
    public interface IButtonNative
    {
        IOurCommand OurCommand { set; }
        bool IsChecked();
        void SetChecked(bool isChecked);
        void SetEnabled(bool isEnabled);
    }
    internal abstract class OurExtensibilityToggleButton<CMD_CLASS> : ToggleCommand, IButtonNative where CMD_CLASS : class, IOurCommand, new()
    {
        public IOurCommand OurCommand { set; get; }
        public OurExtensibilityToggleButton()
        {
            OurCommand = new CMD_CLASS();
            OurCommand.Native = this;
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await OurCommand?.Initialize();
            await base.InitializeAsync(cancellationToken);
        }

        public void SetChecked(bool isChecked) => this.IsChecked = isChecked;

        public void SetEnabled(bool isEnabled) => this.SetEnabledState(isEnabled);

        bool IButtonNative.IsChecked() => this.IsChecked;

        override public async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            await (OurCommand?.Execute() ?? Task.CompletedTask);
            // base is abstract for this
        }
    }
    internal abstract class OurOLEButton<CMD_CLASS,OUR_CLASS> : BaseCommand<OUR_CLASS>, IButtonNative where OUR_CLASS : class, new()
        where CMD_CLASS : class, IOurCommand, new()
    {
        public OurOLEButton()
        {
            OurCommand = new CMD_CLASS();
            OurCommand.Native = this;
        }
        public IOurCommand OurCommand { protected get; set; }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await (OurCommand?.Execute() ?? Task.CompletedTask);
            await base.ExecuteAsync(e);
        }
        public bool IsChecked() => this.Command.Checked;
        public void SetChecked(bool isChecked) => this.Command.Checked = isChecked;
        public void SetEnabled(bool isEnabled) => this.Command.Enabled = isEnabled;
        override protected async Task InitializeCompletedAsync()
        {
            await (OurCommand?.Initialize() ?? Task.CompletedTask);
            await base.InitializeCompletedAsync();
        }
    }
    internal abstract class ToggleSettingCmd<SETTING_TYPE> : IOurCommand
    {


        protected virtual ISetSettingInterface<SETTING_TYPE> setter { get; set; }
        protected virtual SETTING_TYPE enabled_val { get; set; }
        protected virtual SETTING_TYPE disabled_val { get; set; }
        public IButtonNative Native
        {
            set
            {
                field = value;
                //field.OurCommand = this;
            }
            protected get;
        }

        protected virtual async Task SyncCheckedToCurVal()

        {
            try
            {
                this.SetCheckedToMatch(await setter.GetSetting());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JMC ToggleSettingCmd {this.GetType()} StartProjChanged exception: {ex}");
            }
        }

        public ToggleSettingCmd(ISetSettingInterface<SETTING_TYPE> setter, SETTING_TYPE enabled_val, SETTING_TYPE disabled_val)
        {
            this.setter = setter;
            this.enabled_val = enabled_val;
            this.disabled_val = disabled_val;
        }

        public virtual async Task Execute()
        {
            try
            {

                var cur = await setter.GetSetting();
                cur = cur.Equals(disabled_val) ? enabled_val : disabled_val;
                await setter.SetSetting(cur);
                SetCheckedToMatch(cur);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JMC ToggleSettingCmd {this.GetType()} ExecuteAsync exception: {ex}");

            }


        }
        protected virtual void SetCheckedToMatch(SETTING_TYPE cur)
        {
            Native.SetChecked(!cur.Equals(disabled_val));
        }
        public async Task Initialize()
        {
            try
            {
                await SyncCheckedToCurVal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JMC ToggleSettingCmd InitializeCompletedAsync exception: {ex}");

            }

        }
    }
}
