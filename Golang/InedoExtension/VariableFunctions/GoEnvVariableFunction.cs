using Inedo.Agents;
using Inedo.Documentation;
using Inedo.ExecutionEngine;
using Inedo.Extensibility;
using Inedo.Extensibility.Agents;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.VariableFunctions;
using System;
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
    public sealed class GoEnvVariableFunction : ScalarVariableFunction, IAsyncVariableFunction
    {
        [VariableFunctionParameter(0)]
        [Description("Environment variable name, such as GOOS or CGO_ENABLED.")]
        public string Name { get; set; }

        [VariableFunctionParameter(1, Optional = true)]
        [Description("Path to the go command.")]
        [DefaultValue("go")]
        public string GoExecutableName { get; set; } = "go";

        protected override object EvaluateScalar(IVariableFunctionContext context)
        {
            if (!(context is IOperationExecutionContext opContext))
            {
                throw new NotSupportedException("GoEnv may only be called in an execution");
            }

            return GetAsync(opContext.Agent, this.Name, this.GoExecutableName, null, opContext.CancellationToken).Result();
        }

        public async Task<RuntimeValue> EvaluateAsync(IVariableFunctionContext context)
        {
            if (!(context is IOperationExecutionContext opContext))
            {
                throw new NotSupportedException("GoEnv may only be called in an execution");
            }

            return await GetAsync(opContext.Agent, this.Name, this.GoExecutableName, null, opContext.CancellationToken).ConfigureAwait(false);
        }

        public static async Task<string> GetAsync(Agent agent, string name, string go = "go", IDictionary<string, string> env = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var values = await GetMultiAsync(agent, new[] { name }, go, env, cancellationToken).ConfigureAwait(false);
            if (!values.Any())
            {
                throw new ArgumentException($"No GO environment variable found for name '{name}'", nameof(name));
            }

            return values.First();
        }

        public static async Task<IEnumerable<string>> GetMultiAsync(Agent agent, IEnumerable<string> names, string go = "go", IDictionary<string, string> env = null, CancellationToken cancellationToken = default(CancellationToken))
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
