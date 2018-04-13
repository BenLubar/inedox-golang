using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Golang.SuggestionProviders;
using Inedo.Web;

namespace Inedo.Extensions.Golang.Operations
{
    [DisplayName("Test Go Package")]
    [Description("Run test cases on a Go package using the go test command.")]
    [ScriptAlias("Test")]
    public sealed class GoTestOperation : GoBuildOperationBase
    {
        [Required]
        [DisplayName("Package path")]
        [Description("The import path of the package to test.")]
        [ScriptAlias("Package")]
        public string Package { get; set; }

        [DisplayName("Test RegEx")]
        [PlaceholderText("Run all tests and examples")]
        [ScriptAlias("TestPattern")]
        public string TestPattern { get; set; }

        [DisplayName("Benchmark RegEx")]
        [PlaceholderText("Do not run any benchmarks")]
        [ScriptAlias("BenchmarkPattern")]
        public string BenchmarkPattern { get; set; }

        [DisplayName("Benchmark memory report")]
        [DefaultValue(true)]
        [ScriptAlias("BenchmarkMemory")]
        public bool BenchmarkMemory { get; set; } = true;

        [DisplayName("Benchmark goal time (seconds)")]
        [DefaultValue(1)]
        [ScriptAlias("BenchTime")]
        [TimeSpanUnit(TimeSpanUnit.Seconds)]
        public TimeSpan BenchmarkGoal { get; set; } = TimeSpan.FromSeconds(1);

        [DisplayName("Iteration count")]
        [DefaultValue(1)]
        [ScriptAlias("RepeatCount")]
        public int RepeatCount { get; set; } = 1;

        [DisplayName("Thread pool sizes")]
        [PlaceholderText("eg. 1,2,4")]
        [ScriptAlias("ThreadPoolSizes")]
        public string ThreadPoolSizes { get; set; }

        [DisplayName("Overall time limit (seconds)")]
        [DefaultValue(10 * 60)]
        [ScriptAlias("Deadline")]
        [TimeSpanUnit(TimeSpanUnit.Seconds)]
        public TimeSpan Deadline { get; set; } = TimeSpan.FromMinutes(10);

        [DisplayName("Only run short tests")]
        [DefaultValue(false)]
        [ScriptAlias("Short")]
        public bool Short { get; set; } = false;

        [DisplayName("Test coverage profile")]
        [Category("Profiling")]
        [ScriptAlias("CoverProfile")]
        public string CoverProfile { get; set; }

        [DisplayName("Coverage mode")]
        [Category("Profiling")]
        [ScriptAlias("CoverMode")]
        [SuggestableValue(typeof(CoverModeSuggestionProvider))]
        public string CoverMode { get; set; }

        [DisplayName("CPU profile")]
        [Category("Profiling")]
        [ScriptAlias("CpuProfile")]
        public string CpuProfile { get; set; }

        [DisplayName("Memory allocation profile")]
        [Category("Profiling")]
        [ScriptAlias("MemProfile")]
        public string MemProfile { get; set; }

        [DisplayName("Bytes per memory profile record")]
        [Category("Profiling")]
        [DefaultValue(512 * 1024)]
        [ScriptAlias("MemProfileRate")]
        public int MemProfileRate { get; set; } = 512 * 1024;

        [DisplayName("Blocked goroutine profile")]
        [Category("Profiling")]
        [ScriptAlias("BlockProfile")]
        public string BlockProfile { get; set; }

        [DisplayName("Nanoseconds per block record")]
        [Category("Profiling")]
        [DefaultValue(1)]
        [ScriptAlias("BlockProfileRate")]
        public int BlockProfileRate { get; set; } = 1;

        [DisplayName("Mutex contention profile")]
        [Category("Profiling")]
        [ScriptAlias("MutexProfile")]
        public string MutexProfile { get; set; }

        [DisplayName("Sample 1 in N mutex contentions")]
        [Category("Profiling")]
        [DefaultValue(1)]
        [ScriptAlias("MutexProfileFraction")]
        public int MutexProfileFraction { get; set; } = 1;

