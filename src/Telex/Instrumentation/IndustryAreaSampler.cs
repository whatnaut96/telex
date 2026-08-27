using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;

namespace Telex.Instrumentation
{
    internal static class IndustryAreaSampler
    {
        public static object Sample()
        {
            var manager = Singleton<DistrictManager>.instance;
            var records = new List<object>();
            if (manager == null)
            {
                return records;
            }

            var parks = manager.m_parks.m_buffer;
            for (byte id = 1; id < parks.Length; id++)
            {
                var park = parks[id];
                if ((park.m_flags & DistrictPark.Flags.Created) == 0)
                {
                    continue;
                }

                var record = new Dictionary<string, object>();
                record["area_id"] = id;
                record["name"] = manager.GetParkName(id);
                record["type"] = park.m_parkType.ToString();
                record["level"] = park.m_parkLevel.ToString();
                record["synthetic_gis"] = SyntheticGis(manager, id, park);
                record["flags"] = park.m_flags.ToString();
                record["policies"] = park.m_parkPolicies.ToString();
                record["policies_effect"] = park.m_parkPoliciesEffect.ToString();
                record["building_count"] = park.m_buildings == null ? 0 : park.m_buildings.m_size;
                record["total_production_amount"] = park.m_totalProductionAmount;
                record["work_efficiency_delta"] = park.m_finalWorkEfficiencyDelta;
                record["storage_delta"] = park.m_finalStorageDelta;
                record["worker_count"] = park.m_finalWorkerCount;
                record["visitor_count"] = park.m_totalVisitorCount;
                record["ticket_income"] = park.m_finalTicketIncome;
                record["resources"] = ResourceRecords(park);
                records.Add(record);
            }

            return records;
        }

        private static object SyntheticGis(DistrictManager districtManager, byte parkId, DistrictPark park)
        {
            var data = new Dictionary<string, object>();
            data["geometry_type"] = "synthetic_industry_area";
            data["label_point"] = CitySampler.Vector(park.m_nameLocation);

            int minX;
            int minZ;
            int maxX;
            int maxZ;
            districtManager.GetParkArea(parkId, out minX, out minZ, out maxX, out maxZ);
            var gridBounds = new Dictionary<string, object>();
            gridBounds["min_x"] = minX;
            gridBounds["min_z"] = minZ;
            gridBounds["max_x"] = maxX;
            gridBounds["max_z"] = maxZ;
            data["park_grid_bounds"] = gridBounds;

            var buildingIds = new List<object>();
            var buildingPoints = new List<object>();
            var buildingExtent = BuildingExtent(park, buildingIds, buildingPoints);
            data["building_ids"] = buildingIds;
            data["building_points"] = buildingPoints;
            data["building_bounds"] = buildingExtent.ContainsKey("bounds") ? buildingExtent["bounds"] : null;
            data["building_centroid"] = buildingExtent.ContainsKey("centroid") ? buildingExtent["centroid"] : null;
            return data;
        }

        private static IDictionary<string, object> BuildingExtent(DistrictPark park, IList<object> buildingIds, IList<object> buildingPoints)
        {
            var result = new Dictionary<string, object>();
            if (park.m_buildings == null || park.m_buildings.m_size == 0)
            {
                return result;
            }

            var manager = Singleton<BuildingManager>.instance;
            if (manager == null)
            {
                return result;
            }

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var sum = Vector3.zero;
            var count = 0;

            for (var i = 0; i < park.m_buildings.m_size; i++)
            {
                var buildingId = park.m_buildings.m_buffer[i];
                if (buildingId == 0)
                {
                    continue;
                }

                var building = manager.m_buildings.m_buffer[buildingId];
                if ((building.m_flags & Building.Flags.Created) == 0)
                {
                    continue;
                }

                var point = new Dictionary<string, object>();
                point["building_id"] = buildingId;
                point["position"] = CitySampler.Vector(building.m_position);
                buildingIds.Add(buildingId);
                buildingPoints.Add(point);

                min.x = Mathf.Min(min.x, building.m_position.x);
                min.y = Mathf.Min(min.y, building.m_position.y);
                min.z = Mathf.Min(min.z, building.m_position.z);
                max.x = Mathf.Max(max.x, building.m_position.x);
                max.y = Mathf.Max(max.y, building.m_position.y);
                max.z = Mathf.Max(max.z, building.m_position.z);
                sum += building.m_position;
                count++;
            }

            if (count == 0)
            {
                return result;
            }

            var bounds = new Dictionary<string, object>();
            bounds["min"] = CitySampler.Vector(min);
            bounds["max"] = CitySampler.Vector(max);
            result["bounds"] = bounds;
            result["centroid"] = CitySampler.Vector(sum / count);
            return result;
        }

        private static object ResourceRecords(DistrictPark park)
        {
            var data = new Dictionary<string, object>();
            data["grain"] = ResourceData(park.m_grainData);
            data["logs"] = ResourceData(park.m_logsData);
            data["ore"] = ResourceData(park.m_oreData);
            data["oil"] = ResourceData(park.m_oilData);
            data["animal_products"] = ResourceData(park.m_animalProductsData);
            data["flours"] = ResourceData(park.m_floursData);
            data["paper"] = ResourceData(park.m_paperData);
            data["planed_timber"] = ResourceData(park.m_planedTimberData);
            data["petroleum"] = ResourceData(park.m_petroleumData);
            data["plastics"] = ResourceData(park.m_plasticsData);
            data["glass"] = ResourceData(park.m_glassData);
            data["metals"] = ResourceData(park.m_metalsData);
            data["luxury_products"] = ResourceData(park.m_luxuryProductsData);
            return data;
        }

        private static object ResourceData(DistrictAreaResourceData resource)
        {
            var data = new Dictionary<string, object>();
            data["consumption"] = resource.m_finalConsumption;
            data["production"] = resource.m_finalProduction;
            data["buffer_amount"] = resource.m_finalBufferAmount;
            data["buffer_capacity"] = resource.m_finalBufferCapacity;
            data["incoming_transfer"] = resource.m_finalIncomingTransfer;
            data["import"] = resource.m_finalImport;
            data["export"] = resource.m_finalExport;
            data["temp_consumption"] = resource.m_tempConsumption;
            data["temp_production"] = resource.m_tempProduction;
            data["temp_import"] = resource.m_tempImport;
            data["temp_export"] = resource.m_tempExport;
            return data;
        }
    }
}
