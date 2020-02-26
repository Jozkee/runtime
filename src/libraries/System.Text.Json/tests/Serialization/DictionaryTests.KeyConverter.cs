// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class DictionaryKeyConverterTests<TKey, TValue>
    {
        protected abstract TKey Key { get; }
        protected abstract TValue Value { get; }
        protected virtual string _expectedJson => $"{{\"{Key}\":{Value}}}";
        protected virtual string _expectedJsonHashedProperties => $"{{\"{HashingNamingPolicy.HashName(Key.ToString())}\":{Value}}}";

        private static JsonSerializerOptions _policyOptions = new JsonSerializerOptions { DictionaryKeyPolicy = new HashingNamingPolicy() };

        protected virtual void Validate(Dictionary<TKey, TValue> dictionary)
        {
            bool success = dictionary.TryGetValue(Key, out TValue value);
            Assert.True(success);
            Assert.Equal(Value, value);
        }

        protected virtual void ValidateHashed(Dictionary<int, TValue> dictionary)
        {
            bool success = dictionary.TryGetValue(Key.ToString().GetHashCode(), out TValue value);
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

        [Fact]
        public async Task TestNonStringKeyDictionaryAsync()
        {
            Dictionary<TKey, TValue> dictionary = BuildDictionary();

            MemoryStream serializeStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(serializeStream, dictionary);
            string json = Encoding.UTF8.GetString(serializeStream.ToArray());
            Assert.Equal(_expectedJson, json);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            Stream deserializeStream = new MemoryStream(jsonBytes);
            Dictionary<TKey, TValue> dictionaryCopy = await JsonSerializer.DeserializeAsync<Dictionary<TKey, TValue>>(deserializeStream);
            Validate(dictionaryCopy);
        }

        [Fact]
        public void TestNonStringKeyDictinaryUsingPolicy()
        {
            Dictionary<TKey, TValue> dictionary = BuildDictionary();

            string json = JsonSerializer.Serialize(dictionary, _policyOptions);
            Assert.Equal(_expectedJsonHashedProperties, json);

            Dictionary<int, TValue> dictionaryCopy = JsonSerializer.Deserialize<Dictionary<int, TValue>>(json);
            ValidateHashed(dictionaryCopy);
        }

        [Fact]
        public async Task TestNonStringKeyDictionaryAsyncUsingPolicy()
        {
            Dictionary<TKey, TValue> dictionary = BuildDictionary();

            MemoryStream serializeStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(serializeStream, dictionary, _policyOptions);
            string json = Encoding.UTF8.GetString(serializeStream.ToArray());
            Assert.Equal(_expectedJsonHashedProperties, json);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            Stream deserializeStream = new MemoryStream(jsonBytes);
            Dictionary<int, TValue> dictionaryCopy = await JsonSerializer.DeserializeAsync<Dictionary<int, TValue>>(deserializeStream);
            ValidateHashed(dictionaryCopy);
        }
    }

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
        protected override string Key => "KeyString";
        protected override int Value => 1;
    }

    public class DictionaryObjectKey : DictionaryKeyConverterTests<object, int>
    {
        protected override object Key => 1;
        protected override int Value => 1;
        protected override string _expectedJson => BuildExpectedJson();
        protected override string _expectedJsonHashedProperties => BuildExpectedJsonHashedProperties();
        private Dictionary<object, int> _dictionary;
        private Dictionary<string, int> _dictionaryOfStringKeys;
        private Dictionary<int, int> _dictionaryOfHashedKeys;

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
            _dictionaryOfStringKeys = dictionary.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
            _dictionaryOfHashedKeys = dictionary.ToDictionary(kvp => kvp.Key.ToString().GetHashCode(), kvp => kvp.Value);

            return dictionary;
        }

        private string BuildExpectedJson()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    foreach (KeyValuePair<object, int> kvp in _dictionary)
                    {
                        writer.WriteNumber(kvp.Key.ToString(), kvp.Value);
                    }
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private string BuildExpectedJsonHashedProperties()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    foreach (KeyValuePair<object, int> kvp in _dictionary)
                    {
                        string hashedKey = HashingNamingPolicy.HashName(kvp.Key.ToString());
                        writer.WriteNumber(hashedKey, kvp.Value);
                    }
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        protected override void Validate(Dictionary<object, int> dictionary)
        {
            foreach (KeyValuePair<object, int> kvp in dictionary)
            {
                if (kvp.Key is JsonElement keyJsonElement)
                {
                    Assert.Equal(JsonValueKind.String, keyJsonElement.ValueKind);

                    string keyString = keyJsonElement.GetString();
                    Assert.Equal(_dictionaryOfStringKeys[keyString], kvp.Value);
                }
                else
                {
                    Assert.True(false, "Polymorphic key is not JsonElement");
                }
            }
        }

        protected override void ValidateHashed(Dictionary<int, int> dictionary)
        {
            foreach (KeyValuePair<int, int> kvp in dictionary)
            {
                bool success = _dictionaryOfHashedKeys.TryGetValue(kvp.Key, out int value);
                Assert.True(success);
                Assert.Equal(value, kvp.Value);
            }
        }
    }

    public class HashingNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return HashName(name);
        }

        public static string HashName(string name)
        {
            return name.GetHashCode().ToString();
        }
    }

    public abstract class DictionaryUnsupportedKeyTests<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _dictionary => BuildDictionary();
        private static JsonSerializerOptions _policyOptions = new JsonSerializerOptions { DictionaryKeyPolicy = new HashingNamingPolicy() };

        private Dictionary<TKey, TValue> BuildDictionary()
        {
            return new Dictionary<TKey, TValue>();
        }

        [Fact]
        public void ThrowUnsupportedSerialize() => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(_dictionary));
        [Fact]
        public void ThrowUnsupportedSerializeUsingPolicy() => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(_dictionary, _policyOptions));
        [Fact]
        public async Task ThrowUnsupportedSerializeAsync() => await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializer.SerializeAsync(new MemoryStream(), _dictionary));
        [Fact]
        public async Task ThrowUnsupportedSerializeAsyncUsingPolicyAsync() => await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializer.SerializeAsync(new MemoryStream(), _dictionary, _policyOptions));
        [Fact]
        public void ThrowUnsupportedDeserialize() => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Dictionary<TKey, TValue>>("{}"));
        [Fact]
        public async Task ThrowUnsupportedDeserializeAsync() => await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializer.DeserializeAsync<Dictionary<TKey, TValue>>(new MemoryStream(Encoding.UTF8.GetBytes("{}"))));
    }

    public class DictionaryUriKeyUnsupported : DictionaryUnsupportedKeyTests<Uri, int>{ }
    public class MyClass { }
    public class DictionaryMyClassKeyUnsupported : DictionaryUnsupportedKeyTests<MyClass, int>{ }
    public struct MyStruct { }
    public class DictionaryMyStructKeyUnsupported : DictionaryUnsupportedKeyTests<MyStruct, int> { }

    public class DictionaryNonStringKeyTests
    {
        private static JsonSerializerOptions _policyOptions = new JsonSerializerOptions { DictionaryKeyPolicy = new HashingNamingPolicy() };

        [Fact]
        public void ThrowOnUnsupportedRuntimeType()
        {
            Dictionary<object, int> dictionary = new Dictionary<object, int>();
            dictionary.Add(new Uri("http://github.com/Jozkee"), 1);

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary));

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary, _policyOptions));
        }

        [Fact]
        public async Task ThrowOnUnsupportedRuntimeTypeAsync()
        {
            Dictionary<object, int> dictionary = new Dictionary<object, int>();
            dictionary.Add(new Uri("http://github.com/Jozkee"), 1);

            await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializer.SerializeAsync(new MemoryStream(), dictionary));

            await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializer.SerializeAsync(new MemoryStream(), dictionary, _policyOptions));
        }

        [Theory] // Extend this test when support for more types is added.
        [InlineData(@"{""1.1"":1}", typeof(Dictionary<int, int>))]
        [InlineData(@"{""{00000000-0000-0000-0000-000000000000}"":1}", typeof(Dictionary<Guid, int>))]
        public void ThrowOnInvalidFormat(string json, Type typeToConvert)
        {
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, typeToConvert));
            Assert.Contains(typeToConvert.ToString(), ex.Message);
        }
         
        [Theory] // Extend this test when support for more types is added.
        [InlineData(@"{""1.1"":1}", typeof(Dictionary<int, int>))]
        [InlineData(@"{""{00000000-0000-0000-0000-000000000000}"":1}", typeof(Dictionary<Guid, int>))]
        public async Task ThrowOnInvalidFormatAsync(string json, Type typeToConvert)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            Stream stream = new MemoryStream(jsonBytes);

            JsonException ex = await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializer.DeserializeAsync(stream, typeToConvert));
            Assert.Contains(typeToConvert.ToString(), ex.Message);
        }
    }
}
