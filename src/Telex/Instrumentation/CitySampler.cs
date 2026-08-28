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
        public CitySampler()
        {
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
            records.Add(CreateRecord("citizens", realTimeInterval, simulationTimeDelta, CitizenSampler.Sample()));
            records.Add(CreateRecord("roads", realTimeInterval, simulationTimeDelta, RoadSampler.Sample()));
            records.Add(CreateRecord("industry_areas", realTimeInterval, simulationTimeDelta, IndustryAreaSampler.Sample()));
            records.Add(CreateRecord("districts", realTimeInterval, simulationTimeDelta, GenerateDistrictSnapshot()));
            records.Add(CreateRecord("transport", realTimeInterval, simulationTimeDelta, GenerateTransportSnapshot()));
            return records;
        }

        private static TelemetryRecord CreateRecord(string type, float realTimeInterval, float simulationTimeDelta, object data)
        {
            var simulation = Singleton<SimulationManager>.instance;

            var record = new TelemetryRecord();
            record.Type = type;
            record.CityName = GetCityName();
            record.Data = data;

            if (simulation != null)
            {
                record.Date = simulation.m_currentGameTime.ToString("yyyy-MM-dd");
            }

            return record;
        }

        private static object GenerateEconomySnapshot()
        {
            return EconomySampler.Sample();
        }

        private static object GenerateResourceSnapshot()
        {
            var data = new Dictionary<string, object>();
            data["transfer_reasons"] = TransferReasonSampler.Sample();
            data["resources"] = TransferReasonSampler.SampleByResource();
            data["natural_resources"] = ResourceManagerSampler.SampleNaturalResources();
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
                var districtManager = Singleton<DistrictManager>.instance;
                var districtId = districtManager == null ? (byte)0 : districtManager.GetDistrict(building.m_position);
                var parkId = districtManager == null ? (byte)0 : districtManager.GetPark(building.m_position);
                var record = new Dictionary<string, object>();
                record["building_id"] = id;
                record["zone_type"] = ZoneType(info);
                record["service"] = ServiceType(info);
                record["sub_service"] = SubServiceType(info);
                record["position"] = Vector(building.m_position);
                record["garbage_buffer"] = building.m_garbageBuffer;
                record["water_buffer"] = building.m_waterBuffer;
                record["sewage_buffer"] = building.m_sewageBuffer;
                record["electricity_buffer"] = building.m_electricityBuffer;
                record["district"] = districtId == 0 ? null : (object)districtId;
                record["district_name"] = DistrictName(districtId);
                record["industry_area_id"] = parkId == 0 ? null : (object)parkId;
                record["industry_area_name"] = ParkName(districtManager, parkId);
                record["road_edge"] = building.m_accessSegment;
                record["curve_position"] = building.m_angle;
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

        public static object Vector(Vector3 value)
        {
            var result = new Dictionary<string, object>();
            result["x"] = value.x;
            result["y"] = value.y;
            result["z"] = value.z;
            return result;
        }

        public static string ZoneType(BuildingInfo info)
        {
            if (info == null || info.m_class == null)
            {
                return "unknown";
            }

            var service = info.m_class.m_service;
            var subService = info.m_class.m_subService.ToString();
            if (service == ItemClass.Service.Residential)
            {
                return "residential";
            }
            if (service == ItemClass.Service.Commercial)
            {
                return "commercial";
            }
            if (service == ItemClass.Service.Industrial)
            {
                return "zoned_industry";
            }
            if (service == ItemClass.Service.PlayerIndustry)
            {
                return "controlled_industry";
            }
            if (service == ItemClass.Service.Office)
            {
                return "office";
            }
            if (subService.IndexOf("Industrial", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "zoned_industry";
            }
            if (subService.IndexOf("PlayerIndustry", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "controlled_industry";
            }

            return "other";
        }

        private static string ServiceType(BuildingInfo info)
        {
            if (info == null || info.m_class == null)
            {
                return "unknown";
            }

            var service = info.m_class.m_service;
            if (service == ItemClass.Service.Industrial)
            {
                return "zoned_industry";
            }
            if (service == ItemClass.Service.PlayerIndustry)
            {
                return "controlled_industry";
            }

            return ToSnakeCase(service.ToString());
        }

        private static string SubServiceType(BuildingInfo info)
        {
            if (info == null || info.m_class == null)
            {
                return "none";
            }

            var subService = info.m_class.m_subService;
            if (subService == ItemClass.SubService.None)
            {
                return "none";
            }

            return ToSnakeCase(subService.ToString());
        }

        private static string DistrictName(byte districtId)
        {
            if (districtId == 0)
            {
                return GetCityName();
            }

            var manager = Singleton<DistrictManager>.instance;
            return manager == null ? null : manager.GetDistrictName(districtId);
        }

        private static string ParkName(DistrictManager manager, byte parkId)
        {
            return manager == null || parkId == 0 ? null : manager.GetParkName(parkId);
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

        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var chars = new List<char>();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c) && chars.Count > 0 && chars[chars.Count - 1] != '_')
                {
                    chars.Add('_');
                }

                chars.Add(char.ToLowerInvariant(c));
            }

            return new string(chars.ToArray());
        }
    }
}
