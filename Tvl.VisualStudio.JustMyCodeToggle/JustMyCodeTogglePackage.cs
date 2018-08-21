// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using IMenuCommandService = System.ComponentModel.Design.IMenuCommandService;
    using Task = System.Threading.Tasks.Task;

    [Guid(JustMyCodeToggleConstants.GuidJustMyCodeTogglePackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource(1000, 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    internal class JustMyCodeTogglePackage : AsyncPackage
    {
        private readonly OleMenuCommand _command;

        public JustMyCodeTogglePackage()
        {
            var id = new CommandID(JustMyCodeToggleConstants.GuidJustMyCodeToggleCommandSet, JustMyCodeToggleConstants.CmdidJustMyCodeToggle);
            EventHandler invokeHandler = HandleInvokeJustMyCodeToggle;
            EventHandler changeHandler = HandleChangeJustMyCodeToggle;
            EventHandler beforeQueryStatus = HandleBeforeQueryStatusJustMyCodeToggle;
            _command = new OleMenuCommand(invokeHandler, changeHandler, beforeQueryStatus, id);
        }

        public EnvDTE.DTE ApplicationObject
        {
            get
            {
                return GetService(typeof(EnvDTE._DTE)) as EnvDTE.DTE;
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var mcs = (IMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService));
            Assumes.Present(mcs);
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
