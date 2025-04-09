using System.Text.Json.Serialization;

namespace CodeCompilerService.Models
{
    public class TestCase
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("expectedOutput")]
        public string ExpectedOutput { get; set; } = string.Empty;
    }

    public class UserInputRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("userInput")]
        public string UserInput { get; set; } = string.Empty;

        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 5;

        [JsonPropertyName("memoryLimitMB")]
        public int MemoryLimitMB { get; set; } = 256;
    }

    public class UserInputResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("output")]
        public string Output { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("compilationError")]
        public string CompilationError { get; set; } = string.Empty;
    }

    public class TestRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("testCases")]
        public List<TestCase> TestCases { get; set; } = new();

        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 5;

        [JsonPropertyName("memoryLimitMB")]
        public int MemoryLimitMB { get; set; } = 256;
    }
} 