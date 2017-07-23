#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
using InedoAgent = Inedo.BuildMaster.Extensibility.Agents.BuildMasterAgent;
using IGenericContext = Inedo.BuildMaster.Extensibility.IGenericBuildMasterContext;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensibility.VariableFunctions;
using InedoAgent = Inedo.Otter.Extensibility.Agents.OtterAgent;
using IGenericContext = Inedo.Otter.IOtterContext;
#endif
using Inedo.Agents;
using Inedo.Documentation;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.VariableFunctions
{
    [ScriptAlias("GoList")]
    [ScriptNamespace("Golang")]
    [Description("Returns a list of Go packages that match a pattern.")]
    [Tag("go")]
    public sealed class GoListVariableFunction : VectorVariableFunction
    {
        [VariableFunctionParameter(0)]
        [Description("Package pattern, like encoding/... or golang.org/x/crypto/bcrypt.")]
        public string Pattern { get; set; }

        [VariableFunctionParameter(1, Optional = true)]
        [Description("Path to the go command.")]
        [DefaultValue("go")]
        public string GoExecutableName { get; set; } = "go";

        [VariableFunctionParameter(2, Optional = true)]
        [Description("True to only list packages named main. False to exclude packages named main.")]
        public bool? Commands { get; set; }

        protected override IEnumerable EvaluateVector(IGenericContext context)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            Dictionary<string, string> env = null;
            if (context is IOperationExecutionContext)
            {
                cancellationToken = ((IOperationExecutionContext)context).CancellationToken;
                env = new Dictionary<string, string>() { { "GOPATH", ((IOperationExecutionContext)context).WorkingDirectory } };
            }
            using (var agent = InedoAgent.Create(context.ServerId.Value))
            {
                return ListAsync(agent, new[] { this.Pattern }, this.Commands, this.GoExecutableName, env, cancellationToken).Result();
            }
        }

        public static async Task<IEnumerable<string>> ListAsync(InedoAgent agent, IEnumerable<string> names, bool? commands, string go = "go", IDictionary<string, string> env = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var processExecuter = await agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var info = new RemoteProcessStartInfo
            {
                FileName = go,
                Arguments = GoUtils.JoinArgs(agent, new[]
                {
                    "list",
                    commands.HasValue ? "-f" : null,
                    commands.HasValue ?
                        commands.Value ?
                            "{{if eq .Name \"main\"}}{{.ImportPath}}{{end}}" :
                            "{{if ne .Name \"main\"}}{{.ImportPath}}{{end}}" :
                        null,
                    "--"
                }.Concat(names))
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
                process.OutputDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) { output.Add(e.Data); } };
                process.Start();
                await process.WaitAsync(cancellationToken).ConfigureAwait(false);
                return output;
            }
        }
    }
}
