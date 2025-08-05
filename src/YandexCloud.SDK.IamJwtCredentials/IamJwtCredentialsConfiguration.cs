using System.Text.Json.Serialization;

namespace YandexCloud.IamJwtCredentials
{
    public class IamJwtCredentialsConfiguration
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("service_account_id")]
        public string ServiceAccountId { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("public_key")]
        public string? PublicKey { get; set; }

        [JsonPropertyName("private_key")]
        public string PrivateKey { get; set; }
    }
}
