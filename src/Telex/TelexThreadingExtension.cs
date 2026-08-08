using ICities;

namespace Telex
{
    public sealed class TelexThreadingExtension : ThreadingExtensionBase
    {
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            TelexRuntime.Update(realTimeDelta, simulationTimeDelta);
        }
    }
}
