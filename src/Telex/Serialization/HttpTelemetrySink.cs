using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Telex.Serialization
{
    internal sealed class HttpTelemetrySink : ITelemetrySink
    {
        private const int DefaultTimeoutMilliseconds = 5000;
        private const int DefaultMaxQueueDepth = 256;

        private readonly object gate = new object();
        private readonly Queue<TelemetryRecord> queue = new Queue<TelemetryRecord>();
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private readonly string baseUrl;
        private readonly int timeoutMilliseconds;
        private readonly int maxQueueDepth;

        private Thread worker;
        private bool running;

        public HttpTelemetrySink()
            : this(
                GetSetting("TELEX_HTTP_URL", "http://127.0.0.1:2145/ingest"),
                GetIntSetting("TELEX_HTTP_TIMEOUT_MS", DefaultTimeoutMilliseconds),
                GetIntSetting("TELEX_HTTP_MAX_QUEUE", DefaultMaxQueueDepth))
        {
        }

        public HttpTelemetrySink(string baseUrl, int timeoutMilliseconds, int maxQueueDepth)
        {
            this.baseUrl = baseUrl;
            this.timeoutMilliseconds = timeoutMilliseconds;
            this.maxQueueDepth = maxQueueDepth;
        }

        public void Open()
        {
            if (IsEnabled("TELEX_HTTP_INSECURE_TLS"))
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }

            running = true;
            worker = new Thread(DrainQueue);
            worker.IsBackground = true;
            worker.Name = "Telex HTTP telemetry";
            worker.Start();

            Debug.Log("[Telex] HTTP telemetry sink ready for " + baseUrl);
        }

        public void Write(TelemetryRecord record)
        {
            if (record == null)
            {
                return;
            }

            lock (gate)
            {
                if (!running)
                {
                    return;
                }

                if (queue.Count >= maxQueueDepth)
                {
                    queue.Dequeue();
                    Debug.LogWarning("[Telex] HTTP telemetry queue full; dropped oldest record");
                }

                queue.Enqueue(record);
            }

            wake.Set();
        }

        public void Close()
        {
            lock (gate)
            {
                running = false;
            }

            wake.Set();

            if (worker != null && worker.IsAlive)
            {
                worker.Join(2000);
            }

            worker = null;
        }

        private void DrainQueue()
        {
            while (true)
            {
                var record = Dequeue();
                if (record == null)
                {
                    lock (gate)
                    {
                        if (!running && queue.Count == 0)
                        {
                            return;
                        }
                    }

                    wake.WaitOne(1000);
                    continue;
                }

                try
                {
                    Post(record);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Telex] HTTP publish failed for '" + record.Type + "': " + ex.Message);
                }
            }
        }

        private TelemetryRecord Dequeue()
        {
            lock (gate)
            {
                if (queue.Count == 0)
                {
                    return null;
                }

                return queue.Dequeue();
            }
        }

        private void Post(TelemetryRecord record)
        {
            var url = AppendTypeQuery(baseUrl, record.Type);
            var body = TelemetryJson.Serialize(record);
            var bytes = Encoding.UTF8.GetBytes(body);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            request.ContentLength = bytes.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var status = (int)response.StatusCode;
                if (status < 200 || status >= 300)
                {
                    throw new InvalidOperationException("HTTP " + status);
                }
            }
        }

        private static string AppendTypeQuery(string url, string type)
        {
            var separator = url.IndexOf('?') >= 0 ? "&" : "?";
            return url + separator + "program=telex&type=" + Uri.EscapeDataString(type ?? "unknown");
        }

        private static string GetSetting(string name, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static int GetIntSetting(string name, int fallback)
        {
            var value = Environment.GetEnvironmentVariable(name);
            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : fallback;
        }

        private static bool IsEnabled(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
