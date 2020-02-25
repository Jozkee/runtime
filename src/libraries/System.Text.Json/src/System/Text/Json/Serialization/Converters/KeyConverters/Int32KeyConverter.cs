// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int32KeyConverter : KeyConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            bool success = reader.TryGetInt32AfterValidation(out int keyValue);
            Debug.Assert(success);

            return keyValue;
        }

        public override int ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            bool success = Utf8Parser.TryParse(bytes, out int keyValue, out int _);
            Debug.Assert(success);

            return keyValue;
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WritePropertyName(value);
    }
}
