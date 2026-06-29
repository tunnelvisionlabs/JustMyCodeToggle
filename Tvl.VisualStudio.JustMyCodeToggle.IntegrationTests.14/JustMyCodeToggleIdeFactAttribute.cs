// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using Xunit;
    using Xunit.Sdk;

#if NET472
    [XunitTestCaseDiscoverer("Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests.JustMyCodeToggleIdeFactDiscoverer", "Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests.17")]
#else
    [XunitTestCaseDiscoverer("Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests.JustMyCodeToggleIdeFactDiscoverer", "Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests.14")]
#endif
    public sealed class JustMyCodeToggleIdeFactAttribute : IdeFactAttribute
    {
    }
}
