using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using System.Diagnostics;
using Tvl.VisualStudio.JustMyCodeToggle.Managers;

namespace Tvl.VisualStudio.JustMyCodeToggle
{
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
        };

        
        protected override void InitializeServices(IServiceCollection serviceCollection)
        {

            Debug.WriteLine("ExtensionEntrypoint InitializeServices called!");
            
            base.InitializeServices(serviceCollection);
            


        }
    }
}
