using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;

namespace Telex.Instrumentation
{
    internal sealed class DlcReflectionSampler
    {
        private static readonly string[] CandidateManagerTypes =
        {
            "DistrictParkManager",
            "IndustryManager",
            "CampusManager",
            "ParkManager"
        };

        public IList<ManagerSnapshot> Sample()
        {
            var snapshots = new List<ManagerSnapshot>();

            for (var i = 0; i < CandidateManagerTypes.Length; i++)
            {
                var type = FindGameType(CandidateManagerTypes[i]);
                if (type == null)
                {
                    continue;
                }

                var instance = GetSingletonInstance(type);
                if (instance == null)
                {
                    continue;
                }

                snapshots.Add(Snapshot(type, instance));
            }

            return snapshots;
        }

        private static Type FindGameType(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(typeName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static object GetSingletonInstance(Type managerType)
        {
            var singletonType = typeof(Singleton<>).MakeGenericType(managerType);
            var property = singletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            return property == null ? null : property.GetValue(null, null);
        }

        private static ManagerSnapshot Snapshot(Type type, object instance)
        {
            var snapshot = new ManagerSnapshot();
            snapshot.Name = type.Name;
            snapshot.Values = new Dictionary<string, string>();

            CaptureFields(type, instance, snapshot.Values);
            CaptureProperties(type, instance, snapshot.Values);

            return snapshot;
        }

        private static void CaptureFields(Type type, object instance, IDictionary<string, string> values)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (IsSimple(field.FieldType))
                {
                    values[field.Name] = Convert.ToString(field.GetValue(instance));
                }
            }
        }

        private static void CaptureProperties(Type type, object instance, IDictionary<string, string> values)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!property.CanRead || property.GetIndexParameters().Length != 0 || !IsSimple(property.PropertyType))
                {
                    continue;
                }

                values[property.Name] = Convert.ToString(property.GetValue(instance, null));
            }
        }

        private static bool IsSimple(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
        }
    }
}
