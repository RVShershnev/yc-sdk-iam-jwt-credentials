using System;
using System.IO;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Threading;
using Grpc.Net.Client;
using Yandex.Cloud.Credentials;
using Yandex.Cloud.Iam.V1;

#if NETSTANDARD2_0
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
#endif

namespace YandexCloud.IamJwtCredentials
{
    /// <summary>
    /// Provides IAM credentials by generating JWT tokens signed with an RSA private key.
    /// </summary>
    /// <remarks>
    /// This implementation handles the end-to-end process of:
    /// <list type="bullet">
    /// <item><description>Parsing RSA private keys from PEM strings (supporting PKCS#1 and PKCS#8).</description></item>
    /// <item><description>Cross-framework cryptographic compatibility (.NET Standard 2.0/2.1 and .NET 5+).</description></item>
    /// <item><description>Creating signed JWT tokens for service account authentication.</description></item>
    /// </list>
    /// </remarks>
    public class IamJwtCredentialsProvider : ICredentialsProvider
    {
        /// <summary>
        /// <para>The default base endpoint for the Yandex Cloud IAM API.</para>
        /// <para>Used to construct the 'Audience' claim in the JWT and to direct identity service requests.</para>
        /// </summary>
        public const string IamApiCloud = "iam.api.cloud.yandex.net";

        private readonly string _keyId;
        private readonly string _pemCertificate;
        private readonly string _serviceAccountId;
        private readonly IamTokenService.IamTokenServiceClient _tokenService;

#if NETSTANDARD2_0
        private CreateIamTokenResponse _iamToken;
#elif NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        private CreateIamTokenResponse? _iamToken;
#endif

        /// <summary>
        /// Makes <see cref="GetToken"/> thread safe
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// <para>The time buffer in seconds before the actual token expiration to trigger a preemptive refresh.</para>
        /// <para>This prevents token rejection due to network latency or minor clock desynchronization.</para>
        /// <remarks>Ignored if value is less than 30. Minimum of 30 seconds will be applied</remarks>
        /// </summary>
        public int TokenExpirationBufferSeconds { get; set; } = 300;

        public IamJwtCredentialsProvider(string serviceAccountId, string keyId, string pemCertificate,
            IamTokenService.IamTokenServiceClient tokenService)
        {
            _serviceAccountId = serviceAccountId;
            _keyId = keyId;
            _pemCertificate = pemCertificate;
            _tokenService = tokenService;
        }

        public IamJwtCredentialsProvider(string serviceAccountId, string keyId, string pemCertificate)
            : this(serviceAccountId, keyId, pemCertificate, null)
        {
            _tokenService = TokenService();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IamJwtCredentialsProvider"/> class using the specified configuration.
        /// </summary>
        /// <param name="configuration">
        /// The credential configuration containing the Key ID, Service Account ID and the PEM-formatted private key.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
        public IamJwtCredentialsProvider(IamJwtCredentialsConfiguration configuration)
            : this(configuration?.ServiceAccountId, configuration?.Id, configuration?.PrivateKey)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            TokenExpirationBufferSeconds = configuration.TokenExpirationBufferSeconds;
        }

        /// <summary>
        /// Retrieves a valid IAM token, automatically refreshing it if it is expired or nearing expiration.
        /// </summary>
        /// <remarks>
        /// The method checks the current cached token's expiration time.
        /// If the token is null or expires within <see cref="TokenExpirationBufferSeconds"/>,
        /// it generates a new JWT and requests a fresh IAM token from the identity service.
        /// </remarks>
        /// <returns>A valid IAM token string to be used in authorization headers.</returns>
        public string GetToken()
        {
            var expiration = DateTimeOffset.Now.ToUnixTimeSeconds() + Math.Max(30, TokenExpirationBufferSeconds);
            if (_iamToken == null || _iamToken.ExpiresAt.Seconds <= expiration)
            {
                _semaphore.Wait();
                try 
                {
                    if (_iamToken == null || _iamToken.ExpiresAt.Seconds <= expiration)
                    {
                        _iamToken = _tokenService.Create(new CreateIamTokenRequest
                        {
                            Jwt = GetJwtToken()
                        });
                    }
                }
                finally
                { _semaphore.Release(); }
            }
            return _iamToken.IamToken;
        }

