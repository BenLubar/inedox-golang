using System.Collections.Generic;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class BuildModeSuggestionProvider : ConstantSuggestionProvider
    {
        protected override IEnumerable<string> Suggestions => new[]
        {
            "default",
            "exe",
            "archive",
            "shared",
            "plugin",
            "c-archive",
            "c-shared",
            "pie"
        };
    }
}
