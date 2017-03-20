using System;
using System.Collections.Generic;
using System.Text;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class CompilerSuggestionProvider : ConstantSuggestionProvider
    {
        protected override IEnumerable<string> Suggestions => new[]
        {
            "gc",
            "gccgo"
        };
    }
}
