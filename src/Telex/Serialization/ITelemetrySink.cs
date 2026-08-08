namespace Telex.Serialization
{
    internal interface ITelemetrySink
    {
        void Open();
        void Write(TelemetryRecord record);
        void Close();
    }
}
