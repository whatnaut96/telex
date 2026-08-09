using System;

namespace Telex.Serialization
{
    internal sealed class TelemetryRecord
    {
        public int SchemaVersion;
        public string Type;
        public string CityName;
        public string Date;
        public DateTime GameTime;
        public uint CurrentFrameIndex;
        public int AbsoluteDay;
        public float RealTimeIntervalSeconds;
        public float SimulationTimeDeltaSeconds;
        public object Data;
    }
}
