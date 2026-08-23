using System.Collections.Generic;
using ColossalFramework;

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
