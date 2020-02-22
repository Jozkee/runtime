// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Text.Json.Serialization.Converters
{
    internal sealed class GuidKeyConverter : KeyConverter<Guid>
    {
        public override bool ReadKey(ref Utf8JsonReader reader, out Guid value)
        {
            return reader.TryGetGuidAfterValidation(out value);
        }

        protected override void WriteKeyAsT(Utf8JsonWriter writer, Guid key, JsonSerializerOptions options)
            => writer.WritePropertyName(key);
    }
}
