using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HttpStreamer
{
    /// <summary>
    /// Listen for http stream and saves result to output
    /// </summary>
    public class HttpStreamListener
    {
        private string url;
        private string host;
        private System.Collections.Concurrent.ConcurrentQueue<string> queue;
        private readonly ILogger<HttpStreamListener> logger;
        private bool started = false;
        private bool stop = false;


        public HttpStreamListener(ILogger<HttpStreamListener> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="url"></param>
        /// <param name="host"></param>
        /// <param name="queue"></param>
        public HttpStreamListener(string url, string host, System.Collections.Concurrent.ConcurrentQueue<string> queue, ILogger<HttpStreamListener> logger)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Url is missing", nameof(url));
            }

            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Domain is missing", nameof(host));
            }

            this.url = url;
            this.host = host;
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.logger = logger;
        }

        /// <summary>
        /// Starts listen for http streaming
        /// </summary>
        public void Start()
        {
            if (this.started)
                throw new InvalidOperationException("Already started");


            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException("Url is missing");
            }

            if (string.IsNullOrEmpty(host))
            {
                throw new InvalidOperationException("Domain is missing");
            }

            if (queue == null)
                throw new InvalidOperationException("Queue is missing");


            ThreadPool.QueueUserWorkItem(HttpStreamListener.Runner, this);
            this.started = true;
            this.stop = false;
        }

        public void Start(string url, string host, ConcurrentQueue<string> queue)
        {
            if (this.started)
                throw new InvalidOperationException("Already started");


            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Url is missing", nameof(url));
            }

            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Domain is missing", nameof(host));
            }

            if (queue == null)
                throw new ArgumentNullException(nameof(queue));

            this.url = url;
            this.host = host;
            this.queue = queue;

            this.Start();
        }

        public void Stop()
        {
            this.stop = true;
        }
        private static void Runner(object state)
        {
            var listener = (HttpStreamListener)state;
            try
            {
                listener.logger?.LogInformation("Starting http stream listener");
                using (TcpClient client = new TcpClient())
                {
                    string requestString = $"GET {listener.url} HTTP/1.1\r\n";
                    //requestString += "Authorization: " + token + "\r\n";
                    requestString += $"Host: {listener.host}\r\n";
                    requestString += "Connection: keep-alive\r\n";
                    requestString += "\r\n";

                    client.Connect(listener.host, 80);

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Send the request.
                        StreamWriter writer = new StreamWriter(stream);
                        writer.Write(requestString);
                        writer.Flush();

                        // Process the response.
                        StreamReader rdr = new StreamReader(stream);

                        var isReadingHeader = true;

                        while (!rdr.EndOfStream)
                        {
                            var line = rdr.ReadLine();
                            if (isReadingHeader)
                            {
                                if (line == "\r\n\r\n")
                                {
                                    isReadingHeader = false;
                                }
                                else if (line.Length > 0 && line[0] == '{')
                                {
                                    isReadingHeader = false;
                                    listener.queue.Enqueue(line);
                                    listener.logger?.LogInformation("Added item to queue");
                                    listener.logger?.LogDebug($"Added item to queue: {line}");
                                }
                            }
                            else
                            {
                                listener.queue.Enqueue(line);
                                listener.logger?.LogInformation("Added item to queue");
                                listener.logger?.LogDebug($"Added item to queue: {line}");
                            }

                            if (listener.stop)
                                break;
                        }
                    }

                    listener.started = false;
                }
            }
            catch (Exception ex)
            {
                listener.logger?.LogError(ex, $"Error listening for http data stream");
            }

            listener.logger?.LogInformation("Stopping http stream listener");

        }
    }
}
