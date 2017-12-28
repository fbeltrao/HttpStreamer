using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace HttpStreamer
{
    public class Application
    {
        private readonly IConfiguration config;
        private readonly HttpStreamListener listener;
        private readonly EventHubProducer eventHubProducer;
        private readonly ILogger<Application> logger;

        public Application(ILogger<Application> logger, IConfiguration config, HttpStreamListener listener, EventHubProducer eventHubProducer)
        {
            this.config = config;
            this.listener = listener;
            this.eventHubProducer = eventHubProducer;
            this.logger = logger;
        }

        public void Run()
        {
            var streamURL = config["HTTPSTREAM_URL"];
            var streamHost = config["HTTPSTREAM_HOST"];

            if (string.IsNullOrEmpty(streamURL) || string.IsNullOrEmpty(streamHost))
            {
                Console.Error.WriteLine("Please provide HTTP stream details (url + host)");
                Environment.Exit(1);
            }

            var eventHub = config["HTTPSTREAM_EVENTHUB"];
            if (string.IsNullOrEmpty(eventHub))
            {
                Console.Error.WriteLine("Please provide Event Hub connection string");
                Environment.Exit(1);
            }


            this.logger?.LogInformation($"Starting streaming\nURL: {streamURL}\nHost: {streamHost}\nEvent Hub: {eventHub}");

            var data = new System.Collections.Concurrent.ConcurrentQueue<string>();
                        
            eventHubProducer.Start(eventHub, data);
            listener.Start(streamURL, streamHost, data);


            var endEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += delegate {
                endEvent.Set();
            };

            endEvent.WaitOne();

            eventHubProducer.Stop();
            listener.Stop();
        }
    }
}
