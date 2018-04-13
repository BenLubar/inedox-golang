using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Update generated Go files")]
    [Description("Runs go generate on a package.")]
    [ScriptAlias("Generate")]
    public sealed class GoGenerateOperation : GoOperationBase
    {
        [Required]
        [DisplayName("Package path")]
        [Description("The import path of the package to update generated files. " + GoUtils.ImportPathDescription)]
        [ScriptAlias("Package")]
        public string Package { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            await this.ExecuteCommandLineAsync(context, "generate", new[] { "--", this.Package }).ConfigureAwait(false);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription("Update generated files in Go package ", new Hilite(config[nameof(Package)])));
        }
    }
}