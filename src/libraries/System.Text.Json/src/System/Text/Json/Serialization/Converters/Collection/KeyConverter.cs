using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Unicode;

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class KeyConverter
    {
        public abstract Type Type { get; }

        internal static readonly Dictionary<Type, KeyConverter> s_KeyConverters = GetSupportedKeyConverters();

        internal static KeyConverter? ResolveKeyConverter(Type keyType)
        {
            if (s_KeyConverters.TryGetValue(keyType!, out KeyConverter? converter))
            {
                return converter;
            }
            else
            {
                // Throw?
                return null;
            }
        }

        private const int NumberOfKeyConverters = 2;

        private static Dictionary<Type, KeyConverter> GetSupportedKeyConverters()
        {
            var converters = new Dictionary<Type, KeyConverter>(NumberOfKeyConverters);

            // Use a dictionary for simple converters.
            foreach (KeyConverter converter in KeyConverters)
            {
                converters.Add(converter.Type, converter);
            }

            Debug.Assert(NumberOfKeyConverters == converters.Count);

            return converters;
        }

        private static IEnumerable<KeyConverter> KeyConverters
        {
            get
            {
                // When adding to this, update NumberOfKeyConverters above.
                yield return new Int32KeyConverter();
                yield return new GuidKeyConverter();
            }
        }
    }

    internal abstract class KeyConverter<T> : KeyConverter
    {
        public abstract T ReadKey(ReadOnlySpan<byte> key);
        public abstract string WriteKey(T key);
        public abstract void WriteKeySpan(Span<byte> buffer, T Key);
        public abstract int DetermineKeyLength(T key);

        public override Type Type => typeof(T);
    }

    internal sealed class Int32KeyConverter : KeyConverter<int>
    {
        public override int ReadKey(ReadOnlySpan<byte> key)
        {
            Utf8Parser.TryParse(key, out int parsedKey, out int _);
            return parsedKey;
        }

        public override string WriteKey(int key)
        {
            return key.ToString();
        }

        public override void WriteKeySpan(Span<byte> buffer, int key)
        {
            Utf8Formatter.TryFormat(key, buffer, out int _);
        }

        public override int DetermineKeyLength(int key)
        {
            int length = (int)Math.Log10(Math.Abs(key)) + 1;

            if (key < 0)
            {
                length++;
            }

            return length;
        }
    }

    internal sealed class GuidKeyConverter : KeyConverter<Guid>
    {
        public override Guid ReadKey(ReadOnlySpan<byte> key)
        {
            Utf8Parser.TryParse(key, out Guid parsedKey, out int _);
            return parsedKey;
        }

        public override string WriteKey(Guid key)
        {
            return key.ToString();
        }

        public override void WriteKeySpan(Span<byte> buffer, Guid key)
        {
            Utf8Formatter.TryFormat(key, buffer, out int _);
        }

        public override int DetermineKeyLength(Guid _)
        {
            return JsonConstants.MaximumFormatGuidLength;
        }
    }
}
