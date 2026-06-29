// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Extensibility.Testing;

    public abstract class JustMyCodeToggleIntegrationTestBase : AbstractIdeIntegrationTest
    {
        protected async Task RunWithUnexpectedModalDialogDetectionAsync(Func<Task> testBody)
        {
            string testStackTrace = new StackTrace(skipFrames: 1, fNeedFileInfo: true).ToString();

            // Reporting from test cleanup loses the useful exception through the harness IPC boundary.
            using (UnexpectedModalDialogMonitor monitor = UnexpectedModalDialogMonitor.StartForCurrentProcess(testStackTrace))
            {
                Task testTask = testBody();
                Task completedTask = await Task.WhenAny(testTask, monitor.FailureTask).ConfigureAwait(false);

                if (completedTask == monitor.FailureTask)
                {
                    Exception dialogException = await monitor.FailureTask.ConfigureAwait(false);

                    try
                    {
                        await testTask.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(dialogException.Message, ex);
                    }

                    throw dialogException;
                }

                await testTask.ConfigureAwait(false);
                monitor.ThrowIfDialogDetected();
            }
        }
    }
}
