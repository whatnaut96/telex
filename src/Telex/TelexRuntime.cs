using System;
using Telex.Instrumentation;
using Telex.Serialization;
using UnityEngine;

namespace Telex
{
    internal static class TelexRuntime
    {
        private static CitySampler sampler;
        private static ITelemetrySink sink;
        private static float elapsed;
        private static int lastGameDay = -1;
        private static bool running;

        public static void Start(CitySampler citySampler, ITelemetrySink telemetrySink)
        {
            Stop();

            sampler = citySampler;
            sink = telemetrySink;
            elapsed = 0f;
            lastGameDay = -1;
            running = true;
            sink.Open();
        }

        public static void Stop()
        {
            if (!running)
            {
                return;
            }

            running = false;

            try
            {
                if (sink != null)
                {
                    sink.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Telex] failed to close telemetry sink: " + ex);
            }
            finally
            {
                sampler = null;
                sink = null;
                elapsed = 0f;
                lastGameDay = -1;
            }
        }

        public static void Update(float realTimeDelta, float simulationTimeDelta)
        {
            if (!running || sampler == null || sink == null)
            {
                return;
            }

            elapsed += realTimeDelta;

            var gameDay = sampler.GetCurrentGameDay();
            if (gameDay == lastGameDay)
            {
                return;
            }

            var interval = elapsed;
            elapsed = 0f;
            lastGameDay = gameDay;

            try
            {
                var records = sampler.Sample(interval, simulationTimeDelta);
                for (var i = 0; i < records.Count; i++)
                {
                    sink.Write(records[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Telex] sample failed: " + ex);
            }
        }
    }
}
