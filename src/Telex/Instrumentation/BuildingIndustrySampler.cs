using System;
using System.Collections.Generic;
using System.Reflection;

namespace Telex.Instrumentation
{
    internal static class BuildingIndustrySampler
    {
        public static object Sample(ushort buildingId, Building building, BuildingInfo info)
        {
            if (info == null || info.m_buildingAI == null)
            {
                return null;
            }

            var ai = info.m_buildingAI;
            var aiType = ai.GetType().Name;
            if (!IsInterestingAi(aiType))
            {
                return null;
            }

            var data = new Dictionary<string, object>();
            data["classification"] = Classification(ai, aiType);
            data["materials"] = MaterialProfile(ai);
            data["production"] = ProductionProfile(building, ai);
            data["logistics"] = LogisticsProfile(building, ai);
            data["utilities"] = UtilityProfile(ai);
            data["employment"] = EmploymentProfile(ai);
            data["costs"] = CostProfile(ai);
            data["problems"] = ProblemProfile(building);
            return data;
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

        private static string IndustryRole(string aiType)
        {
            if (aiType.IndexOf("Extract", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "extractor";
            }
            if (aiType.IndexOf("Processing", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "processor";
            }
            if (aiType.IndexOf("Warehouse", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "storage";
            }
            if (aiType.IndexOf("MainIndustry", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "industry_area_main";
            }
            if (aiType.IndexOf("Auxiliary", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "industry_area_auxiliary";
            }
            if (aiType.IndexOf("Campus", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "campus";
            }
            if (aiType.IndexOf("Park", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "park_area";
            }

            return "industry_related";
        }

        private static object Classification(object ai, string aiType)
        {
            var data = new Dictionary<string, object>();
            data["ai_type"] = aiType;
            data["role"] = IndustryRole(aiType);
            AddIfPresent(data, "industry_area_type", ai, "m_industryType");
            AddIfPresent(data, "campus_type", ai, "m_campusType");
            return data;
        }

        private static object MaterialProfile(object ai)
        {
            var data = new Dictionary<string, object>();
            AddIfPresent(data, "incoming_resource", ai, "m_incomingResource");
            AddIfPresent(data, "output_resource", ai, "m_outputResource");
            AddIfPresent(data, "stored_resource", ai, "m_storageType");
            AddIfPresent(data, "raw_resource", ai, "m_rawResource");

            var inputs = new List<object>();
            AddResourceInput(inputs, ai, "m_inputResource1");
            AddResourceInput(inputs, ai, "m_inputResource2");
            AddResourceInput(inputs, ai, "m_inputResource3");
            AddResourceInput(inputs, ai, "m_inputResource4");
            if (inputs.Count > 0)
            {
                data["inputs"] = inputs;
            }

            var refined = new List<object>();
            AddResourceInput(refined, ai, "m_refinedResource1");
            AddResourceInput(refined, ai, "m_refinedResource2");
            if (refined.Count > 0)
            {
                data["refined_resources"] = refined;
            }

            return data.Count == 0 ? null : data;
        }

        private static object ProductionProfile(Building building, object ai)
        {
            var data = new Dictionary<string, object>();
            data["rate_percent"] = building.m_productionRate;
            AddIfPresent(data, "extract_rate", ai, "m_extractRate");
            AddIfPresent(data, "output_rate", ai, "m_outputRate");
            AddIfPresent(data, "cycle_duration_frames", ai, "m_productionCycleDuration");
            AddIfPresent(data, "material_production", ai, "m_materialProduction");
            AddIfPresent(data, "goods_capacity", ai, "m_goodsCapacity");
            AddIfPresent(data, "production_capacity", ai, "m_productionCapacity");
            AddIfPresent(data, "goods_consumption_rate", ai, "m_goodsConsumptionRate");
            return data.Count == 0 ? null : data;
        }

        private static object LogisticsProfile(Building building, object ai)
        {
            var data = new Dictionary<string, object>();
            data["cargo_traffic_rate"] = building.m_cargoTrafficRate;
            AddIfPresent(data, "output_vehicle_count", ai, "m_outputVehicleCount");
            AddIfPresent(data, "truck_count", ai, "m_truckCount");
            AddIfPresent(data, "storage_capacity", ai, "m_storageCapacity");
            AddIfPresent(data, "storage_buffer_size", ai, "m_storageBufferSize");

            var counters = new Dictionary<string, object>();
            counters["import_pending"] = building.m_tempImport;
            counters["import_committed"] = building.m_finalImport;
            counters["export_pending"] = building.m_tempExport;
            counters["export_committed"] = building.m_finalExport;
            data["transfer_counters"] = counters;
            return data.Count == 0 ? null : data;
        }

        private static object UtilityProfile(object ai)
        {
            var data = new Dictionary<string, object>();
            AddIfPresent(data, "electricity_consumption", ai, "m_electricityConsumption");
            AddIfPresent(data, "water_consumption", ai, "m_waterConsumption");
            AddIfPresent(data, "sewage_accumulation", ai, "m_sewageAccumulation");
            AddIfPresent(data, "garbage_accumulation", ai, "m_garbageAccumulation");
            return data.Count == 0 ? null : data;
        }

        private static object EmploymentProfile(object ai)
        {
            var capacity = new Dictionary<string, object>();
            var total = 0;
            total += AddWorkerCapacity(capacity, ai, "uneducated", "m_workPlaceCount0");
            total += AddWorkerCapacity(capacity, ai, "educated", "m_workPlaceCount1");
            total += AddWorkerCapacity(capacity, ai, "well_educated", "m_workPlaceCount2");
            total += AddWorkerCapacity(capacity, ai, "highly_educated", "m_workPlaceCount3");
            if (capacity.Count == 0)
            {
                return null;
            }

            var data = new Dictionary<string, object>();
            data["workplace_capacity_total"] = total;
            data["workplace_capacity_by_education"] = capacity;
            return data;
        }

        private static object CostProfile(object ai)
        {
            var data = new Dictionary<string, object>();
            AddIfPresent(data, "construction_cost", ai, "m_constructionCost");
            AddIfPresent(data, "maintenance_cost", ai, "m_maintenanceCost");
            AddIfPresent(data, "goods_sell_price", ai, "m_goodsSellPrice");
            return data.Count == 0 ? null : data;
        }

        private static object ProblemProfile(Building building)
        {
            var data = new Dictionary<string, object>();
            data["incoming_resource_timer"] = building.m_incomingProblemTimer;
            data["outgoing_resource_timer"] = building.m_outgoingProblemTimer;
            data["worker_timer"] = building.m_workerProblemTimer;
            return data;
        }

        private static void AddResourceInput(IList<object> inputs, object source, string fieldName)
        {
            var value = FieldValue(source, fieldName);
            if (value == null || value.ToString() == "None")
            {
                return;
            }

            inputs.Add(value);
        }

        private static int AddWorkerCapacity(IDictionary<string, object> target, object source, string key, string fieldName)
        {
            var value = FieldValue(source, fieldName);
            if (value == null)
            {
                return 0;
            }

            var count = Convert.ToInt32(value);
            target[key] = count;
            return count;
        }

        private static void AddIfPresent(IDictionary<string, object> target, string key, object source, string fieldName)
        {
            var value = FieldValue(source, fieldName);
            if (value != null)
            {
                target[key] = value;
            }
        }

        private static object FieldValue(object source, string fieldName)
        {
            if (source == null)
            {
                return null;
            }

            var field = source.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                return null;
            }

            return ConvertValue(field.GetValue(source));
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
    }
}
