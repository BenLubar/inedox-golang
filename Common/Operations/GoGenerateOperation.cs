#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
#endif
using Inedo.Documentation;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Update generated Go files")]
    [Description("Runs go generate on a package.")]
    [ScriptNamespace("Golang")]
    [ScriptAlias("Go-Generate")]
    [Tag("go")]
    public sealed class GoGenerateOperation : GoOperationBase
    {
        [Required]
        [DisplayName("Package path")]
        [Description("The import path of the package to update generated files.")]
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