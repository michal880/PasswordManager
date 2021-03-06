using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Paccia;

namespace WebServer
{
    class HmacRequestValidator
    {
        static readonly Lazy<string> EmptyStringMd5 = new Lazy<string>(() => string.Empty.ToUtf8Bytes().ToBase64Md5Hash());

        public static Task<bool> ValidateAsync(HttpRequest request, string authorization, HmacAuthenticationOptions options, IMemoryCache memoryCache, Lazy<byte[]> secretKeyBytes)
        {
            var authenticationHeader = AuthenticationHeaderValue.Parse(authorization);

            if (!authenticationHeader.Scheme.EqualsInsensitiveCase(HmacAuthenticationHandler.SchemeName))
                return Task.FromResult(false);

            var parameters = authenticationHeader.Parameter.Split(':');

            if (parameters.Length != 4)
                return Task.FromResult(false);

            var appId = parameters[0];
            var signatureHash = parameters[1];
            var nonce = parameters[2];
            var requestTimeStamp = parameters[3];

            return ValidateContentAsync(request, appId, signatureHash, nonce, requestTimeStamp, options, memoryCache, secretKeyBytes);
        }

        static async Task<bool> ValidateContentAsync(HttpRequest request, string appId, string requestSignatureHash, string nonce, string requestTimeStamp, HmacAuthenticationOptions options, IMemoryCache memoryCache, Lazy<byte[]> secretKeyBytes)
        {
            if (options.AppId != appId)
                return false;

            if (ReplayRequestBouncer.IsReplayRequest(memoryCache, options, nonce, requestTimeStamp))
                return false;

            var content = await GetRequestBodyMd5(request).ConfigureAwait(false);

            var data = HmacHasher.GetDataToHash(request, appId, nonce, requestTimeStamp, content);

            var signatureHash = data.ToUtf8Bytes()
                                    .ToBase64HmacSha256Hash(secretKeyBytes.Value);

            return requestSignatureHash.Equals(signatureHash, StringComparison.Ordinal);
        }

        static async Task<string> GetRequestBodyMd5(HttpRequest request)
        {
            var memoryStream = await request.Body.ToMemoryStreamAsync().ConfigureAwait(false);

            request.Body = memoryStream;

            return memoryStream.Length == 0 ?
                   EmptyStringMd5.Value :
                   memoryStream.ToArray()
                               .ToBase64Md5Hash();
        }
    }
}