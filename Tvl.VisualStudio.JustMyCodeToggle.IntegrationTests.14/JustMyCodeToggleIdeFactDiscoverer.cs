// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using System.Collections.Generic;
    using Xunit.Abstractions;
    using Xunit.Sdk;
    using Xunit.Threading;

    public sealed class JustMyCodeToggleIdeFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink diagnosticMessageSink;
        private readonly IdeFactDiscoverer innerDiscoverer;

        public JustMyCodeToggleIdeFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;
            this.innerDiscoverer = new IdeFactDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            foreach (IXunitTestCase testCase in this.innerDiscoverer.Discover(discoveryOptions, testMethod, factAttribute))
            {
                IdeTestCaseBase ideTestCase = testCase as IdeTestCaseBase;
                if (ideTestCase == null)
                {
                    yield return testCase;
                    continue;
                }

                yield return new JustMyCodeToggleIdeTestCase(
                    this.diagnosticMessageSink,
                    ideTestCase.DefaultMethodDisplay,
                    ideTestCase.DefaultMethodDisplayOptions,
                    ideTestCase.TestMethod,
                    ideTestCase.VisualStudioInstanceKey,
                    ideTestCase.TestMethodArguments);
            }
        }
    }
}
