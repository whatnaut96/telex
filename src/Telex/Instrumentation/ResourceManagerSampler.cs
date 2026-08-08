using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;

namespace Telex.Instrumentation
{
    internal static class ResourceManagerSampler
    {
        public static object SampleNaturalResources()
        {
            var manager = Singleton<NaturalResourceManager>.instance;
            var data = new Dictionary<string, object>();
            if (manager == null)
            {
                return data;
            }

            data["oil"] = SumResource(manager, NaturalResourceManager.Resource.Oil);
            data["ore"] = SumResource(manager, NaturalResourceManager.Resource.Ore);
            data["forest"] = SumResource(manager, NaturalResourceManager.Resource.Forest);
            data["fertility"] = SumResource(manager, NaturalResourceManager.Resource.Fertility);
            return data;
        }

        public static object SampleIndustryAreas()
        {
            return ReflectionSummary.SampleSingleton("DistrictParkManager");
        }

        private static long SumResource(NaturalResourceManager manager, NaturalResourceManager.Resource resource)
        {
            var total = 0L;
            var buffer = manager.m_naturalResources;
            var field = typeof(NaturalResourceManager.ResourceCell).GetField("m_" + resource, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                field = typeof(NaturalResourceManager.ResourceCell).GetField("m_" + resource.ToString().ToLowerInvariant(), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (field == null)
            {
                return total;
            }

            for (var i = 0; i < buffer.Length; i++)
            {
                var value = field.GetValue(buffer[i]);
                if (value != null)
                {
                    total += System.Convert.ToInt64(value);
                }
            }

            return total;
        }
    }
}
