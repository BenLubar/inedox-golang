using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Documentation;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Generate Go Coverage Report")]
    [ScriptNamespace("Golang")]
    [ScriptAlias("Go-Cover-Report")]
    [Tag("go")]
    public sealed class GoCoverReportOperation : GoReportOperationBase
    {
        [Required]
        [DisplayName("Coverage profile")]
        [ScriptAlias("CoverProfile")]
        public string CoverFile { get; set; }

        protected override async Task GenerateReportAsync(IOperationExecutionContext context, string outputPath)
        {
            await this.ExecuteCommandLineAsync(context, "tool", new[] { "cover", "-html", this.CoverFile, "-o", outputPath }).ConfigureAwait(false);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription("Generate test coverage report ", new Hilite(config[nameof(OutputName)]), " from ", new Hilite(config[nameof(CoverFile)])));
        }
    }
}
