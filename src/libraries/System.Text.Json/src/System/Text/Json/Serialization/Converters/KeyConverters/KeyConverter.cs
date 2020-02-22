// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class KeyConverter
    {
        public abstract Type Type { get; }
        public abstract void WriteKeyAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options);
    }
}
