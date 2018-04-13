using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Web;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    public sealed class GoVersionSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var downloads = await GoUtils.PopulateGoDownloadsAsync(null, CancellationToken.None).ConfigureAwait(false);
            return new[] { "latest" }.Concat(downloads.
                Where(v => v.StartsWith("go") && v.EndsWith(".windows-amd64.zip")).
                Select(v => v.Substring(0, v.Length - ".windows-amd64.zip".Length).Substring("go".Length)).Reverse());
        }
    }
}
