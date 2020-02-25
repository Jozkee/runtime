// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class GuidKeyConverter : KeyConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            bool success = reader.TryGetGuidAfterValidation(out Guid value);
            Debug.Assert(success);

            return value;
        }

        public override Guid ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            bool success = Utf8Parser.TryParse(bytes, out Guid keyValue, out int _);
            Debug.Assert(success);

            return keyValue;
        }

        public override void Write(Utf8JsonWriter writer, Guid key, JsonSerializerOptions options)
            => writer.WritePropertyName(key);
    }
}
