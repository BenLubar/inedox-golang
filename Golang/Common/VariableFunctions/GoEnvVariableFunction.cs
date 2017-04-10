#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
using InedoAgent = Inedo.BuildMaster.Extensibility.Agents.BuildMasterAgent;
using IGenericContext = Inedo.BuildMaster.Extensibility.IGenericBuildMasterContext;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.VariableFunctions;
using InedoAgent = Inedo.Otter.Extensibility.Agents.OtterAgent;
using IGenericContext = Inedo.Otter.IOtterContext;
#endif
using Inedo.Agents;
using Inedo.Documentation;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.VariableFunctions
{
    [ScriptAlias("GoEnv")]
    [ScriptNamespace("Golang")]
    [Description("Returns the value of a Go environment variable.")]
    [Tag("go")]
    public sealed class GoEnvVariableFunction : ScalarVariableFunction
    {
        [VariableFunctionParameter(0)]
        [Description("Environment variable name, such as GOOS or CGO_ENABLED.")]
        public string Name { get; set; }

        [VariableFunctionParameter(1, Optional = true)]
        [Description("Path to the go command.")]
        [DefaultValue("go")]
        public string GoExecutableName { get; set; } = "go";

        protected override object EvaluateScalar(IGenericContext context)
        {
            using (var agent = InedoAgent.Create(context.ServerId.Value))
            {
                return GetAsync(agent, this.Name, this.GoExecutableName).Result();
            }
        }

        public static async Task<string> GetAsync(InedoAgent agent, string name, string go = "go", IDictionary<string, string> env = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await GetMultiAsync(agent, new[] { name }, go, env, cancellationToken).ConfigureAwait(false)).First();
        }

        public static async Task<IEnumerable<string>> GetMultiAsync(InedoAgent agent, IEnumerable<string> names, string go = "go", IDictionary<string, string> env = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var processExecuter = await agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var info = new RemoteProcessStartInfo
            {
                FileName = go,
                Arguments = GoUtils.JoinArgs(agent, new[] { "env" }.Concat(names))
            };
            if (env != null)
            {
                foreach (var e in env)
                {
                    info.EnvironmentVariables.Add(e);
                }
            }

            using (var process = processExecuter.CreateProcess(info))
            {
                var output = new List<string>(names.Count());
                process.OutputDataReceived += (s, e) => output.Add(e.Data);
                process.Start();
                await process.WaitAsync(cancellationToken).ConfigureAwait(false);
                return output;
            }
        }
    }
}
