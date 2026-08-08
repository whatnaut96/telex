using System;
using System.Collections.Generic;
using System.Reflection;

namespace Telex.Instrumentation
{
    internal static class BuildingIndustrySampler
    {
        private static readonly string[] BuildingFields =
        {
            "m_customBuffer1",
            "m_customBuffer2",
            "m_cashBuffer",
            "m_productionRate",
            "m_tempImport",
            "m_tempExport",
            "m_finalImport",
            "m_finalExport",
            "m_cargoTrafficRate",
            "m_incomingProblemTimer",
            "m_outgoingProblemTimer",
            "m_workerProblemTimer"
        };

        private static readonly string[] AiFields =
        {
            "m_incomingResource",
            "m_outputResource",
            "m_storageType",
            "m_industryType",
            "m_campusType",
            "m_inputResource1",
            "m_inputResource2",
            "m_inputResource3",
            "m_inputResource4",
            "m_extractRate",
            "m_outputRate",
            "m_outputVehicleCount",
            "m_truckCount",
            "m_storageCapacity",
            "m_productionCycleDuration",
            "m_productionRate",
            "m_materialProduction",
            "m_goodsCapacity",
            "m_productionCapacity",
            "m_goodsSellPrice",
            "m_goodsConsumptionRate",
            "m_storageBufferSize",
            "m_rawResource",
            "m_refinedResource1",
            "m_refinedResource2"
        };

        public static object Sample(ushort buildingId, Building building, BuildingInfo info)
        {
            var data = new Dictionary<string, object>();

            CaptureFields(building, BuildingFields, data);

            if (info != null && info.m_buildingAI != null)
            {
                var ai = info.m_buildingAI;
                data["ai_type"] = ai.GetType().Name;
                data["is_industry_or_campus"] = IsInterestingAi(ai.GetType().Name);
                CaptureFields(ai, AiFields, data);
                CapturePublicSimpleFields(ai, data);
            }

            return data.Count == 0 ? null : data;
        }

        private static bool IsInterestingAi(string name)
        {
            if (name == null)
            {
                return false;
            }

            return name.IndexOf("Industry", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Industrial", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Extracting", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Extractor", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Warehouse", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Campus", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Park", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void CaptureFields(object target, string[] fieldNames, IDictionary<string, object> data)
        {
            if (target == null)
            {
                return;
            }

            var type = target.GetType();
            for (var i = 0; i < fieldNames.Length; i++)
            {
                var field = type.GetField(fieldNames[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    continue;
                }

                data[ToSnakeName(field.Name)] = ConvertValue(field.GetValue(target));
            }
        }

        private static void CapturePublicSimpleFields(object target, IDictionary<string, object> data)
        {
            var fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (var i = 0; i < fields.Length; i++)
            {
                if (!IsSimple(fields[i].FieldType))
                {
                    continue;
                }

                var name = ToSnakeName(fields[i].Name);
                if (!data.ContainsKey(name))
                {
                    data[name] = ConvertValue(fields[i].GetValue(target));
                }
            }
        }

        private static bool IsSimple(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
        }

        private static object ConvertValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            var type = value.GetType();
            return type.IsEnum ? value.ToString() : value;
        }

        private static string ToSnakeName(string name)
        {
            return name != null && name.StartsWith("m_") ? name.Substring(2) : name;
        }
    }
}
