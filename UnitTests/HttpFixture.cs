using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using WebApp;
using Xunit;

namespace TestServerRepro
{
    public class HttpFixture
    {
        private HttpMessageHandler _messageHandler;

        public HttpFixture()
        {
            var builder = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .ConfigureServices(serviceCollection =>
                {
                    // ensure that HttpClients
                    // use a message handler for the test server
                    serviceCollection
                        .AddHttpClient(Options.DefaultName)
                        .ConfigurePrimaryHttpMessageHandler(() => _messageHandler);

                    serviceCollection.PostConfigure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        options => options.BackchannelHttpHandler = _messageHandler);
                }); ;

            var host = new TestServer(builder);

            _messageHandler = host.CreateHandler();

            HttpClient = host.CreateClient();
        }
        
        public HttpClient HttpClient { get; }

        public async Task<string> CreateAsync(string uri, string value)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));
            message.Content = new StringContent(value);

            HttpResponseMessage response = await HttpClient.SendAsync(message);

            Assert.True(response.IsSuccessStatusCode);

            return await response.Content.ReadAsStringAsync();
        }

        public async Task SetupAuth()
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "ro.client"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "api1"),
            };

            var formContent = new FormUrlEncodedContent(values);

            HttpResponseMessage tokenResponse = await HttpClient.PostAsync("/connect/token", formContent);

            var tokenJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());

            var bearerToken = tokenJson["access_token"].Value<string>();
            HttpClient.SetBearerToken(bearerToken);
        }
    }
}
