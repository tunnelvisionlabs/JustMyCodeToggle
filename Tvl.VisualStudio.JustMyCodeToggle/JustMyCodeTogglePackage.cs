// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;
    using IMenuCommandService = System.ComponentModel.Design.IMenuCommandService;

    [Guid(JustMyCodeToggleConstants.guidJustMyCodeTogglePackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource(1000, 1)]
    internal class JustMyCodeTogglePackage : Package
    {
        private readonly OleMenuCommand _command;

        public JustMyCodeTogglePackage()
        {
            var id = new CommandID(JustMyCodeToggleConstants.guidJustMyCodeToggleCommandSet, JustMyCodeToggleConstants.cmdidJustMyCodeToggle);
            EventHandler invokeHandler = HandleInvokeJustMyCodeToggle;
            EventHandler changeHandler = HandleChangeJustMyCodeToggle;
            EventHandler beforeQueryStatus = HandleBeforeQueryStatusJustMyCodeToggle;
            _command = new OleMenuCommand(invokeHandler, changeHandler, beforeQueryStatus, id);
        }

        public SVsServiceProvider ServiceProvider
        {
            get
            {
                return new VsServiceProviderWrapper(this);
            }
        }

        public EnvDTE.DTE ApplicationObject
        {
            get
            {
                return ServiceProvider.GetService(typeof(EnvDTE._DTE)) as EnvDTE.DTE;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            var mcs = (IMenuCommandService)GetService(typeof(IMenuCommandService));
            mcs.AddCommand(_command);
        }

        private void HandleInvokeJustMyCodeToggle(object sender, EventArgs e)
        {
            try
            {
                EnvDTE.Property enableJustMyCode = ApplicationObject.get_Properties("Debugging", "General").Item("EnableJustMyCode");
                if (enableJustMyCode.Value is bool value)
                {
                    enableJustMyCode.Value = !value;
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
            }
        }

        private void HandleChangeJustMyCodeToggle(object sender, EventArgs e)
        {
        }

        private void HandleBeforeQueryStatusJustMyCodeToggle(object sender, EventArgs e)
        {
            try
            {
                _command.Supported = true;

                EnvDTE.Property enableJustMyCode = ApplicationObject.get_Properties("Debugging", "General").Item("EnableJustMyCode");
                if (enableJustMyCode.Value is bool value)
                {
                    _command.Checked = value;
                }

                _command.Enabled = true;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                _command.Supported = false;
                _command.Enabled = false;
            }
        }
    }
}
