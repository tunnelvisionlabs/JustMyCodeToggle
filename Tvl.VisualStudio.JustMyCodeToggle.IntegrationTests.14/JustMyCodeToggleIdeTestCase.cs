// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.Threading;

    public sealed class JustMyCodeToggleIdeTestCase : IdeTestCaseBase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", true)]
        public JustMyCodeToggleIdeTestCase()
        {
        }

        public JustMyCodeToggleIdeTestCase(
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay defaultMethodDisplay,
            TestMethodDisplayOptions defaultMethodDisplayOptions,
            ITestMethod testMethod,
            VisualStudioInstanceKey visualStudioInstanceKey,
            object[] testMethodArguments = null)
            : base(
                diagnosticMessageSink,
                defaultMethodDisplay,
                defaultMethodDisplayOptions,
                testMethod,
                visualStudioInstanceKey,
                testMethodArguments)
        {
        }

        public override Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            TestCaseRunner<IXunitTestCase> runner;
            if (this.SkipReason != null)
            {
                runner = new XunitTestCaseRunner(
                    this,
                    this.DisplayName,
                    this.SkipReason,
                    constructorArguments,
                    this.TestMethodArguments,
                    messageBus,
                    aggregator,
                    cancellationTokenSource);
            }
            else
            {
                runner = new JustMyCodeToggleIdeTestCaseRunner(
                    this,
                    this.DisplayName,
                    this.SkipReason,
                    constructorArguments,
                    this.TestMethodArguments,
                    messageBus,
                    aggregator,
                    cancellationTokenSource);
            }

            return runner.RunAsync();
        }
    }
}
