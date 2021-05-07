using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Goldlight.HttpClientTestSupportTests
{
    public static class RestExtensions
    {
        private static readonly string BaseUrl = "http://www.dumyurl.com/api/dummy";

        public static Task<HttpResponseMessage> PostWrapperAsync(this HttpClient httpClient, string content)
        {
            string convertedContent = JsonConvert.SerializeObject(content);
            StringContent stringContent = new StringContent(convertedContent, Encoding.UTF8, "application/json");
            return httpClient.PostAsync(BaseUrl, stringContent);
        }
    }
}