        [DisplayName("Execution trace")]
        [Category("Profiling")]
        [ScriptAlias("ExecutionTrace")]
        public string ExecutionTrace { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            await this.ExecuteCommandLineAsync(context, "test", this.BuildArgs.Concat(new[]
            {
                "-v",
                string.IsNullOrEmpty(this.TestPattern) ? null : "-run",
                string.IsNullOrEmpty(this.TestPattern) ? null : this.TestPattern,
                string.IsNullOrEmpty(this.BenchmarkPattern) ? null : "-bench",
                string.IsNullOrEmpty(this.BenchmarkPattern) ? null : this.BenchmarkPattern,
                string.IsNullOrEmpty(this.BenchmarkPattern) ? null : "-benchtime",
                string.IsNullOrEmpty(this.BenchmarkPattern) ? null : this.BenchmarkGoal.ToGoString(),
                string.IsNullOrEmpty(this.BenchmarkPattern) || !this.BenchmarkMemory ? null : "-benchmem",
                this.RepeatCount == 1 ? null : "-count",
                this.RepeatCount == 1 ? null : this.RepeatCount.ToString(),
                string.IsNullOrWhiteSpace(this.ThreadPoolSizes) ? null : "-cpu",
                string.IsNullOrWhiteSpace(this.ThreadPoolSizes) ? null : this.ThreadPoolSizes,
                this.Short ? "-short" : null,
                "-timeout",
                this.Deadline.ToGoString(),
                string.IsNullOrWhiteSpace(this.CoverProfile) ? null : "-coverprofile",
                string.IsNullOrWhiteSpace(this.CoverProfile) ? null : this.CoverProfile,
                string.IsNullOrWhiteSpace(this.CoverMode) ? null : "-covermode",
                string.IsNullOrWhiteSpace(this.CoverMode) ? null : this.CoverMode,
                string.IsNullOrWhiteSpace(this.BlockProfile) ? null : "-blockprofile",
                string.IsNullOrWhiteSpace(this.BlockProfile) ? null : this.BlockProfile,
                string.IsNullOrWhiteSpace(this.BlockProfile) || this.BlockProfileRate == 1 ? null : "-blockprofilerate",
                string.IsNullOrWhiteSpace(this.BlockProfile) || this.BlockProfileRate == 1 ? null : this.BlockProfileRate.ToString(),
                string.IsNullOrWhiteSpace(this.CpuProfile) ? null : "-cpuprofile",
                string.IsNullOrWhiteSpace(this.CpuProfile) ? null : this.CpuProfile,
                string.IsNullOrWhiteSpace(this.MemProfile) ? null : "-memprofile",
                string.IsNullOrWhiteSpace(this.MemProfile) ? null : this.MemProfile,
                string.IsNullOrWhiteSpace(this.MemProfile) || this.MemProfileRate == 512 * 1024 ? null : "-memprofilerate",
                string.IsNullOrWhiteSpace(this.MemProfile) || this.MemProfileRate == 512 * 1024 ? null : this.MemProfileRate.ToString(),
                string.IsNullOrWhiteSpace(this.MutexProfile) ? null : "-mutexprofile",
                string.IsNullOrWhiteSpace(this.MutexProfile) ? null : this.MutexProfile,
                string.IsNullOrWhiteSpace(this.MutexProfile) || this.MutexProfileFraction == 1 ? null : "-mutexprofilefraction",
                string.IsNullOrWhiteSpace(this.MutexProfile) || this.MutexProfileFraction == 1 ? null : this.MutexProfileFraction.ToString(),
                string.IsNullOrWhiteSpace(this.ExecutionTrace) ? null : "-trace",
                string.IsNullOrWhiteSpace(this.ExecutionTrace) ? null : this.ExecutionTrace,
                this.Package
            })).ConfigureAwait(false);

#if BuildMaster
            this.FinishedTest(context);
#endif
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var desc = new RichDescription("Run tests");
            if (!string.IsNullOrEmpty(config[nameof(TestPattern)]))
            {
                desc.AppendContent(" matching ", new Hilite(config[nameof(TestPattern)]));
            }
            if (!string.IsNullOrEmpty(config[nameof(BenchmarkPattern)]))
            {
                desc.AppendContent(" and benchmarks matching ", new Hilite(config[nameof(BenchmarkPattern)]));
            }
            desc.AppendContent(" on Go package ", new Hilite(config[nameof(Package)]));
            return new ExtendedRichDescription(desc);
        }

#if BuildMaster
        /* TestStartPattern:
         * 1: Full test name
         * 2: "Test" or "Example"
         * 3: Remainder of test name
         */
        private static readonly Regex TestStartPattern = new Regex(@"^=== RUN   ((Test|Example)(.*))$", RegexOptions.Compiled);
        /* TestEndPattern
         * 1: "PASS", "FAIL", or "SKIP"
         * 2: Full test name
         * 3: "Test" or "Example"
         * 4: Remainder of test name
         * 5: Time taken by test in seconds with 0.01s precision
         */
        private static readonly Regex TestEndPattern = new Regex(@"^(?:    )*--- (PASS|FAIL|SKIP): ((Test|Example)(.*)) \(([0-9]+\.[0-9][0-9])s\)$", RegexOptions.Compiled);
        /* BenchmarkResultPattern
         * 1: Name of benchmark after "Benchmark"
         * 2: Number of CPU cores, missing if 1
         * 3: Number of iterations on last benchmark run
         * 4: Time per iteration in nanoseconds with 0, 1, or 2 decimal places of precision
         * 5: Megabytes processed per second
         * 6: Bytes allocated per iteration
         * 7: Memory allocations per iteration
         */
        private static readonly Regex BenchmarkResultPattern = new Regex(@"^Benchmark([^- \t]+)(?:-([0-9]+))?[ ]*\t[ ]*([1235]0*)\t[ ]*([0-9]+(?:\.[0-9]+)?) ns/op(?:[ ]*([0-9]+\.[0-9][0-9]) MB/s)?(?:\t[ ]*([0-9]+) B/op\t[ ]*([0-9]+) allocs/op)?$", RegexOptions.Compiled);
        private DateTime? lastTestFinished = null;
        private Match lastTest = null;
        private StringBuilder testOutput = null;