        /// <summary>
        /// Generates a new signed JWT token for IAM authentication.
        /// </summary>
        /// <returns>A string representing the encoded and signed JWT token.</returns>
        public string GetJwtToken()
        {
            var handler = new JsonWebTokenHandler();
            var now = DateTime.UtcNow;

            using (var rsa = RSA.Create())
            {
                ImportKeyFromPem(rsa, _pemCertificate);

                var key = new RsaSecurityKey(rsa.ExportParameters(true))
                {
                    KeyId = _keyId
                };

                var descriptor = new SecurityTokenDescriptor
                {
                    Issuer = _serviceAccountId,
                    Audience = $"https://{IamApiCloud}/iam/v1/tokens",
                    IssuedAt = now,
                    NotBefore = now,
                    Expires = now.AddMinutes(60),
                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSsaPssSha256)
                };

                return handler.CreateToken(descriptor);
            }
        }

        private void ImportKeyFromPem(RSA rsa, string pem)
        {
#if NET5_0_OR_GREATER
            rsa.ImportFromPem(pem);
#elif NETSTANDARD2_1_OR_GREATER
            // For .NET Standard 2.1 (using embedded byte parsing)
            var keyBytes = PreparePemBytes(pem, out var isPkcs8);
            if (isPkcs8)
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            else
                rsa.ImportRSAPrivateKey(keyBytes, out _);
#else
            using (var reader = new StringReader(pem))
            {
                var pemReader = new PemReader(reader);
                var keyObject = pemReader.ReadObject();

                // Process as PKCS#1 (RsaPrivateCrtKeyParameters) as well as PKCS#8 (AsymmetricCipherKeyPair)
                RsaPrivateCrtKeyParameters privateKey;
                if (keyObject is AsymmetricCipherKeyPair keyPair)
                    privateKey = (RsaPrivateCrtKeyParameters)keyPair.Private;
                else
                    privateKey = (RsaPrivateCrtKeyParameters)keyObject;

                var rsaParams = DotNetUtilities.ToRSAParameters(privateKey);
                rsa.ImportParameters(rsaParams);
            }
#endif
        }

#if NETSTANDARD2_1_OR_GREATER && !NET5_0_OR_GREATER
        private const string HEADER_RSA = "-----BEGIN RSA PRIVATE KEY-----"; // PKCS#1
        private const string FOOTER_RSA = "-----END RSA PRIVATE KEY-----";
        private const string HEADER_PKCS8 = "-----BEGIN PRIVATE KEY-----"; // PKCS#8
        private const string FOOTER_PKCS8 = "-----END PRIVATE KEY-----";

        private static byte[] PreparePemBytes(string pem, out bool isPkcs8)
        {
            int startIdx, endIdx;
            if (pem.Contains(HEADER_RSA))
            {
                isPkcs8 = false;
                startIdx = pem.IndexOf(HEADER_RSA, StringComparison.Ordinal) + HEADER_RSA.Length;
                endIdx = pem.IndexOf(FOOTER_RSA, StringComparison.Ordinal);
            }
            else if (pem.Contains(HEADER_PKCS8))
            {
                isPkcs8 = true;
                startIdx = pem.IndexOf(HEADER_PKCS8, StringComparison.Ordinal) + HEADER_PKCS8.Length;
                endIdx = pem.IndexOf(FOOTER_PKCS8, StringComparison.Ordinal);
            }
            else
                throw new InvalidOperationException("Unsupported PEM format. Expected PKCS#1 or PKCS#8.");

            if (startIdx < 0 || endIdx < 0 || endIdx <= startIdx)
                throw new FormatException("Invalid PEM structure: missing headers or footers.");

            // IMPORTANT: Remove all extra symbols
            var base64 = pem.Substring(startIdx, endIdx - startIdx)
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(" ", "")
                .Trim();

            try 
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new FormatException($"Base64 conversion failed. Cleaned string length: {base64.Length}. Check for invalid characters.", ex);
            }
        }
#endif

        private static IamTokenService.IamTokenServiceClient TokenService()
        {
            var channel = GrpcChannel.ForAddress($"https://{IamApiCloud}");
            return new IamTokenService.IamTokenServiceClient(channel);
        }
    }
}