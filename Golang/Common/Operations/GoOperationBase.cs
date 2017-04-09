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
using Inedo.Extensions.Golang.VariableFunctions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Inedo.Extensions.Golang.Operations
{
    public abstract class GoOperationBase : ExecuteOperation
    {
        [DisplayName("Path to Go command")]
        [Category("Low-Level")]
        [ScriptAlias("GoExecutable")]
        public string GoExecutableName { get; set; }

        [DisplayName("Use Go version")]
        [Description("Automatically download this Go version. Used if Path to Go command is not set.")]
        [Category("Low-Level")]
        [ScriptAlias("GoVersion")]
        [DefaultValue("latest")]
        public string GoVersion { get; set; } = "latest";

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
            if (string.IsNullOrEmpty(this.GoExecutableName))
            {
                await this.PrepareGoAsync(context).ConfigureAwait(false);
            }
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

        private async Task PrepareGoAsync(IOperationExecutionContext context)
        {
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var dest = fileOps.CombinePath(await fileOps.GetBaseWorkingDirectoryAsync().ConfigureAwait(false), "GoVersions", "go" + this.GoVersion);
            if (await fileOps.DirectoryExistsAsync(dest).ConfigureAwait(false))
            {
                this.GoExecutableName = fileOps.CombinePath(dest, "bin", "go");
                return;
            }

            await fileOps.CreateDirectoryAsync(fileOps.CombinePath(await fileOps.GetBaseWorkingDirectoryAsync().ConfigureAwait(false), "GoVersions")).ConfigureAwait(false);

            await this.PopulateGoDownloadsAsync(context.CancellationToken).ConfigureAwait(false);

            var suffix = ".windows-amd64.zip";
            var useZip = true;
            if (await context.Agent.TryGetServiceAsync<ILinuxFileOperationsExecuter>().ConfigureAwait(false) != null)
            {
                suffix = ".linux-amd64.tar.gz";
                useZip = false;
            }

            if (string.Equals(this.GoVersion, "latest", StringComparison.OrdinalIgnoreCase))
            {
                this.GoVersion = GoDownloads.Where(i => i.StartsWith("go") && i.EndsWith(suffix)).Select(i => i.Substring(0, i.Length - suffix.Length).Substring("go".Length)).OrderByDescending(i =>
                {
                    Version v;
                    if (Version.TryParse(i, out v))
                    {
                        return v;
                    }
                    return new Version(0, 0);
                }).First();

                this.LogDebug($"The latest Go version is {this.GoVersion}.");

                dest = fileOps.CombinePath(await fileOps.GetBaseWorkingDirectoryAsync().ConfigureAwait(false), "GoVersions", "go" + this.GoVersion);
                if (await fileOps.DirectoryExistsAsync(dest).ConfigureAwait(false))
                {
                    this.GoExecutableName = fileOps.CombinePath(dest, "bin", "go");
                    return;
                }
            }

            var fileName = $"go{this.GoVersion}{suffix}";
            if (!GoDownloads.Contains(fileName))
            {
                this.LogError($"Could not find Go version {this.GoVersion} for download.");
                return;
            }

            this.LogDebug($"Downloading and extracting {fileName}...");
            using (var client = new HttpClient())
            using (var response = await client.GetAsync($"https://storage.googleapis.com/golang/{fileName}", HttpCompletionOption.ResponseHeadersRead, context.CancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var archiveDest = dest + ".tmp";
                try
                {
                    using (var archive = await fileOps.OpenFileAsync(archiveDest, FileMode.Create, FileAccess.Write).ConfigureAwait(false))
                    {
                        await response.Content.CopyToAsync(archive).ConfigureAwait(false);
                    }

                    if (useZip)
                    {
                        var zipTempDest = archiveDest + "zip";
                        try
                        {
                            await fileOps.ExtractZipFileAsync(archiveDest, zipTempDest, true).ConfigureAwait(false);
                            await fileOps.MoveDirectoryAsync(fileOps.CombinePath(zipTempDest, "go"), dest).ConfigureAwait(false);
                        }
                        finally
                        {
                            await fileOps.DeleteDirectoryAsync(zipTempDest).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await fileOps.CreateDirectoryAsync(dest).ConfigureAwait(false);
                        await this.ExecuteCommandLineAsync(context, new RemoteProcessStartInfo
                        {
                            FileName = "tar",
                            Arguments = JoinArgs(context.Agent, new[] { "xzC", dest, "-f", archiveDest, "--strip-components=1" })
                        }).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await fileOps.DeleteFileAsync(archiveDest).ConfigureAwait(false);
                }
            }
            this.LogDebug($"Downloaded Go {this.GoVersion} to {dest}.");
            this.GoExecutableName = fileOps.CombinePath(dest, "bin", "go");
        }

        private static readonly SemaphoreSlim GoDownloadsSemaphore = new SemaphoreSlim(1);
        private static readonly List<string> GoDownloads = new List<string>();

        private async Task PopulateGoDownloadsAsync(CancellationToken cancellationToken)
        {
            await GoDownloadsSemaphore.WaitAsync(cancellationToken);
            if (GoDownloads.Any())
            {
                GoDownloadsSemaphore.Release();
                return;
            }
            try
            {
                this.LogDebug("Retrieving Go version information...");
                var items = new List<string>();
                using (var client = new HttpClient())
                {
                    var url = "https://content.googleapis.com/storage/v1/b/golang/o";
                    var query = new Dictionary<string, string>()
                    {
                        { "maxResults", "1000" },
                        { "projection", "noAcl" },
                        { "alt", "json" },
                        { "fields", "nextPageToken,items(name)" }
                    };
                    while (true)
                    {
                        using (var response = await client.GetAsync(url + "?" + await new FormUrlEncodedContent(query).ReadAsStringAsync().ConfigureAwait(false), cancellationToken).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();

                            var objects = JsonConvert.DeserializeObject<StorageObjectList>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

                            items.AddRange(objects.Items.Select(i => i.Name));

                            if (objects.NextPageToken == null)
                            {
                                break;
                            }
                            query["pageToken"] = objects.NextPageToken;
                        }
                    }
                }
                GoDownloads.AddRange(items);
            }
            finally
            {
                GoDownloadsSemaphore.Release();
            }
        }

        private struct StorageObjectList
        {
            [JsonProperty(PropertyName = "nextPageToken")]
            public string NextPageToken { get; set; }
            [JsonProperty(PropertyName = "items")]
            public IEnumerable<Item> Items { get; set; }

            internal struct Item
            {
                [JsonProperty(PropertyName = "name")]
                public string Name { get; set; }
            }
        }
    }
}