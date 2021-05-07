# Testing HttpClient
## Scenario
We have a series of unit tests and, in them, we want to test
a request from `HttpClient` but, in the spirit of true isolated tests, we don't 
want to actually perform the HTTP requests. We need some means of replacing
the real call to the end point with a fake endpoint in its place.

The problem in this scenario is that there is no apparently simple 
way to mock `HttpClient`. There is no `IHttpClient` interface available
to us, that will allow us to set up a mock and the calls we are interested aren't virtual so mocking them
is difficult in most mock frameworks. With this apparent limitation
in mind, how can we easily mock calls such as `PostAsync`? 

To answer this question, we need to understand how `HttpClient` actually
manages HTTP requests. If we look at the source code for `GetAsync` in
the [reference source](https://github.com/microsoft/referencesource/blob/master/System/net/System/Net/Http/HttpClient.cs)
we see that the method calls out to `SendAsync`.
```csharp
public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption,
  CancellationToken cancellationToken)
{
  return SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
}
```
`HttpClient` inherits from a class called [HttpMessageInvoker](https://github.com/microsoft/referencesource/blob/5697c29004a34d80acdaf5742d7e699022c64ecd/System/net/System/Net/Http/HttpMessageInvoker.cs#L11)
which has `SendAsync` as a virtual method inside it. The `HttpMessageInvoker` class
requires posting an `HttpMessageHandler` instance it via a constructor.
This is important to because `SendAsync` calls out to the `HttpMessageHandler.SendAsync` method
and this is the "touch point" that we want to interact with to mock our REST call.

All of this detective work has told us that, in order to replace our calls, we need to inherit
from `HttpMessageHandler` and provide our own implementation that satisfies the ability to
simulate web requests.
## Mocking the message handler
