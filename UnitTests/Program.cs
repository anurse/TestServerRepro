using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            HttpClient client = null;
            var builder = WebHost.CreateDefaultBuilder()
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        var accessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                        if (context.Request.Path.StartsWithSegments("/inner"))
                        {
                            if (accessor.HttpContext == null)
                            {
                                throw new System.Exception("Invalid During Nested Call!");
                            }
                            Console.WriteLine($"[INNER] HttpContextAccessor.HttpContext.Request.Path = {accessor.HttpContext.Request.Path}");
                        }
                        else if (context.Request.Path.StartsWithSegments("/outer"))
                        {
                            Console.WriteLine($"[OUTER] (before) HttpContextAccessor.HttpContext.Request.Path = {accessor.HttpContext.Request.Path}");

                            var nestedResp = await client.GetAsync("/inner");
                            if (accessor.HttpContext == null)
                            {
                                Console.WriteLine("HttpContextAccessor NULL after nested!");
                            }
                            else
                            {
                                Console.WriteLine("HttpContextAccessor NON-NULL after nested!");
                                Console.WriteLine($"[OUTER] (after) HttpContextAccessor.HttpContext.Request.Path = {accessor.HttpContext.Request.Path}");
                            }
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                });
            Console.WriteLine($"Runtime Path: {typeof(string).Assembly.Location}");
            Console.WriteLine($"ASP.NET Path: {typeof(HttpContext).Assembly.Location}");
            var server = new TestServer(builder);
            var handler = server.CreateHandler();
            client = new HttpClient(new SuppressExecutionContextHandler(handler)) { BaseAddress = new Uri("http://localhost") };
            var resp = await client.GetAsync("/outer");
            resp.EnsureSuccessStatusCode();
        }
    }
}
