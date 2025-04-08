using System.Text.Json.Serialization;

namespace CodeCompilerService.Models
{
    public class CompileRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string? Input { get; set; }
    }
} 