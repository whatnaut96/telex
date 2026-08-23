using System.Collections.Generic;
using ColossalFramework;

namespace Telex.Instrumentation
{
    internal static class CitizenSampler
    {
        public static IList<object> Sample()
        {
            var manager = Singleton<CitizenManager>.instance;
            var buildingManager = Singleton<BuildingManager>.instance;
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
                record["entity"] = id;
                record["age"] = citizen.m_age;
                record["age_group"] = AgeGroup(citizen.m_age);
                record["education"] = Education(citizen.m_flags);
                record["state"] = citizen.m_flags.ToString();
                record["wellbeing"] = citizen.m_wellbeing;
                record["health"] = citizen.m_health;
                record["happiness"] = citizen.m_wellbeing;
                record["home_building_id"] = NullZero(citizen.m_homeBuilding);
                record["home_district_id"] = BuildingDistrict(citizen.m_homeBuilding);
                record["workplace_building_id"] = NullZero(citizen.m_workBuilding);
                record["workplace_name"] = BuildingName(buildingManager, citizen.m_workBuilding);
                record["workplace_zone_type"] = BuildingZoneType(citizen.m_workBuilding);
                record["is_tourist"] = (citizen.m_flags & Citizen.Flags.Tourist) != 0;
                record["is_student"] = (citizen.m_flags & Citizen.Flags.Student) != 0;
                record["is_unemployed"] = (citizen.m_flags & Citizen.Flags.Unemployed) != 0;
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

        private static string BuildingName(BuildingManager manager, ushort buildingId)
        {
            if (manager == null || buildingId == 0)
            {
                return null;
            }

            return manager.GetBuildingName(buildingId, InstanceID.Empty);
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
