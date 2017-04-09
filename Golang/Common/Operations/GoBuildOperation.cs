#if BuildMaster
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
#endif
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Compile Go Package")]
    [Description("Builds a Go package using the go build command.")]
    [ScriptAlias("Build")]
    public sealed class GoBuildOperation : GoBuildOperationBase
    {
        [Required]
        [DisplayName("Package path")]
        [Description("The import path of the package to compile.")]
        [ScriptAlias("Package")]
        public string Package { get; set; }

        [DisplayName("Output filename")]
        [Description("The name of the file to create, relative to the package folder. The type of the file depends on the build mode and whether the package is a command.")]
        [ScriptAlias("Output")]
        public string OutputName { get; set; }

#if BuildMaster
        [DisplayName("Create artifact")]
        [ScriptAlias("ArtifactName")]
        [PlaceholderText("Do not create artifact")]
        public string ArtifactName { get; set; }
#endif

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var packagePath = fileOps.CombinePath(context.ResolvePath(this.GoPath), "src", this.Package);
            var outputName = string.IsNullOrWhiteSpace(this.OutputName) ? null : fileOps.CombinePath(packagePath, this.OutputName);

            await this.ExecuteCommandLineAsync(context, "build", this.BuildArgs.Concat(new[] { "-x", "-i", outputName == null ? null : "-o", outputName, "--", this.Package })).ConfigureAwait(false);

#if BuildMaster
            if (outputName == null && !string.IsNullOrWhiteSpace(this.ArtifactName))
            {
                this.LogWarning("Output filename must be set for an artifact to be created.");
            }
            else if (!context.Simulation && !string.IsNullOrWhiteSpace(this.ArtifactName))
            {
                var artifactId = new ArtifactIdentifier(context.ApplicationId.Value, context.ReleaseNumber, context.BuildNumber, context.DeployableId, this.ArtifactName);
                using (var artifactBuilder = new ArtifactBuilder(artifactId, context.ExecutionId))
                {
                    artifactBuilder.RootPath = packagePath;
                    await artifactBuilder.AddFileAsync(outputName, DateTime.Now, fileOps).ConfigureAwait(false);
                    artifactBuilder.Commit();
                }
            }
#endif
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription("Compile Go package ", new Hilite(config[nameof(Package)])));
        }
    }
}