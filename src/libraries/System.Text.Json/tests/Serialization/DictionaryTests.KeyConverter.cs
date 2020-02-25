// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Tests;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class DictionaryKeyConverterTests<TKey, TValue>
    {
        protected abstract TKey Key { get; }
        protected abstract TValue Value { get; }
        protected virtual string _expectedJson => $"{{\"{Key}\":{Value}}}";

        protected virtual void Validate(Dictionary<TKey, TValue> dictionary)
        {
            bool success = dictionary.TryGetValue(Key, out TValue value);
            Assert.True(success);

            Assert.Equal(Value, value);
        }

        protected virtual Dictionary<TKey, TValue> BuildDictionary()
        {
            var dictionary = new Dictionary<TKey, TValue>();
            dictionary.Add(Key, Value);

            return dictionary;
        }

        [Fact]
        public void TestNonStringKeyDictinary()
        {
            Dictionary<TKey, TValue> dictionary = BuildDictionary();

            string json = JsonSerializer.Serialize(dictionary);
            Assert.Equal(_expectedJson, json);

            Dictionary<TKey, TValue> dictionaryCopy = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json);
            Validate(dictionaryCopy);
        }

        // Test with read ahead
        [Fact]
        public async Task TestNonStringKeyDictionaryReadAhead()
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
            dictionary.Add(Key, Value);

            string json = JsonSerializer.Serialize(dictionary);
            Assert.Equal(json, _expectedJson);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            Stream stream = new MemoryStream(jsonBytes);

            Dictionary<TKey, TValue> dictionaryCopy = await JsonSerializer.DeserializeAsync<Dictionary<TKey, TValue>>(stream);
            Validate(dictionaryCopy);
        }

        // Test with DictionaryKeyPolicy

        // Test extension data?? I haven't tested that.
    }

    // Create another abstract to test unsupported types.

    public class DictionaryIntKey : DictionaryKeyConverterTests<int, int>
    {
        protected override int Key => 1;

        protected override int Value => 1;
    }

    public class DictionaryGuidKey : DictionaryKeyConverterTests<Guid, int>
    {
        // Use singleton pattern here so the Guid key does not change everytime this is called.
        protected override Guid Key { get; } = Guid.NewGuid();

        protected override int Value => 1;
    }

    public enum MyEnum
    {
        Foo,
        Bar
    }

    public class DictionaryEnumKey : DictionaryKeyConverterTests<MyEnum, int>
    {
        protected override MyEnum Key => MyEnum.Foo;

        protected override int Value => 1;
    }

    [Flags]
    public enum MyEnumFlags
    {
        Foo,
        Bar,
        Baz
    }

    public class DictionaryEnumFlagsKey : DictionaryKeyConverterTests<MyEnumFlags, int>
    {
        protected override MyEnumFlags Key => MyEnumFlags.Foo | MyEnumFlags.Bar;

        protected override int Value => 1;
    }

    public class DictionaryStringKey : DictionaryKeyConverterTests<string, int>
    {
        protected override string Key => "key1";

        protected override int Value => 1;
    }

    public class DictionaryIntObjectKey : DictionaryKeyConverterTests<object, int>
    {
        protected override object Key => 1;
        protected override int Value => 1;
        protected override string _expectedJson => base._expectedJson;
        private Dictionary<object, int> _dictionary;

        protected override Dictionary<object, int> BuildDictionary()
        {
            var dictionary = new Dictionary<object, int>();
            // Add a bunch of different types.
            dictionary.Add(1, 1);
            dictionary.Add(Guid.NewGuid(), 2);
            dictionary.Add("Key1", 3);
            dictionary.Add(MyEnum.Foo, 4);
            dictionary.Add(MyEnumFlags.Foo | MyEnumFlags.Bar, 5);

            _dictionary = dictionary;

            return dictionary;
        }

        private string GetExpectedJson()
        {
            return string.Empty;
        }

        protected override void Validate(Dictionary<object, int> dictionary)
        {
            Assert.True(dictionary.Count == 1);

            Dictionary<object, int>.Enumerator enumerator = dictionary.GetEnumerator();
            enumerator.MoveNext();

            Assert.Equal(typeof(JsonElement), enumerator.Current.Key.GetType());
            JsonElement key = (JsonElement)enumerator.Current.Key;

            Assert.Equal(JsonValueKind.String, key.ValueKind);
            Assert.Equal(Key.ToString(), key.GetString());

            int value = enumerator.Current.Value;
            Assert.Equal(Value, value);
        }
    }

    // TKey is string at runtime.
    public class DictionaryStringObjectKey : DictionaryKeyConverterTests<object, int>
    {
        protected override object Key => "Key1";

        protected override int Value => 1;

        protected override void Validate(Dictionary<object, int> dictionary)
        {
            Assert.True(dictionary.Count == 1);

            Dictionary<object, int>.Enumerator enumerator = dictionary.GetEnumerator();
            enumerator.MoveNext();

            Assert.Equal(typeof(JsonElement), enumerator.Current.Key.GetType());
            JsonElement key = (JsonElement)enumerator.Current.Key;

            Assert.Equal(JsonValueKind.String, key.ValueKind);
            Assert.Equal(Key, key.GetString());

            int value = enumerator.Current.Value;
            Assert.Equal(Value, value);
        }
    }

    // TKey is enum at runtime.
    public class DictionaryEnumObjectKey : DictionaryKeyConverterTests<object, int>
    {
        protected override object Key => MyEnum.Foo;

        protected override int Value => 1;

        protected override void Validate(Dictionary<object, int> dictionary)
        {
            Assert.True(dictionary.Count == 1);

            Dictionary<object, int>.Enumerator enumerator = dictionary.GetEnumerator();
            enumerator.MoveNext();

            Assert.Equal(typeof(JsonElement), enumerator.Current.Key.GetType());
            JsonElement key = (JsonElement)enumerator.Current.Key;

            Assert.Equal(JsonValueKind.String, key.ValueKind);
            Assert.Equal(Key.ToString(), key.GetString());

            int value = enumerator.Current.Value;
            Assert.Equal(Value, value);
        }
    }
}
