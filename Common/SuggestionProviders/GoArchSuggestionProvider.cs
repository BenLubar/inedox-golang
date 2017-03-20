using System.Collections.Generic;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class GoArchSuggestionProvider : ConstantSuggestionProvider
    {
        protected override IEnumerable<string> Suggestions => new[]
        {
            "386",
            "amd64",
            "amd64p32",
            "arm",
            "arm64",
            "mips",
            "mipsle",
            "mips64",
            "mips64le",
            "ppc64",
            "ppc64le",
            "s390x"
        };
    }
}
