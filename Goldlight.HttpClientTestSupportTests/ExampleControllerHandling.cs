using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Goldlight.HttpClientTestSupportTests
{
  public class ExampleControllerHandling
  {
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://www.goldlight-dummy.com/api/sample/";
    public ExampleControllerHandling(HttpClient httpClient)
    {
      _httpClient = httpClient;
    }

    public async Task<SampleModel> GetById(Guid id)
    {
      HttpResponseMessage response = await _httpClient.GetAsync(BaseUrl + id);
      switch (response.StatusCode)
      {
        case HttpStatusCode.OK:
          return JsonConvert.DeserializeObject<SampleModel>(await response.Content.ReadAsStringAsync());
        case HttpStatusCode.BadRequest:
          throw new Exception("Unable to find " + id);
      }
      return null;
    }

    public async Task<IEnumerable<SampleModel>> GetAll()
    {
      HttpResponseMessage response = await _httpClient.GetAsync(BaseUrl);
      if (response.StatusCode == HttpStatusCode.OK && response.Headers.TryGetValues("order66", out IEnumerable<string> headers))
      {
        if (headers.First() == "babyyoda")
        {
          return JsonConvert.DeserializeObject<IEnumerable<SampleModel>>(await response.Content.ReadAsStringAsync());
        }
      }

      return null;
    }
  }
}
