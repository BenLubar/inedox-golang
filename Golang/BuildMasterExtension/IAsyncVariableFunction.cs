using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.ExecutionEngine;

namespace Inedo.Extensions.Golang
{
    // BuildMaster does not have IAsyncVariableFunction yet.
    internal interface IAsyncVariableFunction
    {
        Task<RuntimeValue> EvaluateAsync(IGenericBuildMasterContext context);
    }
}
