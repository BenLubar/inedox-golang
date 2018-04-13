using System.Collections.Generic;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class GoOSSuggestionProvider : ConstantSuggestionProvider
    {
        protected override IEnumerable<string> Suggestions => new[]
        {
            "windows",
            "linux",
            "darwin",
            "freebsd",
            "netbsd",
            "openbsd",
            "solaris",
            "plan9"
        };
    }
}
