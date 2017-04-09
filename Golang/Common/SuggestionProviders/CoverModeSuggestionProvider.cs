using System.Collections.Generic;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class CoverModeSuggestionProvider : ConstantSuggestionProvider
    {
        protected override IEnumerable<string> Suggestions => new[]
        {
            "set",
            "count",
            "atomic"
        };
    }
}
