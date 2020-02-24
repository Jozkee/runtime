// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ObjectKeyConverter : KeyConverter<object>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out object value)
        {
            throw new NotImplementedException();
        }

        public override object ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
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
            else
            {
                throw new JsonException("key type is not supported");
            }
        }
    }
}
