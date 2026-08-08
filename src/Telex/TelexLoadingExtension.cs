using ICities;
using Telex.Instrumentation;
using Telex.Serialization;
using UnityEngine;

namespace Telex
{
    public sealed class TelexLoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            TelexRuntime.Start(new CitySampler(), new HttpTelemetrySink());
            Debug.Log("[Telex] telemetry started");
        }

        public override void OnLevelUnloading()
        {
            TelexRuntime.Stop();
            Debug.Log("[Telex] telemetry stopped");
        }
    }
}
