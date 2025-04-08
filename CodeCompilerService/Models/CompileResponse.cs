using System.Text.Json.Serialization;

namespace CodeCompilerService.Models
{
    public class CompileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("output")]
        public string? Output { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("executionTime")]
        public long ExecutionTime { get; set; }
    }
} 