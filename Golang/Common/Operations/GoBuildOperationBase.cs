#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Web.Controls;
#endif
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensions.Golang.SuggestionProviders;
using Inedo.Extensions.Golang.VariableFunctions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace Inedo.Extensions.Golang.Operations
{
    public abstract class GoBuildOperationBase : GoOperationBase
    {
        [DisplayName("Tags")]
        [Description(@"A space-separated list of additional <a href=""https://golang.org/pkg/go/build/#hdr-Build_Constraints"">build constraints</a> to consider satisfied.")]
        [ScriptAlias("Tags")]
        [Category("Advanced")]
        public string Tags { get; set; }

        [DisplayName("Build mode")]
        [Description(@"There is a <a href=""https://golang.org/cmd/go/#hdr-Description_of_build_modes"">description of build modes</a> in the Go documentation.")]
        [ScriptAlias("Mode")]
        [Category("Advanced")]
        [PlaceholderText("default")]
        [SuggestibleValue(typeof(BuildModeSuggestionProvider))]
        public string BuildMode { get; set; }

        [DisplayName("Use shared libraries")]
        [ScriptAlias("LinkShared")]
        [Category("Advanced")]
        public bool LinkShared { get; set; }

        [DisplayName("Detect data races")]
        [Description(@"Compile with the <a href=""https://golang.org/doc/articles/race_detector.html"">data race detector</a> enabled. Typical programs compiled this way use 5-10&times; more memory and take 2-20&times; longer to run, but this can be a useful debugging tool.")]
        [ScriptAlias("Race")]
        [Category("Advanced")]
        public bool Race { get; set; } = false;

        [DisplayName("Compiler name")]
        [ScriptAlias("Compiler")]
        [Category("Compiler")]
        [PlaceholderText("gc")]
        [SuggestibleValue(typeof(CompilerSuggestionProvider))]
        public string Compiler { get; set; }

        [DisplayName("Path to gccgo command")]
        [ScriptAlias("GccGoExecutable")]
        [Category("Low-Level")]
        public string GccGoExecutableName { get; set; }

        [DisplayName("ARM architecture version")]
        [ScriptAlias("GoArm")]
        [Category("Low-Level")]
        [SuggestibleValue(typeof(GoArmSuggestionProvider))]
        public string GoArm { get; set; }

        [DisplayName("x86 floating-point instruction set")]
        [ScriptAlias("Go386")]
        [Category("Low-Level")]
        [SuggestibleValue(typeof(Go386SuggestionProvider))]
        public string Go386 { get; set; }

        [DisplayName("Assembler flags")]
        [ScriptAlias("AsmFlags")]
        [Category("Compiler")]
        public string AsmFlags { get; set; }

        [DisplayName("Compiler flags (gc)")]
        [ScriptAlias("GcFlags")]
        [Category("Compiler")]
        public string GcFlags { get; set; }

        [DisplayName("Compiler flags (gccgo)")]
        [ScriptAlias("GccGoFlags")]
        [Category("Compiler")]
        public string GccGoFlags { get; set; }

        [DisplayName("Linker flags (gc)")]
        [ScriptAlias("LdFlags")]
        [Category("Compiler")]
        public string LdFlags { get; set; }

        [DisplayName("Path to C compiler")]
        [ScriptAlias("CcExecutable")]
        [Category("C Integration")]
        public string CcExecutableName { get; set; }

        [DisplayName("Path to C++ compiler")]
        [ScriptAlias("CxxExecutable")]
        [Category("C Integration")]
        public string CxxExecutableName { get; set; }

        [DisplayName("Path to pkg-config command")]
        [ScriptAlias("PkgConfigExecutable")]
        [Category("C Integration")]
        public string PkgConfigExecutableName { get; set; }

        [DisplayName("Compiler flags (C)")]
        [ScriptAlias("CgoCFlags")]
        [Category("C Integration")]
        public string CgoCFlags { get; set; }

        [DisplayName("Compiler flags (C++)")]
        [ScriptAlias("CgoCxxFlags")]
        [Category("C Integration")]
        public string CgoCxxFlags { get; set; }

        [DisplayName("Compiler flags (Fortran)")]
        [ScriptAlias("CgoFFlags")]
        [Category("C Integration")]
        public string CgoFFlags { get; set; }

        [DisplayName("Linker flags")]
        [ScriptAlias("CgoLdFlags")]
        [Category("C Integration")]
        public string CgoLdFlags { get; set; }

        protected IEnumerable<string> BuildArgs => new[]
        {
            string.IsNullOrWhiteSpace(this.Tags) ? null : "-tags",
            string.IsNullOrWhiteSpace(this.Tags) ? null : this.Tags,
            string.IsNullOrWhiteSpace(this.BuildMode) ? null : "-buildmode",
            string.IsNullOrWhiteSpace(this.BuildMode) ? null : this.BuildMode,
            this.LinkShared ? "-linkshared" : null,
            this.Race ? "-race" : null,
            string.IsNullOrWhiteSpace(this.Compiler) ? null : "-compiler",
            string.IsNullOrWhiteSpace(this.Compiler) ? null : this.Compiler,
            string.IsNullOrWhiteSpace(this.AsmFlags) ? null : "-asmflags",
            string.IsNullOrWhiteSpace(this.AsmFlags) ? null : this.AsmFlags,
            string.IsNullOrWhiteSpace(this.GcFlags) ? null : "-gcflags",
            string.IsNullOrWhiteSpace(this.GcFlags) ? null : this.GcFlags,
            string.IsNullOrWhiteSpace(this.GccGoFlags) ? null : "-gccgoflags",
            string.IsNullOrWhiteSpace(this.GccGoFlags) ? null : this.GccGoFlags,
            string.IsNullOrWhiteSpace(this.LdFlags) ? null : "-ldflags",
            string.IsNullOrWhiteSpace(this.LdFlags) ? null : this.LdFlags
        };

        protected override async Task CommandLineSetupAsync(IOperationExecutionContext context, RemoteProcessStartInfo info)
        {
            if (!string.IsNullOrWhiteSpace(this.GccGoExecutableName))
            {
                info.EnvironmentVariables.Add("GCCGO", this.GccGoExecutableName);
                this.LogDebug($"GCCGO = {this.GccGoExecutableName}");
            }
            if (!string.IsNullOrWhiteSpace(this.GoArm))
            {
                info.EnvironmentVariables.Add("GOARM", this.GoArm);
                this.LogDebug($"GOARM = {this.GoArm}");
            }
            if (!string.IsNullOrWhiteSpace(this.Go386))
            {
                info.EnvironmentVariables.Add("GO386", this.Go386);
                this.LogDebug($"GO386 = {this.Go386}");
            }
            var cgoVars = new Dictionary<string, string>
            {
                {"CC", this.CcExecutableName},
                {"CXX", this.CxxExecutableName},
                {"PKG_CONFIG", this.PkgConfigExecutableName},
                {"CGO_CFLAGS", this.CgoCFlags},
                {"CGO_CXXFLAGS", this.CgoCxxFlags},
                {"CGO_FFLAGS", this.CgoFFlags},
                {"CGO_LDFLAGS", this.CgoLdFlags},
            };
            foreach (var cgoVar in cgoVars)
            {
                if (!string.IsNullOrWhiteSpace(cgoVar.Value))
                {
                    info.EnvironmentVariables.Add(cgoVar);
                }
            }
            if (await GoEnvVariableFunction.GetAsync(context.Agent, "CGO_ENABLED", this.GoExecutableName, info.EnvironmentVariables, context.CancellationToken).ConfigureAwait(false) == "1")
            {
                this.LogDebug("CGO_ENABLED = 1");
                var actualCgoVars = await GoEnvVariableFunction.GetMultiAsync(context.Agent, cgoVars.Keys, this.GoExecutableName, info.EnvironmentVariables, context.CancellationToken).ConfigureAwait(false);
                foreach (var cgoVar in cgoVars.Keys.Zip(actualCgoVars, (k, v) => new KeyValuePair<string, string>(k, v)))
                {
                    this.LogDebug($"{cgoVar.Key} = {cgoVar.Value}");
                }
            }
            else
            {
                this.LogDebug("CGO_ENABLED = 0");
            }

            await base.CommandLineSetupAsync(context, info).ConfigureAwait(false);
        }
    }
}