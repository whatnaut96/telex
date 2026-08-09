using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using Telex.Serialization;
using UnityEngine;

namespace Telex.Instrumentation
{
    internal sealed class CitySampler
    {
        private readonly DlcReflectionSampler dlcSampler;

        public CitySampler()
        {
            dlcSampler = new DlcReflectionSampler();
        }

        public int GetCurrentGameDay()
        {
            var simulation = Singleton<SimulationManager>.instance;
            if (simulation == null)
            {
                return -1;
            }

            return GetAbsoluteGameDay(simulation.m_currentGameTime);
        }

        public IList<TelemetryRecord> Sample(float realTimeInterval, float simulationTimeDelta)
        {
            var records = new List<TelemetryRecord>();
            records.Add(CreateRecord("economy", realTimeInterval, simulationTimeDelta, GenerateEconomySnapshot()));
            records.Add(CreateRecord("resources", realTimeInterval, simulationTimeDelta, GenerateResourceSnapshot()));
            records.Add(CreateRecord("buildings", realTimeInterval, simulationTimeDelta, GenerateBuildingSnapshot()));
            records.Add(CreateRecord("districts", realTimeInterval, simulationTimeDelta, GenerateDistrictSnapshot()));
            records.Add(CreateRecord("transport", realTimeInterval, simulationTimeDelta, GenerateTransportSnapshot()));
            records.Add(CreateRecord("dlc_managers", realTimeInterval, simulationTimeDelta, dlcSampler.Sample()));
            return records;
        }

        private static TelemetryRecord CreateRecord(string type, float realTimeInterval, float simulationTimeDelta, object data)
        {
            var simulation = Singleton<SimulationManager>.instance;

            var record = new TelemetryRecord();
            record.SchemaVersion = 1;
            record.Type = type;
            record.CityName = GetCityName();
            record.RealTimeIntervalSeconds = realTimeInterval;
            record.SimulationTimeDeltaSeconds = simulationTimeDelta;
            record.Data = data;

            if (simulation != null)
            {
                record.CurrentFrameIndex = simulation.m_currentFrameIndex;
                record.GameTime = simulation.m_currentGameTime;
                record.Date = simulation.m_currentGameTime.ToString("yyyy-MM-dd");
                record.AbsoluteDay = GetAbsoluteGameDay(simulation.m_currentGameTime);
            }

            return record;
        }

        private static object GenerateEconomySnapshot()
        {
            var economy = Singleton<EconomyManager>.instance;
            var population = Singleton<CitizenManager>.instance;

            var data = new Dictionary<string, object>();
            if (economy != null)
            {
                ReflectionCaptureSimple(economy, data, "LastCashAmount", "LastCashDelta", "m_cashAmount", "m_cashDelta", "m_taxRate");
            }

            if (population != null)
            {
                data["population"] = population.m_citizenCount;
            }

            return data;
        }

        private static object GenerateResourceSnapshot()
        {
            var data = new Dictionary<string, object>();
            data["transfer_reasons"] = TransferReasonSampler.Sample();
            data["natural_resources"] = ResourceManagerSampler.SampleNaturalResources();
            data["industry_areas"] = ResourceManagerSampler.SampleIndustryAreas();
            return data;
        }

