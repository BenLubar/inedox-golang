#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Web.Controls;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Web.Controls;
#endif
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.SuggestionProviders
{
    /// <summary>
    /// An implementation of ISuggestionProvider that returns a predefined list of suggestions.
    /// </summary>
    public abstract class ConstantSuggestionProvider : ISuggestionProvider
    {
        /// <summary>
        /// The suggestions to return
        /// </summary>
        protected abstract IEnumerable<string> Suggestions { get; }

        public Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            return Task.FromResult(this.Suggestions);
        }
    }
}
