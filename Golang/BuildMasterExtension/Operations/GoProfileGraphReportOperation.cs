using Inedo.Agents;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Documentation;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Generate Go Flow Chart")]
    [ScriptNamespace("Golang")]
    [ScriptAlias("Go-Flow-Chart-Report")]
    [Tag("go")]
    public sealed class GoProfileGraphReportOperation : GoProfileReportOperationBase
    {
        protected override IEnumerable<string> ProfileArgs => new[] { "-svg" };

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(new RichDescription("Generate profile flow chart report ", new Hilite(config[nameof(OutputName)]), " from ", new Hilite(config[nameof(ProfileFile)])));
        }

        protected override async Task GenerateReportAsync(IOperationExecutionContext context, string outputPath)
        {
            await base.GenerateReportAsync(context, outputPath).ConfigureAwait(false);

            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var contents = await fileOps.ReadAllTextAsync(outputPath, InedoLib.UTF8Encoding).ConfigureAwait(false);
            contents = contents.Replace("\r", "").Replace(@"setAttributes(root, {
		""onmouseup"" : ""handleMouseUp(evt)"",
		""onmousedown"" : ""handleMouseDown(evt)"",
		""onmousemove"" : ""handleMouseMove(evt)"",
		//""onmouseout"" : ""handleMouseUp(evt)"", // Decomment this to stop the pan functionality when dragging out of the SVG element
	});".Replace("\r", ""), @"root.addEventListener(""mouseup"", handleMouseUp, false);
    root.addEventListener(""mousedown"", handleMouseDown, false);
    root.addEventListener(""mousemove"", handleMouseMove, false);
    root.addEventListener(""mouseout"", handleMouseUp, false);".Replace("\r", ""));
            await fileOps.WriteAllTextAsync(outputPath, contents, InedoLib.UTF8Encoding).ConfigureAwait(false);
        }
    }
}
