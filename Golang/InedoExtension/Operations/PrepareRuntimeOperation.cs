using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Golang.SuggestionProviders;
using Inedo.Web;

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
        [Description("The version of Go to download. By default, this is the latest version.")]
        [SuggestableValue(typeof(GoVersionSuggestionProvider))]
        public string Version { get; set; } = "latest";

        [Output]
        [DisplayName("Resolved version")]
        [ScriptAlias(nameof(ResolvedVersion))]
        [Description("The version of Go that was actually downloaded. Can be different than Version if Version is not a specific number.")]
        [PlaceholderText("eg. $GoVersion")]
        public string ResolvedVersion { get; set; }

        [Output]
        [DisplayName("Go executable path")]
        [ScriptAlias(nameof(ExecutablePath))]
        [Description("A variable where the path to the <code>go</code> command's executable will be stored.")]
        [PlaceholderText("eg. $GoExe")]
        public string ExecutablePath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var version = await GoUtils.PrepareGoAsync(this, context, this.Version).ConfigureAwait(false);
            this.ResolvedVersion = version.Version;
            this.ExecutablePath = version.ExecutablePath;
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
