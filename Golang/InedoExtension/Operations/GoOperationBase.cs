using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Golang.SuggestionProviders;
using Inedo.Extensions.Golang.VariableFunctions;
using Inedo.Web;
using Inedo.Web.Plans.ArgumentEditors;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.Operations
{
    [ScriptNamespace("Golang")]
    [Tag("go")]
    public abstract class GoOperationBase : ExecuteOperation
    {
        [DisplayName("Path to Go command")]
        [Category("Low-Level")]
        [ScriptAlias("GoExecutable")]
        [FilePathEditor]
        public string GoExecutableName { get; set; }

        [DisplayName("Use Go version")]
        [Description("Automatically download this Go version. Used if <em>Path to Go command</em> is not set.")]
        [Category("Low-Level")]
        [ScriptAlias("GoVersion")]
        [DefaultValue("latest")]
        [SuggestableValue(typeof(GoVersionSuggestionProvider))]
        public string GoVersion { get; set; } = "latest";

        [DisplayName("Operating system")]
        [Description("The operating system for which to compile code. Examples are <code>linux</code>, <code>darwin</code>, <code>windows</code>, <code>netbsd</code>. Defaults to the operating system of the current server.")]
        [Category("Low-Level")]
        [ScriptAlias("GoOS")]
        [PlaceholderText("$GoEnv(GOOS)")]
        [SuggestableValue(typeof(GoOSSuggestionProvider))]
        public string GoOS { get; set; }

        [DisplayName("Processor architecture")]
        [Description(@"The architecture, or processor, for which to compile code. Examples are <code>amd64</code>, <code>386</code>, <code>arm</code>, <code>ppc64</code>. Defaults to the architecture of the current server.")]
        [Category("Low-Level")]
        [ScriptAlias("GoArch")]
        [PlaceholderText("$GoEnv(GOARCH)")]
        [SuggestableValue(typeof(GoArchSuggestionProvider))]
        public string GoArch { get; set; }

        [DisplayName("Go working directory")]
        [Description(@"Also known as <code>GOPATH</code>. See the Go documentation of the <a href=""https://golang.org/cmd/go/#hdr-GOPATH_environment_variable"">GOPATH environment variable</a> for more details.")]
        [Category("Low-Level")]
        [PlaceholderText("[agent-specific directory]")]
        [ScriptAlias("GoPath")]
        [FilePathEditor]
        public string GoPath { get; set; }

        protected virtual void CommandLineOutput(IOperationExecutionContext context, string text)
        {
            this.LogDebug(text);
        }

        protected virtual void CommandLineError(IOperationExecutionContext context, string text)
        {
            this.LogDebug(text);
        }

        protected virtual Task CommandLineSetupAsync(IOperationExecutionContext context, RemoteProcessStartInfo info)
        {
            return Task.FromResult<object>(null);
        }

        protected async Task<int> ExecuteCommandLineAsync(IOperationExecutionContext context, string subCommand, IEnumerable<string> args)
        {
            string goroot = null;
            if (string.IsNullOrEmpty(this.GoExecutableName))
            {
                var result = await GoUtils.PrepareGoAsync(this, context, this.GoVersion).ConfigureAwait(false);
                this.GoExecutableName = result.ExecutablePath;
                goroot = result.GoRoot;
                this.GoVersion = result.Version;
            }
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            await fileOps.CreateDirectoryAsync(context.WorkingDirectory).ConfigureAwait(false);
            var processExecuter = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var info = new RemoteProcessStartInfo
            {
                WorkingDirectory = context.WorkingDirectory,
                FileName = this.GoExecutableName,
                Arguments = GoUtils.JoinArgs(context.Agent, new[] { subCommand, context.Simulation ? "-n" : null }.Concat(args))
            };
            var goos = this.GoOS;
            var goarch = this.GoArch;
            var gopath = string.IsNullOrEmpty(this.GoPath) ? fileOps.CombinePath(await fileOps.GetBaseWorkingDirectoryAsync(), "gopath") : context.ResolvePath(this.GoPath);
            if (goroot != null)
            {
                info.EnvironmentVariables.Add("GOROOT", goroot);
            }

            if (string.IsNullOrEmpty(goos) || string.IsNullOrEmpty(goarch))
            {
                var actualOSArch = await GoEnvVariableFunction.GetMultiAsync(context.Agent, new[] { "GOOS", "GOARCH" }, this.GoExecutableName, null, context.CancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(goos))
                {
                    goos = actualOSArch["GOOS"];
                }
                if (string.IsNullOrEmpty(goarch))
                {
                    goarch = actualOSArch["GOARCH"];
                }
            }

            info.EnvironmentVariables.Add("GOOS", goos);
            info.EnvironmentVariables.Add("GOARCH", goarch);
            info.EnvironmentVariables.Add("GOPATH", gopath);

            this.LogDebug($"GOOS = {goos}");
            this.LogDebug($"GOARCH = {goarch}");
            this.LogDebug($"GOPATH = {gopath}");

            await this.CommandLineSetupAsync(context, info).ConfigureAwait(false);

            this.LogInformation($"Executing command: go {info.Arguments}");
            using (var process = processExecuter.CreateProcess(info))
            {
                process.OutputDataReceived += (sender, e) => this.CommandLineOutput(context, e.Data);
                process.ErrorDataReceived += (sender, e) => this.CommandLineError(context, e.Data);

                process.Start();

                await process.WaitAsync(context.CancellationToken).ConfigureAwait(false);

                if (process.ExitCode == 0)
                {
                    this.LogDebug($"go {subCommand} was successful.");
                }
                else
                {
                    this.LogError($"go {subCommand} exited with nonzero exit status {process.ExitCode}.");
                }
                return process.ExitCode.Value;
            }
        }
    }
}