        private static IList<object> GenerateBuildingSnapshot()
        {
            var manager = Singleton<BuildingManager>.instance;
            var records = new List<object>();
            if (manager == null)
            {
                return records;
            }

            var buildings = manager.m_buildings.m_buffer;
            for (ushort id = 1; id < buildings.Length; id++)
            {
                var building = buildings[id];
                if ((building.m_flags & Building.Flags.Created) == 0)
                {
                    continue;
                }

                var info = building.Info;
                var record = new Dictionary<string, object>();
                record["building_id"] = id;
                record["name"] = manager.GetBuildingName(id, InstanceID.Empty);
                record["prefab"] = info == null ? null : info.name;
                record["ai"] = info == null || info.m_buildingAI == null ? null : info.m_buildingAI.GetType().Name;
                record["position"] = Vector(building.m_position);
                record["level"] = building.m_level;
                record["flags"] = building.m_flags.ToString();
                record["problems"] = building.m_problems.ToString();
                record["fire_intensity"] = building.m_fireIntensity;
                record["health"] = building.m_health;
                record["garbage_buffer"] = building.m_garbageBuffer;
                record["water_buffer"] = building.m_waterBuffer;
                record["sewage_buffer"] = building.m_sewageBuffer;
                record["electricity_buffer"] = building.m_electricityBuffer;
                record["district_id"] = Singleton<DistrictManager>.instance.GetDistrict(building.m_position);
                record["industry"] = BuildingIndustrySampler.Sample(id, building, info);

                records.Add(record);
            }

            return records;
        }

        private static IList<object> GenerateDistrictSnapshot()
        {
            var manager = Singleton<DistrictManager>.instance;
            var records = new List<object>();
            if (manager == null)
            {
                return records;
            }

            var districts = manager.m_districts.m_buffer;
            for (byte id = 1; id < districts.Length; id++)
            {
                if ((districts[id].m_flags & District.Flags.Created) == 0)
                {
                    continue;
                }

                var record = new Dictionary<string, object>();
                record["district_id"] = id;
                record["name"] = manager.GetDistrictName(id);
                ReflectionCaptureSimple(districts[id], record, "m_policies", "m_servicePolicies", "m_cityPlanningPolicies", "m_taxationPolicies");
                records.Add(record);
            }

            return records;
        }

        private static object GenerateTransportSnapshot()
        {
            var manager = Singleton<TransportManager>.instance;
            var data = new Dictionary<string, object>();
            var lines = new List<object>();
            data["lines"] = lines;

            if (manager == null)
            {
                return data;
            }

            var buffer = manager.m_lines.m_buffer;
            for (ushort id = 1; id < buffer.Length; id++)
            {
                if ((buffer[id].m_flags & TransportLine.Flags.Created) == 0)
                {
                    continue;
                }

                var record = new Dictionary<string, object>();
                record["line_id"] = id;
                record["name"] = manager.GetLineName(id);
                record["flags"] = buffer[id].m_flags.ToString();
                record["stops"] = buffer[id].CountStops(id);
                record["vehicles"] = buffer[id].CountVehicles(id);
                record["passengers"] = buffer[id].m_passengers;
                lines.Add(record);
            }

            return data;
        }

        private static string GetCityName()
        {
            try
            {
                var simulation = Singleton<SimulationManager>.instance;
                if (simulation != null && simulation.m_metaData != null)
                {
                    return simulation.m_metaData.m_CityName;
                }
            }
            catch
            {
            }

            return null;
        }

        private static int GetAbsoluteGameDay(DateTime gameTime)
        {
            return (int)gameTime.Date.Subtract(new DateTime(gameTime.Year, 1, 1)).TotalDays;
        }

        private static object Vector(Vector3 value)
        {
            var result = new Dictionary<string, object>();
            result["x"] = value.x;
            result["y"] = value.y;
            result["z"] = value.z;
            return result;
        }

        private static void ReflectionCaptureSimple(object source, IDictionary<string, object> target, params string[] names)
        {
            if (source == null)
            {
                return;
            }

            var type = source.GetType();
            for (var i = 0; i < names.Length; i++)
            {
                var memberName = names[i];
                var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    target[NormalizeMemberName(memberName)] = ConvertSimple(field.GetValue(source));
                    continue;
                }

                var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    target[NormalizeMemberName(memberName)] = ConvertSimple(property.GetValue(source, null));
                }
            }
        }

        private static object ConvertSimple(object value)
        {
            return value != null && value.GetType().IsEnum ? value.ToString() : value;
        }

        private static string NormalizeMemberName(string name)
        {
            return name != null && name.StartsWith("m_") ? name.Substring(2) : name;
        }
    }
}
