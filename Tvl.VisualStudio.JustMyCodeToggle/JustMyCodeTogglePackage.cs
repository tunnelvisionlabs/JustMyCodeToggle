// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Community.VisualStudio.Toolkit;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Extensibility;
    using Microsoft.VisualStudio.Extensibility.Shell;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.ServiceBroker;
    using Tvl.VisualStudio.JustMyCodeToggle.Managers;
    using Task = System.Threading.Tasks.Task;

    [Guid(PackageGuids.guidJustMyCodeTogglePackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    internal class JustMyCodeTogglePackage : ToolkitPackage
    {

        protected ProjectEventHandler ProjectEventHandler;

        private StartupProjectManager _startupProjectManager;

        internal T RegisterService<T>(T service)
        {
            AddService(typeof(T), (_, _, _) => Task.FromResult<object>(service), promote: true);
            return service;
        }
        public JustMyCodeTogglePackage()
        {
            instance = this;
        }
        public static JustMyCodeTogglePackage instance;
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {

            // Initialize managers
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _startupProjectManager = RegisterService(new StartupProjectManager());
            RegisterService(new LaunchProfileManager(_startupProjectManager));
            RegisterService(new SettingsStoreManager((IVsSettingsManager)await VS.Services.GetSettingsManagerAsync()));
            RegisterService(new DteManager());
            RegisterService(new DebuggerServiceManager());

            await this.RegisterCommandsAsync();
            await base.InitializeAsync(cancellationToken, progress);


            var reqType = typeof(VisualStudioExtensibility);
            var asm = reqType.Assembly;
            var globalSolutionSvc = await VS.GetServiceAsync<SVsSolution, IVsSolution>();
            ProjectEventHandler = new(_startupProjectManager);
            globalSolutionSvc.AdviseSolutionEvents(ProjectEventHandler, out _);
            var monitor = await VS.GetServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>();
            monitor.AdviseSelectionEvents(ProjectEventHandler, out _);
            var bm = await VS.Services.GetSolutionBuildManagerAsync();
            bm.AdviseUpdateSolutionEvents(ProjectEventHandler, out _);
            AttemptGetExtensibility(); // fire and forget
            AfterLoad();

        }
        /// <summary>
        /// right now this doesn't succeed until a solution is loaded https://github.com/microsoft/VSExtensibility/issues/533
        /// </summary>
        /// <returns></returns>
        public async void AttemptGetExtensibility()
        {
            while (true)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                var serviceBrokerContainer = await this.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                var serviceBroker = serviceBrokerContainer.GetFullAccessServiceBroker();

                var myBrokeredServiceProxy = await serviceBroker.GetProxyAsync<IExtensibilitySettingManager>(IExtensibilitySettingManager.Configuration.ServiceDescriptor, default);

                (myBrokeredServiceProxy as IDisposable)?.Dispose();
                if (myBrokeredServiceProxy != null)
                    return;
                else
                    await Task.Delay(2000);
            }

        }

        private async void AfterLoad()
        {
            await Task.Delay(2000);
            _startupProjectManager.CheckStartupProjectChanged(true);
        }

    }
}
