using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Extensibility.Agents;
using Inedo.Extensibility.Operations;
using Inedo.IO;

namespace Inedo.Extensions.Golang
{
    internal static class GoUtils
    {
        internal const string ImportPathDescription = @"A detailed <a href=""https://golang.org/cmd/go/#hdr-Description_of_package_lists"">description of import paths</a> can be found in the Go documentation.";

        internal enum AgentOperatingSystem
        {
            Windows,
            Linux
        }

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
            if (arg.All(c => char.IsLetterOrDigit(c) || c == '/' || c == '.' || c == '_' || c == '-'))
            {
                return arg;
            }

            return "'" + arg.Replace("'", "'\"'\"'") + "'";
        }

        internal static string JoinArgs(Agent agent, IEnumerable<string> args)
        {
            var escape = AH.Switch<AgentOperatingSystem, Func<string, string>>(GetAgentOperatingSystem(agent)).
                Case(AgentOperatingSystem.Windows, EscapeArgWindows).
                Case(AgentOperatingSystem.Linux, EscapeArgLinux).
                End();
            return string.Join(" ", args.Where(arg => arg != null).Select(escape));
        }

        internal static AgentOperatingSystem GetAgentOperatingSystem(Agent agent)
        {
            return agent.TryGetService<ILinuxFileOperationsExecuter>() == null ? AgentOperatingSystem.Windows : AgentOperatingSystem.Linux;
        }

        internal struct GoVersion
        {
            internal GoVersion(string executablePath, string version)
            {
                this.ExecutablePath = executablePath;
                this.Version = version;
            }

            public string ExecutablePath { get; }
            public string Version { get; }
        }

        internal static async Task<GoVersion> PrepareGoAsync(ILogSink logger, IOperationExecutionContext context, string version)
        {
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var dest = fileOps.CombinePath(await fileOps.GetBaseWorkingDirectoryAsync().ConfigureAwait(false), "GoVersions", "go" + version);
            if (await fileOps.DirectoryExistsAsync(dest).ConfigureAwait(false))
            {
                return new GoVersion(fileOps.CombinePath(dest, "bin", "go"), version);
            }

            await fileOps.CreateDirectoryAsync(fileOps.CombinePath(await fileOps.GetBaseWorkingDirectoryAsync().ConfigureAwait(false), "GoVersions")).ConfigureAwait(false);

            var downloads = await PopulateGoDownloadsAsync(logger, context.CancellationToken).ConfigureAwait(false);

            var agentOS = GetAgentOperatingSystem(context.Agent);
            var suffix = AH.Switch<AgentOperatingSystem, string>(agentOS).
                Case(AgentOperatingSystem.Windows, ".windows-amd64.zip").
                Case(AgentOperatingSystem.Linux, ".linux-amd64.tar.gz").
                End();

            if (string.Equals(version, "latest", StringComparison.OrdinalIgnoreCase))
            {
                version = downloads.Where(i => i.StartsWith("go") && i.EndsWith(suffix)).Select(i => i.Substring(0, i.Length - suffix.Length).Substring("go".Length)).OrderByDescending(i =>
                {
                    Version v;
                    if (Version.TryParse(i, out v))
                    {
                        return v;
                    }
                    return new Version(0, 0);
                }).First();

                logger?.LogDebug($"The latest Go version is {version}.");

                dest = fileOps.CombinePath(await fileOps.GetBaseWorkingDirectoryAsync().ConfigureAwait(false), "GoVersions", "go" + version);
                if (await fileOps.DirectoryExistsAsync(dest).ConfigureAwait(false))
                {
                    return new GoVersion(fileOps.CombinePath(dest, "bin", "go"), version);
                }
            }

            var fileName = $"go{version}{suffix}";
            if (!downloads.Contains(fileName))
            {
                logger?.LogError($"Could not find Go version {version} for download.");
                return new GoVersion(null, version);
            }

            logger?.LogDebug($"Downloading and extracting {fileName}...");
            using (var client = new HttpClient())
            using (var response = await client.GetAsync($"https://storage.googleapis.com/golang/{fileName}", HttpCompletionOption.ResponseHeadersRead, context.CancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var archiveDest = dest + ".tmp";
                var dirDest = dest + ".tmpdir";
                try
                {
                    using (var archive = await fileOps.OpenFileAsync(archiveDest, FileMode.Create, FileAccess.Write).ConfigureAwait(false))
                    {
                        await response.Content.CopyToAsync(archive).ConfigureAwait(false);
                    }

                    switch (agentOS)
                    {
                        case AgentOperatingSystem.Windows:
                            await fileOps.ExtractZipFileAsync(archiveDest, dirDest, FileCreationOptions.OverwriteReadOnly).ConfigureAwait(false);
                            break;
                        case AgentOperatingSystem.Linux:
                            await fileOps.CreateDirectoryAsync(dirDest).ConfigureAwait(false);
                            var processExecuter = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
                            using (var process = processExecuter.CreateProcess(new RemoteProcessStartInfo
                            {
                                FileName = "tar",
                                Arguments = JoinArgs(context.Agent, new[] { "xzC", dirDest, "-f", archiveDest })
                            }))
                            {
                                process.OutputDataReceived += (s, e) => logger?.LogWarning(e.Data);
                                process.ErrorDataReceived += (s, e) => logger?.LogWarning(e.Data);
                                process.Start();
                                await process.WaitAsync(context.CancellationToken).ConfigureAwait(false);
                                if (process.ExitCode != 0)
                                {
                                    logger?.LogError($"tar exited with code {process.ExitCode}");
                                }
                            }
                            break;
                    }
                    await fileOps.MoveDirectoryAsync(fileOps.CombinePath(dirDest, "go"), dest).ConfigureAwait(false);
                }
                finally
                {
                    await fileOps.DeleteDirectoryAsync(dirDest).ConfigureAwait(false);
                    await fileOps.DeleteFileAsync(archiveDest).ConfigureAwait(false);
                }
            }
            logger?.LogDebug($"Downloaded Go {version} to {dest}.");
            return new GoVersion(fileOps.CombinePath(dest, "bin", "go"), version);
        }

        private static readonly SemaphoreSlim GoDownloadsSemaphore = new SemaphoreSlim(1);
        private static volatile List<string> GoDownloadsCache = null;

        internal static async Task<IReadOnlyList<string>> PopulateGoDownloadsAsync(ILogSink logger, CancellationToken cancellationToken)
        {
            await GoDownloadsSemaphore.WaitAsync(cancellationToken);
            var downloads = GoDownloadsCache;
            try
            {
                if (downloads != null)
                {
                    logger?.LogDebug("Using cached Go version information...");
                    return downloads;
                }

                logger?.LogDebug("Retrieving Go version information...");
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
                GoDownloadsCache = items;
                var _ = ClearGoDownloadsAsync();
                return items;
            }
            finally
            {
                GoDownloadsSemaphore.Release();
            }
        }

        private static async Task ClearGoDownloadsAsync()
        {
            await Task.Delay(TimeSpan.FromHours(1)).ConfigureAwait(false);
            await GoDownloadsSemaphore.WaitAsync();
            GoDownloadsCache = null;
            GoDownloadsSemaphore.Release();
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
