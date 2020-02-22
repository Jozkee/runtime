// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class DictionaryKeyConverterTests
    {
        [Theory]
        [MemberData(nameof(GetData))]
        public static void TestDictionaryTKeyTValue(IDictionary dictionary)
        {
            string json = JsonSerializer.Serialize(dictionary);
            // Check is the expected result.

            object copy = JsonSerializer.Deserialize(json, dictionary.GetType());
        }

        private enum MyEnum
        {
            Foo, Bar
        }

        [Flags]
        private enum MyEnumFlags
        {
            Foo, Bar, Baz
        }

        public static IEnumerable<object[]> GetData()
        {
            yield return new object[] { new Dictionary<int, int>() { { int.MinValue, int.MaxValue } } };
            yield return new object[] { new Dictionary<Guid, int>() { { Guid.NewGuid(), int.MaxValue } } };
            yield return new object[] { new Dictionary<MyEnum, int>() { { MyEnum.Foo, int.MaxValue } } };
            yield return new object[] { new Dictionary<MyEnumFlags, int>() { { MyEnumFlags.Foo | MyEnumFlags.Bar | MyEnumFlags.Baz, int.MaxValue } } };
            yield return new object[] { new Dictionary<string, int>() { { "propertyName", int.MaxValue } } };
        }
    }
}
