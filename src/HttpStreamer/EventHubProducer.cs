using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace HttpStreamer
{
    
    public class EventHubProducer
    {
        private string connectionString;
        private System.Collections.Concurrent.ConcurrentQueue<string> queue;
        private int blockSize = 1024 * 1024; // 1 MB
        bool started = false;
        bool stopped = false;
        ManualResetEvent stoppedEvent = new ManualResetEvent(false);
        private readonly ILogger<EventHubProducer> logger;

        public EventHubProducer(ILogger<EventHubProducer> logger)
        {
            this.logger = logger;
        }

        public EventHubProducer(string connectionString, System.Collections.Concurrent.ConcurrentQueue<string> queue, ILogger<EventHubProducer> logger)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            this.connectionString = connectionString;
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.logger = logger;
        }

        public void Start(string eventHub, ConcurrentQueue<string> queue)
        {
            this.connectionString = eventHub;
            this.queue = queue;

            this.Start();
        }

        public void Start()
        {
            if (this.started)
                throw new InvalidOperationException("Already started");


            if (string.IsNullOrEmpty(this.connectionString))
                throw new InvalidOperationException("Event hub connection string was not defined");

            if (queue == null)
                throw new InvalidOperationException("Internal queue was not defined");

            
            this.started = true;
            this.stopped = false;
            this.stoppedEvent.Reset();
            ThreadPool.QueueUserWorkItem(Runner, this);
        }

        public void Stop()
        {
            if (this.started)
            {
                this.stopped = true;
                this.stoppedEvent.WaitOne();
            }
        }


        static void Runner(object state)
        {

            var producer = (EventHubProducer)state;

            producer.logger?.LogInformation("Event hub producer started");

            EventHubClient client = null;
            try
            {
                client = EventHubClient.CreateFromConnectionString(producer.connectionString);
                byte[] memBlock = new byte[producer.blockSize];
                while (!producer.stopped)
                {
                    if (producer.queue.TryDequeue(out var item))
                    {
                        if (item.Length > 0 && item[0] == '{')
                        {
                            int byteCount = Encoding.UTF8.GetBytes(item, 0, Math.Min(memBlock.Length, item.Length), memBlock, 0);
                            var eventData = new EventData(new ArraySegment<byte>(memBlock, 0, byteCount));
                            client.SendAsync(eventData).GetAwaiter().GetResult();

                            producer.logger?.LogInformation($"Sent message to event hub ({byteCount} bytes)");
                        }
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex)
            {
                producer.logger?.LogError(ex, $"Error sending data to EventHub");
            }
            finally
            {
                client?.Close();
            }

            producer.logger?.LogInformation("Event hub producer stopped");

            producer.stopped = true;                 
            producer.started = false;
            producer.stoppedEvent.Set();
        }

        
    }
}
