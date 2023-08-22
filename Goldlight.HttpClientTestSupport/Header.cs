using System;
using System.Collections.Generic;
using System.Net.Http;
#if NETSTANDARD2_1
using System.Net.Http.Headers;
#endif

namespace Goldlight.HttpClientTestSupport
{
    internal class Header
    {
        protected readonly Lazy<Dictionary<string, string>> Headers =
            new Lazy<Dictionary<string, string>>(() => new Dictionary<string, string>());
        protected readonly Lazy<Dictionary<string, IEnumerable<string>>> MultiValueHeaders =
            new Lazy<Dictionary<string, IEnumerable<string>>>(() => new Dictionary<string, IEnumerable<string>>());

        internal void AddResponseHeader(string key, string value)
        {
            Headers.Value[key] = value;
        }

        internal void AddResponseHeader(string key, IEnumerable<string> value)
        {
            MultiValueHeaders.Value[key] = value;
        }

        internal virtual void AddHeadersToResponse(HttpResponseMessage responseMessage)
        {
            if (Headers.IsValueCreated)
            {
                foreach (var header in Headers.Value)
                {
                    responseMessage.Headers.Add(header.Key, header.Value);
                }
            }

            if (MultiValueHeaders.IsValueCreated)
            {
                foreach (var header in MultiValueHeaders.Value)
                {
                    responseMessage.Headers.Add(header.Key, header.Value);
                }
            }
        }
    }

#if !NETSTANDARD2_0
    internal class TrailingHeader : Header
    {
        internal override void AddHeadersToResponse(HttpResponseMessage responseMessage)
        {
            if (Headers.IsValueCreated)
            {
                foreach (var header in Headers.Value)
                {
                    responseMessage.TrailingHeaders.Add(header.Key, header.Value);
                }
            }

            if (MultiValueHeaders.IsValueCreated)
            {
                foreach (var header in MultiValueHeaders.Value)
                {
                    responseMessage.TrailingHeaders.Add(header.Key, header.Value);
                }
            }
        }
    }
#endif
}