using System.Collections.Generic;
using ColossalFramework;

namespace Telex.Instrumentation
{
    internal static class RoadSampler
    {
        public static object Sample()
        {
            var manager = Singleton<NetManager>.instance;
            var records = new List<object>();
            if (manager == null)
            {
                return records;
            }

            var segments = manager.m_segments.m_buffer;
            for (ushort id = 1; id < segments.Length; id++)
            {
                var segment = segments[id];
                if ((segment.m_flags & NetSegment.Flags.Created) == 0)
                {
                    continue;
                }

                var info = segment.Info;
                var start = manager.m_nodes.m_buffer[segment.m_startNode];
                var end = manager.m_nodes.m_buffer[segment.m_endNode];
                var record = new Dictionary<string, object>();
                record["entity_id"] = id;
                record["segment_id"] = id;
                record["name"] = manager.GetSegmentName(id);
                record["prefab"] = info == null ? null : info.name;
                record["start_entity"] = segment.m_startNode;
                record["end_entity"] = segment.m_endNode;
                record["start_node"] = segment.m_startNode;
                record["end_node"] = segment.m_endNode;
                record["start_pos"] = CitySampler.Vector(start.m_position);
                record["end_pos"] = CitySampler.Vector(end.m_position);
                record["middle_position"] = CitySampler.Vector(segment.m_middlePosition);
                record["start_direction"] = CitySampler.Vector(segment.m_startDirection);
                record["end_direction"] = CitySampler.Vector(segment.m_endDirection);
                record["curve_a"] = CitySampler.Vector(start.m_position);
                record["curve_b"] = CitySampler.Vector(start.m_position + segment.m_startDirection * (segment.m_averageLength / 3f));
                record["curve_c"] = CitySampler.Vector(end.m_position + segment.m_endDirection * (segment.m_averageLength / 3f));
                record["curve_d"] = CitySampler.Vector(end.m_position);
                record["elevation"] = Elevation(start, end);
                record["average_length"] = segment.m_averageLength;
                record["flags"] = segment.m_flags.ToString();
                record["flags2"] = segment.m_flags2.ToString();
                record["problems"] = segment.m_problems.ToString();
                record["service_coverage"] = new List<object>();
                record["road_traffic"] = RoadTraffic(segment);
                record["traffic_buffer"] = segment.m_trafficBuffer;
                record["traffic_density"] = segment.m_trafficDensity;
                record["noise_buffer"] = segment.m_noiseBuffer;
                record["noise_density"] = segment.m_noiseDensity;
                record["condition"] = segment.m_condition;
                record["lanes"] = segment.m_lanes;
                record["building_access"] = BuildingsOnSegment(id);
                records.Add(record);
            }

            return records;
        }

        private static object RoadTraffic(NetSegment segment)
        {
            var data = new Dictionary<string, object>();
            data["traffic_buffer"] = segment.m_trafficBuffer;
            data["traffic_density"] = segment.m_trafficDensity;
            data["traffic_light_state0"] = segment.m_trafficLightState0;
            data["traffic_light_state1"] = segment.m_trafficLightState1;
            data["condition"] = segment.m_condition;
            return data;
        }

        private static object Elevation(NetNode start, NetNode end)
        {
            var data = new Dictionary<string, object>();
            data["min"] = start.m_elevation;
            data["max"] = end.m_elevation;
            return data;
        }

        private static IList<object> BuildingsOnSegment(ushort segmentId)
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

                if (building.m_accessSegment == segmentId)
                {
                    records.Add(id);
                }
            }

            return records;
        }
    }
}
