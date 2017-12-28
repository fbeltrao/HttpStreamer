using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace HttpStreamer
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables()                               
                .Build();

            var serviceCollection = new ServiceCollection()
                .AddSingleton(new LoggerFactory()
                .AddConsole());
            serviceCollection.AddLogging();
            ConfigureServices(serviceCollection, config);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.GetService<Application>().Run();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            
            services.AddSingleton<IConfiguration>(config);
            services.AddTransient<EventHubProducer>();
            services.AddTransient<HttpStreamListener>();
            services.AddTransient<Application>();
        }
    }
}
