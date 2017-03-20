#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Plans;
using InedoAgent = Inedo.BuildMaster.Extensibility.Agents.BuildMasterAgent;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Web.Controls;
using Inedo.Otter.Web.Controls.Plans;
using InedoAgent = Inedo.Otter.Extensibility.Agents.OtterAgent;
#endif
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensions.Golang.SuggestionProviders;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Inedo.Extensions.Golang.VariableFunctions;

namespace Inedo.Extensions.Golang.Operations
{
    public abstract class GoOperationBase : ExecuteOperation
    {
        [DisplayName("Path to Go command")]
        [Category("Low-Level")]
        [ScriptAlias("GoExecutable")]
        [DefaultValue("go")]
        public string GoExecutableName { get; set; } = "go";

        [DisplayName("Operating system")]
        [Category("Low-Level")]
        [ScriptAlias("GoOS")]
        [PlaceholderText("$GoEnv(GOOS)")]
        [SuggestibleValue(typeof(GoOSSuggestionProvider))]
        public string GoOS { get; set; }

        [DisplayName("Processor architecture")]
        [Category("Low-Level")]
        [ScriptAlias("GoArch")]
        [PlaceholderText("$GoEnv(GOARCH)")]
        [SuggestibleValue(typeof(GoArchSuggestionProvider))]
        public string GoArch { get; set; }

        [DisplayName("Go working directory")]
        [Category("Low-Level")]
        [PlaceholderText("$WorkingDirectory")]
        [ScriptAlias("GoPath")]
        [FilePathEditor]
        public string GoPath { get; set; }

        private static string EscapeArgWindows(string arg)
        {
            // https://msdn.microsoft.com/en-us/library/ms880421

            if (!arg.Any(c => char.IsWhiteSpace(c) || c == '\\' || c == '"'))
            {
                return arg;
            }

            var str = new StringBuilder();
            str.Append('"');
            int slashes = 0;
            foreach (char c in arg)
            {
                if (c == '"')
                {
                    str.Append('\\', slashes);
                    str.Append('\\', '"');
                    slashes = 0;
                }
                else if (c == '\\')
                {
                    str.Append('\\');
                    slashes++;
                }
                else
                {
                    str.Append(c);
                    slashes = 0;
                }
            }
            str.Append('\\', slashes);
            str.Append('"');

            return str.ToString();
        }

        private static string EscapeArgLinux(string arg)
        {
            // This is terrible and we should be using exec instead of a shell, but what can you do?

            if (arg.All(c => char.IsLetterOrDigit(c) || c == '/' || c == '.' || c == '_' || c == '-'))
            {
                return arg;
            }

            return "'" + arg.Replace("'", "'\"'\"'") + "'";
        }

        internal static string JoinArgs(InedoAgent agent, IEnumerable<string> args)
        {
            bool isLinux = agent.TryGetService<ILinuxFileOperationsExecuter>() != null;
            var escape = isLinux ? (Func<string, string>)EscapeArgLinux : EscapeArgWindows;
            return string.Join(" ", args.Where(arg => arg != null).Select(escape));
        }

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
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            await fileOps.CreateDirectoryAsync(context.WorkingDirectory).ConfigureAwait(false);
            var processExecuter = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var info = new RemoteProcessStartInfo
            {
                WorkingDirectory = context.WorkingDirectory,
                FileName = this.GoExecutableName,
                Arguments = JoinArgs(context.Agent, new[] { subCommand, context.Simulation ? "-n" : null }.Concat(args))
            };
            var goos = this.GoOS;
            var goarch = this.GoArch;
            var gopath = context.ResolvePath(this.GoPath);

            if (string.IsNullOrEmpty(goos) || string.IsNullOrEmpty(goarch))
            {
                var actualOSArch = await GoEnvVariableFunction.GetMultiAsync(context.Agent, new[] { "GOOS", "GOARCH" }, this.GoExecutableName, null, context.CancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(goos))
                {
                    goos = actualOSArch.First();
                }
                if (string.IsNullOrEmpty(goarch))
                {
                    goarch = actualOSArch.Last();
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