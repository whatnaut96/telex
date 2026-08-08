using ICities;

namespace Telex
{
    public sealed class TelexMod : IUserMod
    {
        public string Name
        {
            get { return "Telex"; }
        }

        public string Description
        {
            get { return "Low-overhead telemetry export for Cities: Skylines 1."; }
        }
    }
}
