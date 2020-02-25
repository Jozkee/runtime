// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.Unicode;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class EnumKeyConverter<TEnum> : KeyConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? enumValue = reader.GetString();
            if (!Enum.TryParse(enumValue, out TEnum value)
                    && !Enum.TryParse(enumValue, ignoreCase: true, out value))
            {
                ThrowHelper.ThrowJsonException();
            }

            return value;
        }

        public override TEnum ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            Span<char> utf16Name = stackalloc char[bytes.Length];
            Utf8.ToUtf16(bytes, utf16Name, out int bytesRead, out int CharsWritten);

            Enum.TryParse(utf16Name.ToString(), out TEnum result);

            return result;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            string keyName = value.ToString();
            // Unlike EnumConverter we don't do any validation here since PropertyName can only be string.
            writer.WritePropertyName(keyName);
        }
    }
}
