using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Api
{
    public static class Host
    {
        private static readonly TestServer Server;
        private static readonly HttpClient ServerHttpClient;

        static Host()
        {

            var functionPath = Path.Combine(new FileInfo(typeof(Host).Assembly.Location).Directory.FullName, "..");
            Environment.SetEnvironmentVariable("HOST_FUNCTION_CONTENT_PATH", functionPath, EnvironmentVariableTarget.Process);
            //Directory.SetCurrentDirectory(functionPath);
            
            Server = new TestServer(WebHost
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    config
                        .SetBasePath(functionPath)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{builderContext.HostingEnvironment.EnvironmentName}.json",
                            optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();
                })
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .UseContentRoot(functionPath));

            ServerHttpClient = Server.CreateClient();
        }
        
        /// <summary>
        /// This trigger covers all routes, except those reserved by Azure Functions and will keep the portal running as expected.
        /// </summary>
        /// <returns>HttpResponse built by the WebHost.</returns>
        [FunctionName("AllPaths")]
        public static async Task<HttpResponseMessage> RunAllPaths(
            CancellationToken ct,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "{*x:regex(^(?!admin|debug|runtime).*$)}")]HttpRequestMessage req,
            ILogger log,
            ExecutionContext ctx)
        {
            return await ServerHttpClient.SendAsync(req, ct);
        }

        /// <summary>
        /// This trigger covers root route only which isn't caught by the other HttpTrigger.
        /// In order to have this working, the AppSettings require the following settings:
        ///    "AzureWebJobsDisableHomepage": "true"
        /// </summary>
        /// <returns>HttpResponse built by the WebHost.</returns>
        [FunctionName("Root")]
        public static async Task<HttpResponseMessage> RunRoot(
            CancellationToken ct,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "/")]HttpRequestMessage req,
            ILogger log,
            ExecutionContext ctx)
        {
            return await ServerHttpClient.SendAsync(req, ct);
        }
    }
}
