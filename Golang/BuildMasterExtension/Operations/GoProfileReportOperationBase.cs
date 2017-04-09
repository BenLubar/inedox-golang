using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Documentation;
using Inedo.Extensions.Golang.SuggestionProviders;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.Operations
{
    public abstract class GoProfileReportOperationBase : GoReportOperationBase
    {
        [Required]
        [DisplayName("Profile file")]
        [ScriptAlias("ProfileFile")]
        public string ProfileFile { get; set; }

        [DisplayName("Program executable")]
        [ScriptAlias("BinaryFile")]
        public string BinaryFile { get; set; }

        [DisplayName("Previous profile file")]
        [ScriptAlias("CompareProfileFile")]
        public string CompareProfileFile { get; set; }

        [DisplayName("Ignore negative differences")]
        [ScriptAlias("IgnoreNegativeDifferences")]
        public bool IgnoreNegativeDifferences { get; set; }

        [DisplayName("Granularity")]
        [SuggestibleValue(typeof(ProfileGranularitySuggestionProvider))]
        [DefaultValue("functions")]
        [ScriptAlias("Granularity")]
        public string Granularity { get; set; } = "functions";

        [DisplayName("Cumulative")]
        [ScriptAlias("Cumulative")]
        public bool Cumulative { get; set; }

        [DisplayName("Sample type (memory)")]
        [SuggestibleValue(typeof(MemoryProfileSuggestionProvider))]
        [ScriptAlias("MemorySampleType")]
        public string MemorySampleType { get; set; }

        [DisplayName("Sample type (contention)")]
        [SuggestibleValue(typeof(ContentionProfileSuggestionProvider))]
        [ScriptAlias("ContentionSampleType")]
        public string ContentionSampleType { get; set; }

        [DisplayName("Max nodes")]
        [ScriptAlias("MaxNodes")]
        public int? MaxNodes { get; set; }

        [DisplayName("Node fraction")]
        [ScriptAlias("NodeFraction")]
        public float? NodeFraction { get; set; }

        [DisplayName("Edge fraction")]
        [ScriptAlias("EdgeFraction")]
        public float? EdgeFraction { get; set; }

        [DisplayName("Show runtime frames")]
        [ScriptAlias("ShowRuntime")]
        public bool ShowRuntime { get; set; }

        [DisplayName("Focus RegEx")]
        [ScriptAlias("FocusPattern")]
        public string FocusPattern { get; set; }

        [DisplayName("Ignore RegEx")]
        [ScriptAlias("IgnorePattern")]
        public string IgnorePattern { get; set; }

        [DisplayName("Tag focus RegEx")]
        [ScriptAlias("TagFocusPattern")]
        public string TagFocusPattern { get; set; }

        [DisplayName("Tag ignore RegEx")]
        [ScriptAlias("TagIgnorePattern")]
        public string TagIgnorePattern { get; set; }

        [DisplayName("Convert to unit")]
        [ScriptAlias("ConvertToUnit")]
        public string ConvertToUnit { get; set; }

        [DisplayName("Divide by")]
        [ScriptAlias("DivideBy")]
        public float? DivideBy { get; set; }

        protected abstract IEnumerable<string> ProfileArgs { get; }

        protected override async Task GenerateReportAsync(IOperationExecutionContext context, string outputPath)
        {
            await this.ExecuteCommandLineAsync(context, "tool", new[]
            {
                "pprof", "-output", outputPath,
                this.CompareProfileFile == null ? null : "-base",
                this.CompareProfileFile,
                this.IgnoreNegativeDifferences ? "-drop_negative" : null,
                ProfileGranularitySuggestionProvider.Granularities.Contains(this.Granularity) ? $"-{this.Granularity}" : null,
                this.Cumulative ? "-cum" : null,
                MemoryProfileSuggestionProvider.SampleTypes.Contains(this.MemorySampleType ?? "") ? $"-{this.MemorySampleType}" : null,
                ContentionProfileSuggestionProvider.SampleTypes.Contains(this.ContentionSampleType ?? "") ? $"-{this.ContentionSampleType}" : null,
                this.MaxNodes.HasValue ? "-nodecount" : null,
                this.MaxNodes.HasValue ? this.MaxNodes.Value.ToString() : null,
                this.NodeFraction.HasValue ? "-nodefraction" : null,
                this.NodeFraction.HasValue ? this.NodeFraction.Value.ToString() : null,
                this.EdgeFraction.HasValue ? "-edgefraction" : null,
                this.EdgeFraction.HasValue ? this.EdgeFraction.Value.ToString() : null,
                this.ShowRuntime ? "-runtime" : null,
                string.IsNullOrWhiteSpace(this.FocusPattern) ? null : "-focus",
                string.IsNullOrWhiteSpace(this.FocusPattern) ? null : this.FocusPattern,
                string.IsNullOrWhiteSpace(this.IgnorePattern) ? null : "-ignore",
                string.IsNullOrWhiteSpace(this.IgnorePattern) ? null : this.IgnorePattern,
                string.IsNullOrWhiteSpace(this.TagFocusPattern) ? null : "-tagfocus",
                string.IsNullOrWhiteSpace(this.TagFocusPattern) ? null : this.TagFocusPattern,
                string.IsNullOrWhiteSpace(this.TagIgnorePattern) ? null : "-tagignore",
                string.IsNullOrWhiteSpace(this.TagIgnorePattern) ? null : this.TagIgnorePattern,
                string.IsNullOrWhiteSpace(this.ConvertToUnit) ? null : "-unit",
                string.IsNullOrWhiteSpace(this.ConvertToUnit) ? null : this.ConvertToUnit,
                this.DivideBy.HasValue ? "-divide_by" : null,
                this.DivideBy.HasValue ? this.DivideBy.Value.ToString() : null
            }.Concat(this.ProfileArgs).Concat(new[] { this.BinaryFile, this.ProfileFile })).ConfigureAwait(false);
        }
    }
}
