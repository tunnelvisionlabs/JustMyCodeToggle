using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tvl.VisualStudio.JustMyCodeToggle.Managers;

namespace Tvl.VisualStudio.JustMyCodeToggle
{
    /// <summary>
    /// We need this interface/service registration to get ahold of a valid  VisualStudioExtensibility instance.  We don't use this service though because it just proxies everything which we don't need for inproc.  Once our service is created we can access it directly.
    /// </summary>
    public interface IExtensibilitySettingManager
    {

        public Task<VisualStudioExtensibility> GetExtensibility();

        public static class Configuration
        {
            public const string ServiceName = "JustMyCodeToggle.ExtensibilitySettingManager";
            public static readonly Version ServiceVersion = new(1, 0);

            public static readonly ServiceMoniker ServiceMoniker = new(ServiceName, ServiceVersion);

            public static ServiceRpcDescriptor ServiceDescriptor => new ServiceJsonRpcDescriptor(
                ServiceMoniker,
                ServiceJsonRpcDescriptor.Formatters.MessagePack,
                ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader);
        }

    }


    /// <summary>
    /// Extension entrypoint for the VisualStudio.Extensibility extension.
    /// </summary>
    [VisualStudioContribution]
    internal class ExtensibilityExtensionEntrypoint : Extension
    {
        //public ExtensibilityExtensionEntrypoint(){

        //         Debug.WriteLine("Inited here...");
        //     }

        /// <inheritdoc />
        public override ExtensionConfiguration ExtensionConfiguration => new()
        {
            RequiresInProcessHosting = true,
            LoadedWhen = ActivationConstraint.Or( ActivationConstraint.SolutionState(SolutionState.Exists), ActivationConstraint.SolutionState(SolutionState.NoSolution))
        };


        protected override void InitializeServices(IServiceCollection serviceCollection)
        {

            Debug.WriteLine("ExtensionEntrypoint InitializeServices called!");
            serviceCollection.ProfferBrokeredService(ExtensibilitySettingManager.BrokeredServiceConfiguration, IExtensibilitySettingManager.Configuration.ServiceDescriptor);
            base.InitializeServices(serviceCollection);



        }
    }
}
