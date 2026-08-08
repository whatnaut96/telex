using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Telex.Serialization
{
    internal static class TelemetryJson
    {
        public static string Serialize(object value)
        {
            var builder = new StringBuilder(4096);
            AppendValue(builder, value);
            return builder.ToString();
        }

        private static void AppendValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            var type = value.GetType();

            if (value is string)
            {
                AppendString(builder, (string)value);
            }
            else if (value is bool)
            {
                builder.Append((bool)value ? "true" : "false");
            }
            else if (value is DateTime)
            {
                AppendString(builder, ((DateTime)value).ToString("o", CultureInfo.InvariantCulture));
            }
            else if (type.IsEnum)
            {
                AppendString(builder, value.ToString());
            }
            else if (IsNumber(type))
            {
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
            else if (value is IDictionary)
            {
                AppendDictionary(builder, (IDictionary)value);
            }
            else if (value is IEnumerable)
            {
                AppendEnumerable(builder, (IEnumerable)value);
            }
            else
            {
                AppendObject(builder, value);
            }
        }

        private static void AppendDictionary(StringBuilder builder, IDictionary values)
        {
            builder.Append('{');
            var index = 0;
            foreach (DictionaryEntry entry in values)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                AppendString(builder, Convert.ToString(entry.Key, CultureInfo.InvariantCulture));
                builder.Append(':');
                AppendValue(builder, entry.Value);
                index++;
            }
            builder.Append('}');
        }

        private static void AppendEnumerable(StringBuilder builder, IEnumerable values)
        {
            builder.Append('[');
            var index = 0;
            foreach (var item in values)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                AppendValue(builder, item);
                index++;
            }
            builder.Append(']');
        }

        private static void AppendObject(StringBuilder builder, object value)
        {
            builder.Append('{');
            var index = 0;
            var type = value.GetType();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (var i = 0; i < fields.Length; i++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                AppendString(builder, ToSnakeCase(fields[i].Name));
                builder.Append(':');
                AppendValue(builder, fields[i].GetValue(value));
                index++;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (var i = 0; i < properties.Length; i++)
            {
                if (!properties[i].CanRead || properties[i].GetIndexParameters().Length != 0)
                {
                    continue;
                }

                if (index > 0)
                {
                    builder.Append(',');
                }

                AppendString(builder, ToSnakeCase(properties[i].Name));
                builder.Append(':');
                AppendValue(builder, properties[i].GetValue(value, null));
                index++;
            }

            builder.Append('}');
        }

        private static bool IsNumber(Type type)
        {
            return type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }

        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var builder = new StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private static void AppendString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < 32)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }

            builder.Append('"');
        }
    }
}
