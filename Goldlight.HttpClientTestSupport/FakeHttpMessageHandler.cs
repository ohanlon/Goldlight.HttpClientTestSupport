using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Goldlight.HttpClientTestSupport
{

  internal class HttpActions
  {
    private readonly List<Action> _actions = new List<Action>();

    public void AddAction(Action action)
    {
      _actions.Add(action);
    }

    public void InvokeAll()
    {
      foreach (Action action in _actions)
      {
        action?.Invoke();
      }
    }
  }

  internal class HttpRequestAssertions
  {
    private readonly List<Func<HttpRequestMessage, bool>> _asserts = new List<Func<HttpRequestMessage, bool>>();

    public void AddAction(Func<HttpRequestMessage, bool> requestAssertion)
    {
      _ = requestAssertion ?? throw new ArgumentNullException(nameof(requestAssertion));
      _asserts.Add(requestAssertion);
    }

    public void InvokeAll(HttpRequestMessage request)
    {
      foreach (var assert in _asserts)
      {
        if (!assert(request))
        {
          throw new HttpRequestAssertionException();
        }
      }
    }
  }

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

    private readonly Lazy<HttpActions> _preActions =
      new Lazy<HttpActions>(() => new HttpActions());

    private readonly Lazy<HttpRequestAssertions> _requestAssertions =
      new Lazy<HttpRequestAssertions>(() => new HttpRequestAssertions());

    private readonly Lazy<HttpActions> _postActions =
      new Lazy<HttpActions>(() => new HttpActions());

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      HttpResponseMessage responseMessage = null;
      try
      {
        if (_preActions.IsValueCreated)
        {
          _preActions.Value.InvokeAll();
        }
        if (_requestAssertions.IsValueCreated)
        {
          _requestAssertions.Value.InvokeAll(request);
        }
        if (request.Content != null)
        {
          await request.Content.ReadAsStringAsync();
        }

        responseMessage = new HttpResponseMessage
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
      }
      catch (Exception ex) when (ex is not HttpRequestAssertionException)
      {
        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
      }
      if (_postActions.IsValueCreated)
      {
        _postActions.Value.InvokeAll();
      }
      return responseMessage;
    }

    /// <summary>
    /// Add an action that will happen at the start of the <see cref="SendAsync"/> call.
    /// </summary>
    /// <remarks>
    /// If you need to add multiple actions, you can append multiple WithPreRequest calls
    /// together. The invocation of these actions happens sequentially, following the order
    /// they were added.
    /// </remarks>
    /// <param name="action">The action to perform.</param>
    public FakeHttpMessageHandler WithPreRequest(Action action)
    {
      _preActions.Value.AddAction(action);
      return this;
    }


    public FakeHttpMessageHandler WithRequestValidator(Func<HttpRequestMessage, bool> requestValidator)
    {
      _requestAssertions.Value.AddAction(requestValidator);
      return this;
    }

    /// <summary>
    /// Add an action that will happen at the end of the <see cref="SendAsync"/> call.
    /// </summary>
    /// <remarks>
    /// If you need to add multiple actions, you can append multiple WithPostRequest calls
    /// together. The invocation of these actions happens sequentially, following the order
    /// they were added.
    /// </remarks>
    /// <param name="action">The action to perform.</param>
    public FakeHttpMessageHandler WithPostRequest(Action action)
    {
      _postActions.Value.AddAction(action);
      return this;
    }

    /// <summary>
    /// Set the <see cref="HttpStatusCode"/> that will be returned on the response.
    /// </summary>
    /// <param name="statusCode">The status code being returned.</param>
    public FakeHttpMessageHandler WithStatusCode(HttpStatusCode statusCode)
    {
      _statusCode = statusCode;
      return this;
    }

    /// <summary>
    /// Add a response header.
    /// </summary>
    /// <param name="key">The header key</param>
    /// <param name="value">The header value</param>
    public FakeHttpMessageHandler WithResponseHeader(string key, string value)
    {
      _header.Value.AddResponseHeader(key, value);
      return this;
    }

    /// <summary>
    /// Add a multi-value response header.
    /// </summary>
    /// <param name="key">The header key</param>
    /// <param name="value">The header values</param>
    public FakeHttpMessageHandler WithResponseHeader(string key, IEnumerable<string> value)
    {
      _header.Value.AddResponseHeader(key, value);
      return this;
    }

#if NETSTANDARD2_1
    /// <summary>
    /// Add a trailing response header.
    /// </summary>
    /// <param name="key">The header key</param>
    /// <param name="value">The header value</param>
    public FakeHttpMessageHandler WithTrailingResponseHeader(string key, string value)
    {
      _trailingHeader.Value.AddResponseHeader(key, value);
      return this;
    }

    /// <summary>
    /// Add a multi-value trailing response header.
    /// </summary>
    /// <param name="key">The header key</param>
    /// <param name="value">The header value</param>
    public FakeHttpMessageHandler WithTrailingResponseHeader(string key, IEnumerable<string> value)
    {
      _trailingHeader.Value.AddResponseHeader(key, value);
      return this;
    }
#endif

    /// <summary>
    /// Set the content that is expected in the response.
    /// </summary>
    /// <param name="content">The content to populate the response.</param>
    public FakeHttpMessageHandler WithExpectedContent(string content)
    {
      _content = content;
      return this;
    }

    /// <summary>
    /// Set the content that is expected in the response.
    /// </summary>
    /// <param name="content">The content to populate the response.</param>
    public FakeHttpMessageHandler WithExpectedContent<T>(T content) where T : class
    {
      string converted = JsonSerializer.Serialize(content);
      _content = converted;
      return this;
    }

    /// <summary>
    /// Set the version details for the response.
    /// </summary>
    /// <param name="version">The populated version.</param>
    public FakeHttpMessageHandler WithVersion(Version version)
    {
      _version = version;
      return this;
    }

    /// <summary>
    /// Set the version details for the response.
    /// </summary>
    /// <param name="version">The populated version.</param>
    public FakeHttpMessageHandler WithVersion(string version)
    {
      _version = new Version(version);
      return this;
    }

    /// <summary>
    /// Set the version details for the response.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version</param>
    public FakeHttpMessageHandler WithVersion(int major, int minor)
    {
      _version = new Version(major, minor);
      return this;
    }
  }
}
