using System.Text.Json.Serialization;

namespace HPDcePro.Mcp.Models
{
    public sealed class DceProToolResult
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        public static DceProToolResult Success(object data)
        {
            return new DceProToolResult
            {
                IsSuccess = true,
                Data = data,
                ErrorMessage = null
            };
        }

        public static DceProToolResult Fail(string errorMessage)
        {
            return new DceProToolResult
            {
                IsSuccess = false,
                Data = null,
                ErrorMessage = errorMessage
            };
        }
    }
}
