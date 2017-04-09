using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.Operations
{
    [Tag(Tags.Reports)]
    public abstract class GoReportOperationBase : GoOperationBase
    {
        [Required]
        [ScriptAlias("Name")]
        [DisplayName("Report name")]
        public string OutputName { get; set; }

        private string RandomName
        {
            get
            {
                var rand = new Random();
                var c = new char[32];
                for (int i = 0; i < c.Length; i++)
                {
                    c[i] = (char)rand.Next((int)'a', (int)'z' + 1);
                }
                return new String(c);
            }
        }

        protected async Task SubmitReportAsync(byte[] buildOutput, IOperationExecutionContext context, string reportFormat)
        {
            this.LogDebug($"Saving report to database in {Domains.BuildReportOutputTypes.GetName(reportFormat)} format...");

            await new DB.Context(false).BuildOutputs_AddOutputAsync(context.ExecutionId, this.OutputName, buildOutput, reportFormat).ConfigureAwait(false);

            this.LogInformation($@"Report ""{this.OutputName}"" attached.");
        }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            if (context.Simulation)
            {
                this.LogInformation("Simulating; not generating a report.");
                return;
            }
            var fileName = $"report-{this.RandomName}.html";
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var reportPath = fileOps.CombinePath(context.WorkingDirectory, fileName);
            await this.GenerateReportAsync(context, reportPath).ConfigureAwait(false);
            var reportBytes = await fileOps.ReadFileBytesAsync(reportPath).ConfigureAwait(false);
            await fileOps.DeleteFileAsync(reportPath).ConfigureAwait(false);
            await this.SubmitReportAsync(reportBytes, context, Domains.BuildReportOutputTypes.Html).ConfigureAwait(false);
        }

        protected abstract Task GenerateReportAsync(IOperationExecutionContext context, string outputPath);
    }
}
