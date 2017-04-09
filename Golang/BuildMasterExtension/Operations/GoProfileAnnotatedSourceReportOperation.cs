using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Documentation;
using System.Collections.Generic;
using System.ComponentModel;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Annotate Go Source Code")]
    [ScriptNamespace("Golang")]
    [ScriptAlias("Go-Annotated-Source-Report")]
    [Tag("go")]
    public sealed class GoProfileAnnotatedSourceReportOperation : GoProfileReportOperationBase
    {
        [Required]
        [DisplayName("Search RegEx")]
        [ScriptAlias("SearchPattern")]
        public string SearchPattern { get; set; }

        protected override IEnumerable<string> ProfileArgs => new[] { "-weblist", this.SearchPattern };

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription("Generate profile annotated source code report ", new Hilite(config[nameof(OutputName)]), " from ", new Hilite(config[nameof(ProfileFile)])));
        }
    }
}
