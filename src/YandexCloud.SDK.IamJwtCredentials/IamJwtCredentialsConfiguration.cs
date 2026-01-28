using System;
using System.Text.Json.Serialization;

namespace YandexCloud.IamJwtCredentials
{
    /// <summary>
    /// Represents the configuration for service account IAM credentials used for JWT token generation
    /// </summary>
    public class IamJwtCredentialsConfiguration
    {
        /// <summary>
        /// <para>Unique identifier of the public key (Key ID).</para>
        /// <para>This value is mapped to the 'kid' (Key ID) field in the JWT header.</para>
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// <para>The unique ID of the service account.</para>
        /// <para>This value is used as the 'iss' (Issuer) claim in the JWT payload.</para>
        /// </summary>
        [JsonPropertyName("service_account_id")]
        public string ServiceAccountId { get; set; }

        /// <summary>
        /// <para>Timestamp indicating when the key pair was generated.</para>
        /// <para>Useful for implementing security policies such as automated key rotation.</para>
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// <para>The public key in PEM format.</para>
        /// <para>Optional: typically used for token verification rather than signing.</para>
        /// </summary>
#if NETSTANDARD2_0
        [JsonPropertyName("public_key")]
        public string PublicKey { get; set; }
#elif NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        [JsonPropertyName("public_key")]
        public string? PublicKey { get; set; }
#endif

        /// <summary>
        /// <para>The RSA private key in PEM format (PKCS#1 or PKCS#8).</para>
        /// <para>Used locally to sign the JWT.</para>
        /// <para>SECURITY WARNING: This value contains sensitive data and must be protected 
        /// using secure storage (e.g., Environment Variables, Azure Key Vault, or AWS Secrets Manager).</para>
        /// </summary>
        [JsonPropertyName("private_key")]
        public string PrivateKey { get; set; }

        /// <summary>
        /// <para>The time buffer in seconds before the actual token expiration to trigger a preemptive refresh.</para>
        /// <para>This prevents token rejection due to network latency or minor clock desynchronization.</para>
        /// <remarks>Ignored if value is less than 30. Minimum of 30 seconds will be applied</remarks>
        /// </summary>
        public int TokenExpirationBufferSeconds { get; set; } = 300;
    }
}
