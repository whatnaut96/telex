using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;

namespace Telex.Instrumentation
{
    internal static class ReflectionSummary
    {
        public static object SampleSingleton(string typeName)
        {
            var type = FindGameType(typeName);
            if (type == null)
            {
                return null;
            }

            var singletonType = typeof(Singleton<>).MakeGenericType(type);
            var property = singletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = property == null ? null : property.GetValue(null, null);
            if (instance == null)
            {
                return null;
            }

            var data = new Dictionary<string, object>();
            data["manager"] = type.Name;

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (var i = 0; i < fields.Length; i++)
            {
                if (!IsSimple(fields[i].FieldType))
                {
                    continue;
                }

                data[fields[i].Name] = ConvertValue(fields[i].GetValue(instance));
            }

            return data;
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

        private static bool IsSimple(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
        }

        private static object ConvertValue(object value)
        {
            return value != null && value.GetType().IsEnum ? value.ToString() : value;
        }
    }
}
