using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Goldlight.HttpClientTestSupport
{
    /// <summary>
    /// An <see cref="HttpMessageHandler" /> implementation that allows us to mock HTTP calls for <see cref="HttpClient"/> calls.
    /// </summary>
    /// <remarks>
    /// <para>When we want to unit test HttpClient methods, we quickly run into the limitation that the actual HttpClient calls, such as PostAsync,
    /// are not directly mockable. This means that we have a limitation whereby tests appear to need a live endpoint to test against and this
    /// does not support the principles of unit testing where the tests are isolated from external sources. To work around this issue, 
    /// <see cref="HttpClient"/> accepts <see cref="HttpMessageHandler"/> instances. Internally, all the calls are routed via the SendAsync
    /// method so all we need to do is provide an implementation of this that can handle our test expectations.</para>
    /// <para>This implementation provides the ability to cope with any content that is being transmitted in the request Content,
    /// as well as augmenting the response message to include the version, status codes and response headers.</para>
    /// </remarks>
    /// <example>
    /// In this example, the <see cref="FakeHttpMessageHandler"/> is populated with an expectation that a valid ETag will be returned from
    /// a Post operation
    /// <code>
    /// [Fact]
    /// public async Task ValidateEagIsSetInResponseHeader()
    /// {
    ///   FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithResponseHeader("ETag", "\"33a64df551425fcc55e4d42a148795d9f25f89d4\"");
    ///   HttpClient httpClient = new HttpClient(fake);
    ///   string convertedContent = JsonConvert.SerializeObject("MyContent");
    ///   StringContent stringContent = new StringContent(convertedContent, Encoding.UTF8, "application/json");
    ///   HttpResponseMessage response = await httpClient.PostWrapperAsync("http://www.dummyurl.com", stringContent);
    ///   Assert.Equal("\"33a64df551425fcc55e4d42a148795d9f25f89d4\"", response.Headers.ETag.Tag);
    /// }
    /// </code>
    /// </example>
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private readonly Lazy<Header> _header = new Lazy<Header>(() => new Header());
#if NETSTANDARD2_1
        private readonly Lazy<TrailingHeader> _trailingHeader = new Lazy<TrailingHeader>(() => new TrailingHeader());
#endif
        private string _content;
        private Version _version;
        private static readonly Version DefaultVersion = new Version(1, 0);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content != null)
            {
                await request.Content.ReadAsStringAsync();
            }

            HttpResponseMessage responseMessage = new HttpResponseMessage
            {
                StatusCode = _statusCode,
                RequestMessage = request,
                Content = new StringContent(_content ?? string.Empty),
                Version = _version ?? DefaultVersion
            };
            if (_header.IsValueCreated)
            {
                _header.Value.AddHeadersToResponse(responseMessage);
            }
#if NETSTANDARD2_1
            if (_trailingHeader.IsValueCreated)
            {
                _trailingHeader.Value.AddHeadersToResponse(responseMessage);
            }
#endif
            return responseMessage;
        }

        public FakeHttpMessageHandler WithStatusCode(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            return this;
        }

        public FakeHttpMessageHandler WithResponseHeader(string key, string value)
        {
            _header.Value.AddResponseHeader(key, value);
            return this;
        }

        public FakeHttpMessageHandler WithResponseHeader(string key, IEnumerable<string> value)
        {
            _header.Value.AddResponseHeader(key, value);
            return this;
        }

#if NETSTANDARD2_1
        public FakeHttpMessageHandler WithTrailingResponseHeader(string key, string value)
        {
            _trailingHeader.Value.AddResponseHeader(key, value);
            return this;
        }

        public FakeHttpMessageHandler WithTrailingResponseHeader(string key, IEnumerable<string> value)
        {
            _trailingHeader.Value.AddResponseHeader(key, value);
            return this;
        }
#endif

        public FakeHttpMessageHandler WithExpectedContent(string content)
        {
            _content = content;
            return this;
        }

        public FakeHttpMessageHandler WithExpectedContent<T>(T content) where T : class
        {
          string converted = JsonSerializer.Serialize(content);
          _content = converted;
          return this;
        }

        public FakeHttpMessageHandler WithVersion(Version version)
        {
            _version = version;
            return this;
        }

        public FakeHttpMessageHandler WithVersion(string version)
        {
            _version = new Version(version);
            return this;
        }

        public FakeHttpMessageHandler WithVersion(int major, int minor)
        {
            _version = new Version(major, minor);
            return this;
        }
    }
}
