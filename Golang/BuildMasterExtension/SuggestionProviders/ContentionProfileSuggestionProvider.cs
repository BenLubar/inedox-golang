using System.Collections.Generic;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class ContentionProfileSuggestionProvider : ConstantSuggestionProvider
    {
        internal static readonly IEnumerable<string> SampleTypes = new[]
        {
            "total_delay",
            "contentions",
            "mean_delay"
        };

        protected override IEnumerable<string> Suggestions => SampleTypes;
    }
}
