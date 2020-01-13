using Inedo.Agents;
using Inedo.Documentation;
using Inedo.ExecutionEngine;
using Inedo.Extensibility;
using Inedo.Extensibility.Agents;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.VariableFunctions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

            return values.GetValueOrDefault(name, string.Empty);
        }

        public static async Task<IReadOnlyDictionary<string, string>> GetMultiAsync(Agent agent, IEnumerable<string> names, string go = "go", IDictionary<string, string> env = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var processExecuter = await agent.GetServiceAsync<IRemoteProcessExecuter>().ConfigureAwait(false);
            var info = new RemoteProcessStartInfo
            {
                FileName = go,
                Arguments = GoUtils.JoinArgs(agent, new[] { "env", "-json", "--" }.Concat(names))
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
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (s, e) => output.AppendLine(e.Data);
                process.ErrorDataReceived += (s, e) => error.AppendLine(e.Data);
                process.Start();

                await process.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (error.Length > 0 || process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"go env exited with code {process.ExitCode}\n\noutput:\n{output}\n\nerror:\n{error}");
                }

                return JsonConvert.DeserializeObject<Dictionary<string, string>>(output.ToString());
            }
        }
    }
}
