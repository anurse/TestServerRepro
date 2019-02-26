using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace TestServerRepro
{
    public class UnitTest: IClassFixture<HttpFixture>
    {
        private HttpFixture _fixture;

        public UnitTest(HttpFixture fixture)
        {
            _fixture = fixture;
            Client = fixture.HttpClient;
        }

        protected HttpClient Client { get; set; }

        [Fact]
        public async Task GetTest()
        {
            await _fixture.SetupAuth();
            var response = await Client.GetAsync("/api/values");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostTest()
        {
            await _fixture.SetupAuth();
            var response = await _fixture.CreateAsync("/api/values", "test");

            Assert.Equal("test", response);
        }
    }
}
