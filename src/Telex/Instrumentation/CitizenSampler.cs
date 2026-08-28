using System.Collections.Generic;
using ColossalFramework;

namespace Telex.Instrumentation
{
    internal static class CitizenSampler
    {
        public static IList<object> Sample()
        {
            var manager = Singleton<CitizenManager>.instance;
            var records = new List<object>();
            if (manager == null)
            {
                return records;
            }

            var citizens = manager.m_citizens.m_buffer;
            for (uint id = 1; id < citizens.Length; id++)
            {
                var citizen = citizens[id];
                if ((citizen.m_flags & Citizen.Flags.Created) == 0)
                {
                    continue;
                }

                var record = new Dictionary<string, object>();
                record["entity_id"] = id;
                record["age"] = citizen.m_age;
                record["age_group"] = AgeGroup(citizen.m_age);
                record["education_level"] = Education(citizen.m_flags);
                record["home_building_id"] = NullZero(citizen.m_homeBuilding);
                record["home_district_id"] = BuildingDistrict(citizen.m_homeBuilding);
                record["workplace_building_id"] = NullZero(citizen.m_workBuilding);
                record["workplace_type"] = BuildingZoneType(citizen.m_workBuilding);
                records.Add(record);
            }

            return records;
        }

        private static object BuildingDistrict(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return null;
            }

            var manager = Singleton<BuildingManager>.instance;
            var districtManager = Singleton<DistrictManager>.instance;
            if (manager == null || districtManager == null)
            {
                return null;
            }

            var building = manager.m_buildings.m_buffer[buildingId];
            return districtManager.GetDistrict(building.m_position);
        }

        private static string BuildingZoneType(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return null;
            }

            var manager = Singleton<BuildingManager>.instance;
            if (manager == null)
            {
                return null;
            }

            var info = manager.m_buildings.m_buffer[buildingId].Info;
            return CitySampler.ZoneType(info);
        }

        private static object NullZero(ushort value)
        {
            return value == 0 ? null : (object)value;
        }

        private static string Education(Citizen.Flags flags)
        {
            if ((flags & Citizen.Flags.Education3) != 0)
            {
                return "three_schools";
            }
            if ((flags & Citizen.Flags.Education2) != 0)
            {
                return "two_schools";
            }
            if ((flags & Citizen.Flags.Education1) != 0)
            {
                return "one_school";
            }

            return "uneducated";
        }

        private static string AgeGroup(ushort age)
        {
            if (age < Citizen.AGE_LIMIT_CHILD)
            {
                return "child";
            }
            if (age < Citizen.AGE_LIMIT_TEEN)
            {
                return "teen";
            }
            if (age < Citizen.AGE_LIMIT_YOUNG)
            {
                return "young";
            }
            if (age < Citizen.AGE_LIMIT_ADULT)
            {
                return "adult";
            }

            return "senior";
        }
    }
}
