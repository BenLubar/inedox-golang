using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Compile Go Package")]
    [Description("Builds a Go package using the go build command.")]
    [ScriptAlias("Build")]
    public sealed class GoBuildOperation : GoBuildOperationBase
    {
        [Required]
        [DisplayName("Package path")]
        [Description("The import path of the package to compile. " + GoUtils.ImportPathDescription)]
        [ScriptAlias("Package")]
        public string Package { get; set; }

        [DisplayName("Output filename")]
        [Description("The name of the file to create, relative to the package folder. The type of the file depends on the build mode and whether the package is a command.")]
        [ScriptAlias("Output")]
        public string OutputName { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var packagePath = fileOps.CombinePath(context.ResolvePath(this.GoPath), "src", this.Package);
            var outputName = string.IsNullOrWhiteSpace(this.OutputName) ? null : fileOps.CombinePath(packagePath, this.OutputName);

            await this.ExecuteCommandLineAsync(context, "build", this.BuildArgs.Concat(new[] { "-x", "-i", outputName == null ? null : "-o", outputName, "--", this.Package })).ConfigureAwait(false);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription("Compile Go package ", new Hilite(config[nameof(Package)])));
        }
    }
}
