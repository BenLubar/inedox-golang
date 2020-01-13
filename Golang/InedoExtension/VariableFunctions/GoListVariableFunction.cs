using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Documentation;
using Inedo.ExecutionEngine;
using Inedo.Extensibility;
using Inedo.Extensibility.Agents;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extensions.Golang.VariableFunctions
{
    [ScriptAlias("GoList")]
    [ScriptNamespace("Golang")]
    [Description("Returns a list of Go packages that match a pattern.")]
    [Tag("go")]
    public sealed class GoListVariableFunction : VectorVariableFunction, IAsyncVariableFunction
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

        protected override IEnumerable EvaluateVector(IVariableFunctionContext context)
        {
            if (!(context is IOperationExecutionContext opContext))
            {
                throw new NotSupportedException("GoList may only be called in an execution");
            }

            var env = new Dictionary<string, string>() { { "GOPATH", opContext.WorkingDirectory } };
            return ListAsync(opContext.Agent, new[] { this.Pattern }, this.Commands, this.GoExecutableName, env, opContext.CancellationToken).Result();
        }

        public async Task<RuntimeValue> EvaluateAsync(IVariableFunctionContext context)
        {
            if (!(context is IOperationExecutionContext opContext))
            {
                throw new NotSupportedException("GoList may only be called in an execution");
            }

            var env = new Dictionary<string, string>() { { "GOPATH", opContext.WorkingDirectory } };
            var list = await ListAsync(opContext.Agent, new[] { this.Pattern }, this.Commands, this.GoExecutableName, env, opContext.CancellationToken).ConfigureAwait(false);
            return new RuntimeValue(list.Select(s => new RuntimeValue(s)));
        }

        public static async Task<IEnumerable<string>> ListAsync(Agent agent, IEnumerable<string> names, bool? commands, string go = "go", IDictionary<string, string> env = null, CancellationToken cancellationToken = default(CancellationToken))
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

            using (var process = processExecuter.CreateProcess(GoUtils.ShimEnvironment(agent, info)))
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
