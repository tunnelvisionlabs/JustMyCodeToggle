// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    using System;

    using System.Runtime.InteropServices;
    using System.Threading;
    using Community.VisualStudio.Toolkit;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Extensibility;
    using Microsoft.VisualStudio.Extensibility.Shell;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Tvl.VisualStudio.JustMyCodeToggle.Managers;
    using Task = System.Threading.Tasks.Task;

    [Guid(PackageGuids.guidJustMyCodeTogglePackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    internal class JustMyCodeTogglePackage : ToolkitPackage
    {

        protected ProjectEventHandler ProjectEventHandler;

        private StartupProjectManager _startupProjectManager;

        internal T RegisterService<T>(T service){
            AddService(typeof(T),(_,_,_) => Task.FromResult<object>(service),promote:true);
            return service;
        }
        public JustMyCodeTogglePackage(){
            instance = this;
        }
        public static JustMyCodeTogglePackage instance;
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {

            //await Helpers.Init();
            // Initialize managers
            _startupProjectManager = RegisterService(new StartupProjectManager());
            RegisterService(new LaunchProfileManager(_startupProjectManager));
            RegisterService(new SettingsStoreManager((IVsSettingsManager)await VS.Services.GetSettingsManagerAsync()));
            RegisterService(new DteManager());
            RegisterService(new DebuggerServiceManager());
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await this.RegisterCommandsAsync();
            await base.InitializeAsync(cancellationToken, progress);
            
            
            var reqType = typeof(VisualStudioExtensibility);
            var asm = reqType.Assembly;
            var globalSolutionSvc = await VS.GetServiceAsync<SVsSolution, IVsSolution>();
                    //VisualStudioExtensibility extensibility = await this.GetServiceAsync<VisualStudioExtensibility, VisualStudioExtensibility>();
                    //await extensibility.Shell().ShowPromptAsync("Hello from in-proc", PromptOptions.OK, cancellationToken);


            ProjectEventHandler = new(_startupProjectManager);
            globalSolutionSvc.AdviseSolutionEvents(ProjectEventHandler, out _);
            var monitor = await VS.GetServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>();
            monitor.AdviseSelectionEvents(ProjectEventHandler, out _);
            var bm = await VS.Services.GetSolutionBuildManagerAsync();
            bm.AdviseUpdateSolutionEvents(ProjectEventHandler, out _);

            AfterLoad();
        }


        private async void AfterLoad()
        {
            await Task.Delay(2000);
            _startupProjectManager.CheckStartupProjectChanged(true);
        }

    }
}
