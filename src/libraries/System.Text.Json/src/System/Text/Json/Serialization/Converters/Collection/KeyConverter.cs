using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Unicode;

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class KeyConverter
    {
        public abstract Type Type { get; }

        internal static readonly Dictionary<Type, KeyConverter> s_KeyConverters = GetSupportedKeyConverters();

        internal static KeyConverter? ResolveKeyConverter(Type keyType)
        {
            if (s_KeyConverters.TryGetValue(keyType!, out KeyConverter? converter))
            {
                return converter;
            }
            else
            {
                // Throw?
                return null;
            }
        }

        private const int NumberOfKeyConverters = 2;

        private static Dictionary<Type, KeyConverter> GetSupportedKeyConverters()
        {
            var converters = new Dictionary<Type, KeyConverter>(NumberOfKeyConverters);

            // Use a dictionary for simple converters.
            foreach (KeyConverter converter in KeyConverters)
            {
                converters.Add(converter.Type, converter);
            }

            Debug.Assert(NumberOfKeyConverters == converters.Count);

            return converters;
        }

        private static IEnumerable<KeyConverter> KeyConverters
        {
            get
            {
                // When adding to this, update NumberOfKeyConverters above.
                yield return new Int32KeyConverter();
                yield return new GuidKeyConverter();
            }
        }
    }

    internal abstract class KeyConverter<T> : KeyConverter
    {
        public abstract bool ReadKey(ref Utf8JsonReader reader, out T value);
        public void WriteKey(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options, bool ignoreKeyPolicy)
        {
            WriteKeyAsTOrAsString(writer, value, options, ignoreKeyPolicy);
        }
        protected abstract void WriteKeyAsT(Utf8JsonWriter writer, T value);

        public override Type Type => typeof(T);

        protected void WriteKeyAsTOrAsString(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options, bool ignoreKeyPolicy)
        {
            if (options.DictionaryKeyPolicy != null && !ignoreKeyPolicy)
            {
                // TODO: Why is value(!) neccessary?
                string keyNameAsString = options.DictionaryKeyPolicy.ConvertName(value!.ToString()!);

                if (keyNameAsString == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerDictionaryKeyNull(options.DictionaryKeyPolicy.GetType());
                }

                writer.WritePropertyName(keyNameAsString);
            }
            else
            {
                WriteKeyAsT(writer, value);
            }
        }
    }

    internal sealed class Int32KeyConverter : KeyConverter<int>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out int value)
        {
            return reader.TryGetInt32AfterValidation(out value);
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, int key) => writer.WritePropertyName(key);
    }

    internal sealed class GuidKeyConverter : KeyConverter<Guid>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out Guid value)
        {
            return reader.TryGetGuidAfterValidation(out value);
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, Guid key) => writer.WritePropertyName(key);
    }

    internal sealed class StringKeyConverter : KeyConverter<string>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out string value)
        {
            value = reader.GetString()!;

            return true;
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, string key) => writer.WritePropertyName(key);
    }
}
