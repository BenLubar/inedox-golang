using System.Collections.Generic;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class ProfileGranularitySuggestionProvider : ConstantSuggestionProvider
    {
        internal static readonly IEnumerable<string> Granularities = new[]
        {
            "functions",
            "files",
            "lines",
            "addresses"
        };

        protected override IEnumerable<string> Suggestions => Granularities;
    }
}
