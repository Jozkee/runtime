// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class EnumKeyConverter<TEnum> : KeyConverter<TEnum> where TEnum : struct, Enum
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out TEnum value)
        {
            string enumValue = reader.GetString()!;
            value = Enum.Parse<TEnum>(enumValue);

            return true;//?
        }

        public override TEnum ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            throw new NotImplementedException();
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            string keyName = value.ToString();
            writer.WritePropertyName(keyName);
        }
    }
}
