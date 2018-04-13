using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Web;

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
