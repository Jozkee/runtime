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
        public abstract void WriteKeyAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options);
    }

    internal abstract class KeyConverter<T> : KeyConverter
    {
        public abstract bool ReadKey(ref Utf8JsonReader reader, out T value);
        public void WriteKey(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options, bool ignoreKeyPolicy)
        {
            WriteKeyAsTOrAsString(writer, value, options, ignoreKeyPolicy);
        }
        protected abstract void WriteKeyAsT(Utf8JsonWriter writer, T value, JsonSerializerOptions options);
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
                WriteKeyAsT(writer, value, options);
            }
        }
        public override void WriteKeyAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options) => WriteKeyAsT(writer, (T)value, options);
    }

    internal sealed class Int32KeyConverter : KeyConverter<int>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out int value)
        {
            //ThrowHelper.GetFormatException(NumericType.Int32)
            return reader.TryGetInt32AfterValidation(out value);
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, int key, JsonSerializerOptions options) => writer.WritePropertyName(key);
    }

    internal sealed class GuidKeyConverter : KeyConverter<Guid>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out Guid value)
        {
            return reader.TryGetGuidAfterValidation(out value);
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, Guid key, JsonSerializerOptions options) => writer.WritePropertyName(key);
    }

    internal sealed class StringKeyConverter : KeyConverter<string>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out string value)
        {
            value = reader.GetString()!;

            return true;
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, string key, JsonSerializerOptions options) => writer.WritePropertyName(key);
    }

    internal sealed class EnumKeyConverter<TEnum>: KeyConverter<TEnum> where TEnum : struct, Enum
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out TEnum value)
        {
            throw new NotImplementedException();
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            //string keyName = value.ToString();
            //writer.WritePropertyName(keyName);
        }
    }

    internal sealed class ObjectKeyConverter : KeyConverter<object>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out object value)
        {
            throw new NotImplementedException();
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            Type runtimeType = value.GetType();
            KeyConverter? runtimeTypeConverter = options.GetOrAddKeyConverter(runtimeType);

            // We don't support object itself as TKey, only the other supported types when they are boxed.
            if (runtimeTypeConverter != null
                && runtimeTypeConverter != this)
            {
                // Redirect to the runtime-type key converter.
                runtimeTypeConverter.WriteKeyAsObject(writer, value, options);
            }

            throw new JsonException("key type is not supported");
        }
    }
}
