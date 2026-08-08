using System;
using System.IO;
using System.Text;
using ColossalFramework.IO;

namespace Telex.Serialization
{
    internal sealed class JsonLinesTelemetrySink : ITelemetrySink
    {
        private StreamWriter writer;

        public void Open()
        {
            var directory = Path.Combine(DataLocation.localApplicationData, "Telex");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, "telemetry.jsonl");
            writer = new StreamWriter(path, true, Encoding.UTF8);
        }

        public void Write(TelemetryRecord record)
        {
            if (writer == null)
            {
                return;
            }

            writer.WriteLine(TelemetryJson.Serialize(record));
            writer.Flush();
        }

        public void Close()
        {
            if (writer == null)
            {
                return;
            }

            writer.Flush();
            writer.Dispose();
            writer = null;
        }
    }
}
