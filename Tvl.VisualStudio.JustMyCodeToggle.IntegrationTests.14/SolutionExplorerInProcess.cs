// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using EnvDTE80;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using _DTE = EnvDTE._DTE;
    using DTE = EnvDTE.DTE;

    internal partial class SolutionExplorerInProcess
    {
        private const string CSharpLanguageName = "CSharp";
        private const string ConsoleApplicationTemplateName = "ConsoleApplication.zip";

        public async Task CreateCSharpConsoleApplicationAsync(
            string solutionDirectory,
            string solutionName,
            string projectName,
            CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await CloseSolutionAsync(cancellationToken);

            if (Directory.Exists(solutionDirectory))
            {
                Directory.Delete(solutionDirectory, recursive: true);
            }

            Directory.CreateDirectory(solutionDirectory);

            IVsSolution solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.CreateSolution(solutionDirectory, solutionName, (uint)__VSCREATESOLUTIONFLAGS.CSF_SILENT));

            string projectTemplate = await GetProjectTemplatePathAsync(ConsoleApplicationTemplateName, CSharpLanguageName, cancellationToken);
            string projectDirectory = Path.Combine(solutionDirectory, projectName);
            Directory.CreateDirectory(projectDirectory);

            await AddProjectFromTemplateAsync(projectTemplate, projectDirectory, projectName, cancellationToken);
            VerifyProjectIsLoaded(solution);

            ErrorHandler.ThrowOnFailure(solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, null, 0));
        }

        private async Task AddProjectFromTemplateAsync(
            string projectTemplate,
            string projectDirectory,
            string projectName,
            CancellationToken cancellationToken)
        {
            // VS 2015-2019 only expose project template instantiation through DTE.
            DTE dte = await GetRequiredGlobalServiceAsync<_DTE, DTE>(cancellationToken);
            dte.Solution.AddFromTemplate(projectTemplate, projectDirectory, projectName, Exclusive: false);
        }

        private async Task<string> GetProjectTemplatePathAsync(
            string templateName,
            string languageName,
            CancellationToken cancellationToken)
        {
            DTE dte = await GetRequiredGlobalServiceAsync<_DTE, DTE>(cancellationToken);
            Solution2 solution = (Solution2)dte.Solution;
            return solution.GetProjectTemplate(templateName, languageName);
        }

        private static void VerifyProjectIsLoaded(IVsSolution solution)
        {
            Guid projectType = Guid.Empty;
            ErrorHandler.ThrowOnFailure(
                solution.GetProjectEnum(
                    (uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION,
                    ref projectType,
                    out IEnumHierarchies projects));

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            ErrorHandler.ThrowOnFailure(projects.Next(1, hierarchy, out uint fetched));
            if (fetched == 0 || hierarchy[0] == null)
            {
                throw new InvalidOperationException("The project template did not create a loaded project.");
            }
        }
    }
}
