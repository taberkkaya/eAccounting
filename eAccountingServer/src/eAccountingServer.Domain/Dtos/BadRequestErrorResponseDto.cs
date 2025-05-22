using System.Text.Json.Serialization;

namespace eAccountingServer.Domain.Dtos
{
    public sealed class BadRequestErrorResponseDto
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = default!;
        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; } = default!;
    }
}