        internal struct BenchmarkResult
        {
            public string Name { get; }
            public int CPUs { get; }
            public long Iterations { get; }
            public decimal Nanoseconds { get; }
            public decimal? MBPerSecond { get; }
            public long? BytesAllocated { get; }
            public int? Allocations { get; }

            public BenchmarkResult(Match match)
            {
                this.Name = match.Groups[1].Value;
                this.CPUs = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
                this.Iterations = long.Parse(match.Groups[3].Value);
                this.Nanoseconds = decimal.Parse(match.Groups[4].Value);
                this.MBPerSecond = match.Groups[5].Success ? (decimal?)decimal.Parse(match.Groups[5].Value) : null;
                this.BytesAllocated = match.Groups[6].Success ? (long?)long.Parse(match.Groups[6].Value) : null;
                this.Allocations = match.Groups[7].Success ? (int?)int.Parse(match.Groups[7].Value) : null;
            }

            public BenchmarkResult(Tables.BuildTestResults_Extended table)
            {
                if (!table.Test_Name.StartsWith("Benchmark"))
                {
                    throw new FormatException(nameof(table.Test_Name));
                }
                var nameParts = table.Test_Name.Substring("Benchmark".Length).Split('-');
                if (nameParts.Length != 2)
                {
                    throw new FormatException(nameof(table.Test_Name));
                }
                this.Name = nameParts[0];
                this.CPUs = int.Parse(nameParts[1]);
                var lines = table.TestResult_Text.TrimEnd('\n').Split('\n');
                if (lines.Length < 2)
                {
                    throw new FormatException(nameof(table.TestResult_Text));
                }
                if (!lines[0].StartsWith("Nanoseconds: "))
                {
                    throw new FormatException(nameof(this.Nanoseconds));
                }
                this.Nanoseconds = decimal.Parse(lines[0].Substring("Nanoseconds: ".Length));
                if (!lines[1].StartsWith("Iterations: "))
                {
                    throw new FormatException(nameof(this.Iterations));
                }
                this.Iterations = long.Parse(lines[1].Substring("Iterations: ".Length));
                if (lines.Length > 2 && lines[2].StartsWith("MBPerSecond: "))
                {
                    this.MBPerSecond = decimal.Parse(lines[2].Substring("MBPerSecond: ".Length));
                    if (lines.Length > 3)
                    {
                        if (!lines[3].StartsWith("BytesAllocated: "))
                        {
                            throw new FormatException(nameof(table.TestResult_Text));
                        }
                        this.BytesAllocated = long.Parse(lines[3].Substring("BytesAllocated: ".Length));
                        if (!lines[4].StartsWith("Allocations: "))
                        {
                            throw new FormatException(nameof(table.TestResult_Text));
                        }
                        this.Allocations = int.Parse(lines[4].Substring("Allocations: ".Length));
                    }
                    else
                    {
                        this.BytesAllocated = null;
                        this.Allocations = null;
                    }
                }
                else
                {
                    this.MBPerSecond = null;
                    if (lines.Length > 2)
                    {
                        if (!lines[2].StartsWith("BytesAllocated: "))
                        {
                            throw new FormatException(nameof(table.TestResult_Text));
                        }
                        this.BytesAllocated = long.Parse(lines[2].Substring("BytesAllocated: ".Length));
                        if (!lines[3].StartsWith("Allocations: "))
                        {
                            throw new FormatException(nameof(table.TestResult_Text));
                        }
                        this.Allocations = int.Parse(lines[3].Substring("Allocations: ".Length));
                    }
                    else
                    {
                        this.BytesAllocated = null;
                        this.Allocations = null;
                    }
                }
            }

