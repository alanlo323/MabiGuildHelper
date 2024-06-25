using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using DiscordBot.Configuration;
using DiscordBot.Extension;
using DiscordBot.SemanticKernel;
using DiscordBot.Util;
using DocumentFormat.OpenXml.InkML;
using Google.Protobuf.WellKnownTypes;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;

namespace DiscordBot.ReverseProxy
{
    public class MabinogiProxy(ILogger<MabinogiProxy> logger, IOptionsSnapshot<ReverseProxyConfig> reverseProxyConfig) : IHostedService
    {
        private WebApplication? Proxy { get; set; }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting proxy");
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddHttpForwarder();
            Proxy = builder.Build();
            foreach (var mapping in reverseProxyConfig.Value.Mappings) Proxy.Urls.Add($"http://localhost:{mapping.Port}");

            // Rest of the code...
            // Configure our own HttpMessageInvoker for outbound calls for proxy operations
            var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                EnableMultipleHttp2Connections = true,
                ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
                ConnectTimeout = TimeSpan.FromSeconds(15),
            });

            // Setup our own request transform class
            var transformer = new CustomTransformer(); // or HttpTransformer.Default;
            var requestConfig = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };

            // When using extension methods for registering IHttpForwarder providing configuration, transforms, and HttpMessageInvoker is optional (defaults will be used).
            //Proxy.MapForwarder("/{**catch-all}", "*", requestConfig, transformer, httpClient);

            // When using IHttpForwarder for direct forwarding you are responsible for routing, destination discovery, load balancing, affinity, etc..
            // For an alternate example that includes those features see BasicYarpSample.
            Proxy.Map("/Ping", () => $"Pong!");
            Proxy.Map("/StartClient/{argsPath}", async (string argsPath) => await StartClient(argsPath));
            Proxy.Map("/{**catch-all}", async (HttpContext httpContext, IHttpForwarder forwarder) =>
            {
                var req = httpContext.Request;
                string destinationPrefix = req.Host.Value switch
                {
                    "mabinogi.nexon.net" => "https://mabinogi.nexon.net",
                    _ => "https://www.google.com",
                };
                var error = await forwarder.SendAsync(httpContext, $"locas", httpClient, requestConfig, transformer);
                // Check if the operation was successful
                if (error != ForwarderError.None)
                {
                    var errorFeature = httpContext.GetForwarderErrorFeature();
                    var exception = errorFeature?.Exception;
                    logger.LogException(exception);
                }
            });
            await Proxy.StartAsync(cancellationToken);

            logger.LogInformation($"Proxy started");
            logger.LogInformation($"Url: {Proxy.Urls.Aggregate((s1, s2) => $"{s1} {s2}")}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stoping proxy");
            await Proxy!.StopAsync(cancellationToken: cancellationToken);
            logger.LogInformation($"Proxy stopped");
        }

        internal async Task StartClient(string argsPath)
        {
            await Task.Run(async () =>
            {
                try
                {
                    FileInfo argsFile = new(argsPath);
                    string[] args = File.ReadAllLines(argsFile.FullName);
                    Dictionary<string, string> mapping = args.ToDictionary(x => x.Split(":")[0], x => x.Split(":")[1]);
                    string arg = args.Aggregate((s1, s2) => $"{s1} {s2}");
                    { }
                    string mabinogiFolder = "G:\\Nexon\\Mabinogi";
                    FileInfo realExe = new(Path.Combine(mabinogiFolder, "Client.bak.exe"));
                    if (!realExe.Exists)
                    {
                        logger.LogError($"{realExe.Name} not found");
                        return;
                    }

                    logger.LogInformation($"Starting process: {realExe.FullName}");
                    MiscUtil.LaunchCommandLineApp(realExe, args.Aggregate((s1, s2) => $"{s1} {s2}"));
                    logger.LogInformation($"Process started");
                }
                catch (Exception ex)
                {
                    logger.LogException(ex);
                }
            });
        }
    }
}
