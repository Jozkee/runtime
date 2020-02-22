// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
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
        public override void WriteKeyAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            => WriteKeyAsT(writer, (T)value, options);
    }
}
