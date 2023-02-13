using Grpc.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Yandex.Cloud.Credentials;
using Yandex.Cloud.Iam.V1;

namespace YandexCloud.IamJwtCredentials
{
    public class IamJwtCredentialsProvider : ICredentialsProvider
    {
        private const string IamApiCloud ="iam.api.cloud.yandex.net:443";

        private readonly string _keyId;
        private readonly string _pemCertificate;
        private readonly string _serviceAccountId;
        private readonly IamTokenService.IamTokenServiceClient _tokenService;
        private CreateIamTokenResponse _iamToken;

        public IamJwtCredentialsProvider(string serviceAccountId, string keyId, string pemCertificate)
        {
            _serviceAccountId = serviceAccountId;
            _keyId = keyId;
            _pemCertificate = pemCertificate;
            _tokenService = TokenService();
        }

        public IamJwtCredentialsProvider(IamJwtCredentialsConfiguration configuration)
            : this(configuration.ServiceAccountId, configuration.Id, configuration.PrivateKey) { }
        

        public IamJwtCredentialsProvider(string serviceAccountId, string keyId, string pemCertificate,
            IamTokenService.IamTokenServiceClient tokenService)
        {
            _serviceAccountId = serviceAccountId;
            _keyId = keyId;
            _pemCertificate = pemCertificate;
            _tokenService = tokenService;
        }

        public string GetToken()
        {
            var expiration = DateTimeOffset.Now.ToUnixTimeSeconds() + 300;
            if (_iamToken == null || _iamToken.ExpiresAt.Seconds <= expiration)
                _iamToken = _tokenService.Create(new CreateIamTokenRequest
                {
                    Jwt = GetJwtToken()
                });

            return _iamToken.IamToken;
        }

        private IamTokenService.IamTokenServiceClient TokenService()
        {
            var channel = new Grpc.Core.Channel(IamApiCloud, new SslCredentials());
            return new IamTokenService.IamTokenServiceClient(channel);
        }

        private string GetJwtToken()
        {
            var handler = new JsonWebTokenHandler();
            var now = DateTime.UtcNow;

            var rsa = RSA.Create();
            rsa.ImportFromPem(_pemCertificate);

            var key = new RsaSecurityKey(rsa.ExportParameters(true))
            {
                KeyId = _keyId
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _serviceAccountId,
                Audience = "https://iam.api.cloud.yandex.net/iam/v1/tokens",
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(60),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSsaPssSha256)
            };

            return handler.CreateToken(descriptor);
        }


    }
}