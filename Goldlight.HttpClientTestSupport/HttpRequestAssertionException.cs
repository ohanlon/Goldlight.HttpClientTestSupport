using System;

namespace Goldlight.HttpClientTestSupport
{
  /// <summary>
  /// Exception thrown when <see cref="FakeHttpMessageHandler.WithRequestValidator(Func{System.Net.Http.HttpRequestMessage, bool})"/> returns false.
  /// </summary>
  public class HttpRequestAssertionException : Exception
  {
    private const string AssertionMessage = "Request message validation returned failure.";

    /// <summary>
    /// Creates a new instance of <see cref="HttpRequestAssertionException"/>, with a custom error message.
    /// </summary>
    public HttpRequestAssertionException() : base(AssertionMessage)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="HttpRequestAssertionException"/>, with a custom error message.
    /// </summary>
    public HttpRequestAssertionException(Exception exception) : base(AssertionMessage, exception)
    {
    }
  }
}
