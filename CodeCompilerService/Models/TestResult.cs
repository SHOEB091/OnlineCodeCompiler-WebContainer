using System.Text.Json.Serialization;

namespace CodeCompilerService.Models
{
    public class TestCaseResult
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("expectedOutput")]
        public string ExpectedOutput { get; set; } = string.Empty;

        [JsonPropertyName("actualOutput")]
        public string? ActualOutput { get; set; }

        [JsonPropertyName("passed")]
        public bool Passed { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("executionTime")]
        public long ExecutionTime { get; set; }
    }

    public class TestResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("testResults")]
        public List<TestCaseResult> TestResults { get; set; } = new();

        [JsonPropertyName("totalTests")]
        public int TotalTests { get; set; }

        [JsonPropertyName("passedTests")]
        public int PassedTests { get; set; }

        [JsonPropertyName("compilationError")]
        public string? CompilationError { get; set; }

        [JsonPropertyName("totalExecutionTime")]
        public long TotalExecutionTime { get; set; }
    }
} 