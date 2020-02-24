// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class StringKeyConverter : KeyConverter<string>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out string value)
        {
            value = reader.GetString()!;

            return true;
        }

        public override string ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            throw new NotImplementedException();
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, string key, JsonSerializerOptions options)
            => writer.WritePropertyName(key);
    }
}
