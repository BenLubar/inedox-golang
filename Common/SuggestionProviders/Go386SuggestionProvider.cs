using System.Collections.Generic;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class Go386SuggestionProvider : ConstantSuggestionProvider
    {
        protected override IEnumerable<string> Suggestions => new[]
        {
            "387",
            "sse2"
        };
    }
}
