// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using Xunit.Sdk;
    using Xunit.Threading;

    internal sealed class JustMyCodeToggleInProcessIdeTestRunner : InProcessIdeTestRunner
    {
        public JustMyCodeToggleInProcessIdeTestRunner(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            string skipReason,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(
                test,
                messageBus,
                testClass,
                constructorArguments,
                testMethod,
                testMethodArguments,
                skipReason,
                beforeAfterAttributes,
                aggregator,
                cancellationTokenSource)
        {
        }

        protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            string testStackTrace = this.TestClass.FullName + "." + this.TestMethod.Name
                + Environment.NewLine
                + new StackTrace(skipFrames: 1, fNeedFileInfo: true);

            using (UnexpectedModalDialogMonitor monitor = UnexpectedModalDialogMonitor.StartForCurrentProcess(testStackTrace))
            {
                Task<decimal> testTask = base.InvokeTestMethodAsync(aggregator);
                Task completedTask = await Task.WhenAny(testTask, monitor.FailureTask).ConfigureAwait(false);

                if (completedTask == monitor.FailureTask)
                {
                    await Task.WhenAny(testTask, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
                    throw await monitor.FailureTask.ConfigureAwait(false);
                }

                decimal result = await testTask.ConfigureAwait(false);
                monitor.ThrowIfDialogDetected();
                return result;
            }
        }
    }
}
