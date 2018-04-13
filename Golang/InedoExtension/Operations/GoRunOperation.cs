using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.IO;
using Inedo.Web.Plans.ArgumentEditors;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Run Go program from source code")]
    [Description("Compile and run a Go program in one step using the go run command.")]
    [ScriptAlias("Run")]
    public sealed class GoRunOperation : GoBuildOperationBase
    {
        [DisplayName("From directory")]
        [PlaceholderText("$WorkingDirectory")]
        [ScriptAlias("From")]
        [FilePathEditor]
        public string SourceDirectory { get; set; }

        [DisplayName("Include files")]
        [MaskingDescription]
        [DefaultValue("*.go")]
        [ScriptAlias("Include")]
        public IEnumerable<string> Includes { get; set; }
        [DisplayName("Exclude files")]
        [MaskingDescription]
        [ScriptAlias("Exclude")]
        public IEnumerable<string> Excludes { get; set; }

        [DisplayName("Arguments")]
        [ScriptAlias("Arguments")]
        public string Arguments { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = await context.Agent.GetServiceAsync<Inedo.Agents.IFileOperationsExecuter>().ConfigureAwait(false);
            var args = new List<string>();
            var directory = context.ResolvePath(this.SourceDirectory);
            foreach (var info in await fileOps.GetFileSystemInfosAsync(directory, new MaskingContext(this.Includes, this.Excludes)).ConfigureAwait(false))
            {
                // go run assumes the first non-*.go argument is the start of the program arguments.
                if (info is SlimFileInfo && info.Name.EndsWith(".go"))
                {
                    args.Add(info.FullName);
                }
            }

            await this.ExecuteCommandLineAsync(context, "run", this.BuildArgs.Concat(args)).ConfigureAwait(false);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Compile and run Go files ", new MaskHilite(config[nameof(Includes)], config[nameof(Excludes)])),
                new RichDescription(this.Arguments)
            );
        }

        protected override Task CommandLineSetupAsync(IOperationExecutionContext context, RemoteProcessStartInfo info)
        {
            if (!string.IsNullOrEmpty(this.Arguments))
            {
                info.Arguments += " " + this.Arguments;
            }
            return base.CommandLineSetupAsync(context, info);
        }
    }
}
