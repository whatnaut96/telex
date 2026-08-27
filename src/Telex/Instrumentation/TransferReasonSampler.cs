using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;

namespace Telex.Instrumentation
{
    internal static class TransferReasonSampler
    {
        public static IList<object> Sample()
        {
            var records = new List<object>();
            var names = Enum.GetNames(typeof(TransferManager.TransferReason));
            var manager = Singleton<TransferManager>.instance;
            var incoming = GetIntArray(manager, "m_incomingAmount");
            var outgoing = GetIntArray(manager, "m_outgoingAmount");
            var incomingCount = GetUShortArray(manager, "m_incomingCount");
            var outgoingCount = GetUShortArray(manager, "m_outgoingCount");

            for (var i = 0; i < names.Length; i++)
            {
                if (!LooksLikeMaterialFlow(names[i]))
                {
                    continue;
                }

                var record = new Dictionary<string, object>();
                record["reason"] = names[i];
                record["index"] = i;
                record["incoming_amount"] = Get(incoming, i);
                record["outgoing_amount"] = Get(outgoing, i);
                record["incoming_count"] = Get(incomingCount, i);
                record["outgoing_count"] = Get(outgoingCount, i);
                records.Add(record);
            }

            return records;
        }

        public static object SampleByResource()
        {
            var data = new Dictionary<string, object>();
            var rows = Sample();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i] as IDictionary<string, object>;
                if (row == null || !row.ContainsKey("reason") || row["reason"] == null)
                {
                    continue;
                }

                data[Normalize(row["reason"].ToString())] = row;
            }

            return data;
        }

        private static bool LooksLikeMaterialFlow(string name)
        {
            return Contains(name, "Goods")
                || Contains(name, "Food")
                || Contains(name, "Grain")
                || Contains(name, "Logs")
                || Contains(name, "Lumber")
                || Contains(name, "Oil")
                || Contains(name, "Petrol")
                || Contains(name, "Ore")
                || Contains(name, "Coal")
                || Contains(name, "Animal")
                || Contains(name, "Agricultural")
                || Contains(name, "Fish")
                || Contains(name, "Paper")
                || Contains(name, "PlanedTimber")
                || Contains(name, "Plastics")
                || Contains(name, "Petroleum")
                || Contains(name, "Metal")
                || Contains(name, "Glass");
        }

        private static bool Contains(string text, string value)
        {
            return text != null && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int[] GetIntArray(object instance, string fieldName)
        {
            if (instance == null)
            {
                return null;
            }

            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field == null ? null : field.GetValue(instance) as int[];
        }

        private static ushort[] GetUShortArray(object instance, string fieldName)
        {
            if (instance == null)
            {
                return null;
            }

            var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field == null ? null : field.GetValue(instance) as ushort[];
        }

        private static object Get(int[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? (object)values[index] : null;
        }

        private static object Get(ushort[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? (object)values[index] : null;
        }

        private static string Normalize(string name)
        {
            if (name == null)
            {
                return null;
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
