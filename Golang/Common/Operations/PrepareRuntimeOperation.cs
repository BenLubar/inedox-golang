#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Web.Controls;
#endif
using Inedo.Documentation;
using Inedo.Extensions.Golang.SuggestionProviders;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Prepare Go Runtime")]
    [Description("Ensure that a specific version of Go's compiler and standard library is available on the current server.")]
    [ScriptAlias("Prepare-Runtime")]
    [ScriptNamespace("Golang")]
    [Tag("go")]
    public sealed class PrepareRuntimeOperation : ExecuteOperation
    {
        [DisplayName("Version")]
        [ScriptAlias("Version")]
        [DefaultValue("latest")]
        [SuggestibleValue(typeof(GoVersionSuggestionProvider))]
        public string Version { get; set; } = "latest";

        [Output]
        [DisplayName("Go executable path")]
        [ScriptAlias(nameof(ExecutablePath))]
        [PlaceholderText("eg. $GoExe")]
        public string ExecutablePath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var version = await GoUtils.PrepareGoAsync(this, context, this.Version).ConfigureAwait(false);
            this.ExecutablePath = version.Item1;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var extended = new RichDescription();

            if (config.OutArguments.ContainsKey(nameof(ExecutablePath)))
            {
                extended.AppendContent("storing executable path in ", new Hilite(config.OutArguments[nameof(ExecutablePath)].ToString()));
            }

            return new ExtendedRichDescription(
                string.Equals(AH.CoalesceString(config[nameof(Version)], "latest"), "latest", StringComparison.OrdinalIgnoreCase) ?
                    new RichDescription("Download ", new Hilite("latest"), " Go version") :
                    new RichDescription("Download Go version ", new Hilite(config[nameof(Version)])),
                extended
            );
        }
    }
}
