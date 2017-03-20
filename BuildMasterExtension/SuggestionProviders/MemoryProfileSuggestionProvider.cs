using System.Collections.Generic;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class MemoryProfileSuggestionProvider : ConstantSuggestionProvider
    {
        internal static readonly IEnumerable<string> SampleTypes = new[]
        {
            "inuse_space",
            "inuse_objects",
            "alloc_space",
            "alloc_objects"
        };

        protected override IEnumerable<string> Suggestions => SampleTypes;
    }
}