            public override string ToString()
            {
                var mb = this.MBPerSecond.HasValue ? $"MBPerSecond: {this.MBPerSecond}\n" : "";
                var alloc = this.BytesAllocated.HasValue ? $"BytesAllocated: {this.BytesAllocated}\nAllocations: {this.Allocations}\n" : "";
                return $"Nanoseconds: {this.Nanoseconds}\nIterations: {this.Iterations}\n{mb}{alloc}";
            }
        }

        protected override void CommandLineOutput(IOperationExecutionContext context, string text)
        {
            var match = BenchmarkResultPattern.Match(text);
            if (match.Success)
            {
                this.FinishedTest(context);
                var result = new BenchmarkResult(match);
                var end = DateTime.UtcNow;
                var start = end - TimeSpan.FromSeconds((double)(result.Iterations * result.Nanoseconds / 1000000000));
                DB.BuildTestResults_RecordTestResult(context.ExecutionId, this.Package, $"Benchmark{result.Name}-{result.CPUs}", Domains.TestStatusCodes.Passed, result.ToString(), start, end);
                base.CommandLineOutput(context, text);
                return;
            }

            match = TestStartPattern.Match(text);
            if (match.Success)
            {
                this.FinishedTest(context);
                base.CommandLineOutput(context, text);
                return;
            }

            match = TestEndPattern.Match(text);
            if (match.Success)
            {
                this.FinishedTest(context);
                this.lastTestFinished = DateTime.UtcNow;
                this.lastTest = match;
                this.testOutput = new StringBuilder();
                base.CommandLineOutput(context, text);
                return;
            }

            if (this.testOutput != null)
            {
                if (text.StartsWith("\t"))
                {
                    this.testOutput.AppendLine(text);
                }
                else
                {
                    this.FinishedTest(context);
                }
            }

            base.CommandLineOutput(context, text);
        }

        private void FinishedTest(IOperationExecutionContext context)
        {
            if (this.lastTest == null)
            {
                return;
            }
            var match = this.lastTest;
            var end = this.lastTestFinished.Value;
            var start = end.AddSeconds(-double.Parse(match.Groups[5].Value));
            var status = Domains.TestStatusCodes.Inconclusive;
            if (string.Equals(match.Groups[1].Value, "PASS", StringComparison.OrdinalIgnoreCase))
            {
                status = Domains.TestStatusCodes.Passed;
            }
            else if (string.Equals(match.Groups[1].Value, "FAIL", StringComparison.OrdinalIgnoreCase))
            {
                status = Domains.TestStatusCodes.Failed;
            }
            DB.BuildTestResults_RecordTestResult(context.ExecutionId, this.Package, match.Groups[2].Value, status, this.testOutput.ToString(), start, end);
            this.lastTest = null;
            this.lastTestFinished = null;
            this.testOutput = null;
        }
#endif
    }
}
