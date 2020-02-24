// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class Int32KeyConverter : KeyConverter<int>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out int value)
        {
            //ThrowHelper.GetFormatException(NumericType.Int32)
            return reader.TryGetInt32AfterValidation(out value);
        }

        public override int ReadKeyFromBytes(ReadOnlySpan<byte> bytes)
        {
            bool success = Utf8Parser.TryParse(bytes, out int keyValue, out int _);
            Debug.Assert(success);

            return keyValue;
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, int key, JsonSerializerOptions options)
            => writer.WritePropertyName(key);
    }
}
