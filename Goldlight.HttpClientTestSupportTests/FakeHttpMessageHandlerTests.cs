using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Goldlight.HttpClientTestSupport;
using Newtonsoft.Json;
using Xunit;
using Xunit.Sdk;

namespace Goldlight.HttpClientTestSupportTests
{
  public class FakeHttpMessageHandlerTests
  {
    [Fact]
    public async Task GivenValidRequestWithDefaultsOnly_WhenGetIsCalled_ThenStatusIsOK()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler();
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage responseMessage = await httpClient.GetAsync("https://dummyaddress.com/someapi");
      Assert.Equal(HttpStatusCode.OK, responseMessage.StatusCode);
    }

    [Fact]
    public async Task GivenValidRequestWithModelContentExpected_WhenGetIsCalled_ThenContentIsSetToModel()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithExpectedContent(new SampleModel());
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage responseMessage = await httpClient.GetAsync("https://dummyaddress.com/someapi");
      SampleModel converted =
        JsonConvert.DeserializeObject<SampleModel>(await responseMessage.Content.ReadAsStringAsync());
      Assert.Equal("Stan Lee", converted.FullName);
    }

    [Fact]
    public async Task GivenExpectedContentWithSerializationOptions_WhenGetIsCalled_SerializationOptionsUsed()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithExpectedContent(new SampleModel(), new JsonSerializerOptions() { IgnoreReadOnlyProperties = true });
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage responseMessage = await httpClient.GetAsync("https://dummyaddress.com/someapi");
      string result = await responseMessage.Content.ReadAsStringAsync();
      Assert.Equal("{}", result);
    }

    [Fact]
    public async Task GivenExpectedContentWithoutSerializationOptions_WhenGetIsCalled_UsesDefaultSerializationOptions()
    {
        FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithExpectedContent(new SampleModel(), null);
        HttpClient httpClient = new HttpClient(fake);
        HttpResponseMessage responseMessage = await httpClient.GetAsync("https://dummyaddress.com/someapi");
        string result = await responseMessage.Content.ReadAsStringAsync();
        Assert.Equal(@"{""FirstName"":""Stan"",""LastName"":""Lee"",""FullName"":""Stan Lee""}", result);
    }

    [Fact]
    public async Task GivenValidRequestWithEtagHeaderAsResponse_WhenPostIsCalled_ThenEtagTagIsSet()
    {
      FakeHttpMessageHandler fake =
        new FakeHttpMessageHandler().WithResponseHeader("ETag", "\"33a64df551425fcc55e4d42a148795d9f25f89d4\"");
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
      Assert.Equal("\"33a64df551425fcc55e4d42a148795d9f25f89d4\"", response.Headers.ETag.Tag);
    }

    [Fact]
    public async Task GivenValidRequestWithNoVersion_WhenPostIsCalled_ThenDefaultVersionIsApplied()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler();
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
      Assert.Equal(response.Version, new Version(1, 0));
    }

    [Fact]
    public async Task GivenValidRequestWithCustomVersion_WhenPostIsCalled_ThenCustomVersionIsSet()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithVersion(new Version(2, 1));
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
      Assert.Equal(response.Version, new Version(2, 1));
    }

    [Fact]
    public async Task GivenValidRequestWithNoResponseContent_WhenPostIsCalled_ThenContentIsEmpty()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler();
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
      Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GivenValidRequestWithResponseContent_WhenPostIsCalled_ThenContentIsSet()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithExpectedContent("Response");
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
      Assert.Equal("Response", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GivenValidRequestWithStringVersion_WhenPostIsCalled_ThenCustomVersionIsSet()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithVersion("2.1");
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
      Assert.Equal(new Version(2, 1), response.Version);
    }

    [Fact]
    public async Task GivenValidRequestWithMajorMinorVersion_WhenPostIsCalled_ThenCustomVersionIsSet()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithVersion(2, 1);
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
      Assert.Equal(new Version(2, 1), response.Version);
    }

    [Fact]
    public async Task GivenValidRequestWithContent_WhenPostIsCalled_ThenContentIsSet()
    {
      string content = "{\"content\":\"set\"}";
      string returnedContent = "\"{\\\"content\\\":\\\"set\\\"}\""; // This is the converted Content inside the request
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler();
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync(content);
      Assert.Equal(returnedContent, await response.RequestMessage.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GivenValidRequestWithTrailingHeader_WhenPostIsCalled_ThenTrailingHeaderIsSet()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithTrailingResponseHeader("custom", "value");
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("Content");
      Assert.Equal("value", response.TrailingHeaders.GetValues("custom").First());
    }

    [Fact]
    public async Task GivenValidRequestWithMultiValueTrailingHeader_WhenPostIsCalled_ThenTrailingHeaderIsSet()
    {
      FakeHttpMessageHandler fake =
        new FakeHttpMessageHandler().WithTrailingResponseHeader("custom", new[] { "value", "value2" });
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("Content");
      Assert.Equal(2, response.TrailingHeaders.GetValues("custom").Count());
    }

    [Fact]
    public async Task GivenValidRequestWithMultiValueHeader_WhenPostIsCalled_ThenHeaderIsSet()
    {
      FakeHttpMessageHandler fake =
        new FakeHttpMessageHandler().WithResponseHeader("custom", new[] { "value", "value2" });
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("Content");
      Assert.Equal(2, response.Headers.GetValues("custom").Count());
    }

    [Fact]
    public async Task GivenValidRequestWithBadRequestStatus_WhenPostIsCalled_ThenBadRequestIsReturnedAsStatus()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler().WithStatusCode(HttpStatusCode.BadRequest);
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
      Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GivenInvalidRequest_WhenRequestValidatorPerformed_ExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidator(request => request.Headers.TryGetValues("Authorization", out var _));
      HttpClient httpClient = new HttpClient(fake);
      await Assert.ThrowsAsync<HttpRequestAssertionException>(() => httpClient.PostWrapperAsync("MyContent"));
    }

    [Fact]
    public async Task GivenValidRequest_WhenRequestValidatorPerformed_NoExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidator(request => request.Method == HttpMethod.Post);
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
    }

    [Fact]
    public async Task GivenValidRequest_WhenRequestValidatorReturnsTrue_NoExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidator(request => true);
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
    }

    [Fact]
    public async Task GivenInvalidRequest_WhenRequestValidatorReturnsFalse_ExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidator(request => false);
      HttpClient httpClient = new HttpClient(fake);
      await Assert.ThrowsAsync<HttpRequestAssertionException>(() => httpClient.PostWrapperAsync("MyContent"));
    }

    [Fact]
    public async Task GivenValidRequest_WhenRequestValidatorNotThrows_NoExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidator<XunitException>(request => Assert.NotNull(request));
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
    }

    [Fact]
    public async Task GivenInvalidRequest_WhenRequestValidatorThrows_ExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidator<XunitException>(request => Assert.Null(request));
      HttpClient httpClient = new HttpClient(fake);
      await Assert.ThrowsAsync<HttpRequestAssertionException>(() => httpClient.PostWrapperAsync("MyContent"));
    }

    [Fact]
    public async Task GivenValidRequestWithAssertion_WhenRequestValidatorThrowsNonAssertion_NoExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidator<InvalidOperationException>(request => Assert.Equal(HttpMethod.Get, request.Method));
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
    }

    [Fact]
    public async Task GivenInvalidRequest_WhenAsyncRequestValidatorPerformed_ExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidatorAsync(async request => await request.Content.ReadAsStringAsync() == null);
      HttpClient httpClient = new HttpClient(fake);
      await Assert.ThrowsAsync<HttpRequestAssertionException>(() => httpClient.PostWrapperAsync("MyContent"));
    }

    [Fact]
    public async Task GivenValidRequest_WhenAsyncRequestValidatorPerformed_NoExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidatorAsync(async request => await request.Content.ReadAsStringAsync() != null);
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
    }

    [Fact]
    public async Task GivenValidRequest_WhenAsyncRequestValidatorReturnsTrue_NoExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidatorAsync(request => Task.FromResult(true));
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
    }

    [Fact]
    public async Task GivenInvalidRequest_WhenAsyncRequestValidatorReturnsFalse_ExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidatorAsync(request => Task.FromResult(false));
      HttpClient httpClient = new HttpClient(fake);
      await Assert.ThrowsAsync<HttpRequestAssertionException>(() => httpClient.PostWrapperAsync("MyContent"));
    }

    [Fact]
    public async Task GivenInvalidRequestWithAssertion_WhenAsyncRequestValidatorReturnsFalse_ExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidatorAsync<XunitException>(async request => { Assert.NotNull(await request.Content.ReadAsStringAsync()); return false; });
      HttpClient httpClient = new HttpClient(fake);
      await Assert.ThrowsAsync<HttpRequestAssertionException>(() => httpClient.PostWrapperAsync("MyContent"));
    }

    [Fact]
    public async Task GivenValidRequestWithAssertion_WhenAsyncRequestValidatorReturnsTrue_NoExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidatorAsync<XunitException>(async request => { Assert.NotNull(await request.Content.ReadAsStringAsync()); return true; });
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
    }

    [Fact]
    public async Task GivenInvalidRequestWithAssertion_WhenAsyncRequestValidatorThrows_ExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidatorAsync<XunitException>(async request => Assert.Null(await request.Content.ReadAsStringAsync()));
      HttpClient httpClient = new HttpClient(fake);
      await Assert.ThrowsAsync<HttpRequestAssertionException>(() => httpClient.PostWrapperAsync("MyContent"));
    }

    [Fact]
    public async Task GivenValidRequestWithAssertion_WhenAsyncRequestValidatorDoesNotThrow_NoExceptionThrown()
    {
      FakeHttpMessageHandler fake = new FakeHttpMessageHandler()
        .WithRequestValidatorAsync<XunitException>(async request => { Assert.NotNull(await request.Content.ReadAsStringAsync()); });
      HttpClient httpClient = new HttpClient(fake);
      HttpResponseMessage response = await httpClient.PostWrapperAsync("MyContent");
    }
  }
}
