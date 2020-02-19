// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for Dictionary{TKey, TValue} that (de)serializes as a JSON object with properties
    /// representing the dictionary element key and value.
    /// </summary>
    internal sealed class DictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>
        : DictionaryDefaultConverter<TCollection, TKey, TValue>
        where TCollection : Dictionary<TKey, TValue>
        where TKey : notnull
    {
        protected override void Add(TKey key, TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            ((TCollection)state.Current.ReturnValue!)[key] = value;
        }

        protected override void CreateCollection(ref ReadStack state)
        {
            if (state.Current.JsonClassInfo.CreateObject == null)
            {
                ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(state.Current.JsonClassInfo.Type);
            }

            state.Current.ReturnValue = state.Current.JsonClassInfo.CreateObject();
        }

        protected internal override bool OnWriteResume(
            Utf8JsonWriter writer,
            TCollection value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            Dictionary<TKey, TValue>.Enumerator enumerator;
            if (state.Current.CollectionEnumerator == null)
            {
                enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    return true;
                }
            }
            else
            {
                enumerator = (Dictionary<TKey, TValue>.Enumerator)state.Current.CollectionEnumerator;
            }

            JsonConverter<TValue> valueConverter = GetValueConverter(ref state);
            KeyConverter<TKey> keyConverter = (KeyConverter<TKey>)state.Current.JsonClassInfo.KeyConverter;
            if (!state.SupportContinuation && valueConverter.CanUseDirectReadOrWrite)
            {
                // Fast path that avoids validation and extra indirection.
                do
                {
                    WriteKeyName(keyConverter, enumerator.Current.Key, ref state, writer, options);
                    valueConverter.Write(writer, enumerator.Current.Value, options);
                } while (enumerator.MoveNext());
            }
            else
            {
                do
                {
                    if (ShouldFlush(writer, ref state))
                    {
                        state.Current.CollectionEnumerator = enumerator;
                        return false;
                    }

                    TValue element = enumerator.Current.Value;

                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.PropertyState = StackFramePropertyState.Name;
                        WriteKeyName(keyConverter, enumerator.Current.Key, ref state, writer, options);
                    }

                    if (!valueConverter.TryWrite(writer, element, options, ref state))
                    {
                        state.Current.CollectionEnumerator = enumerator;
                        return false;
                    }

                    state.Current.EndDictionaryElement();
                } while (enumerator.MoveNext());
            }

            return true;
        }

        private void WriteKeyName(KeyConverter<TKey> keyConverter, TKey key, ref WriteStack state, Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            // DictionaryKeyPolicy.ConverterName can only take a string key name,
            // So we avoid allocating the string when there is no DictionaryKeyPolicy.
            if (options.DictionaryKeyPolicy == null)
            {

                int length = keyConverter.DetermineKeyLength(key);
                Span<byte> keyNameSpan = stackalloc byte[length];

                keyConverter.WriteKeySpan(keyNameSpan, key);
                writer.WritePropertyName(keyNameSpan);
            }
            else
            {
                string keyNameString = keyConverter.WriteKey(key);
                // Apply KeyPolicy.
                // TODO: DictionaryKeyPolicy != null check is repeated on GetKeyName.
                keyNameString = GetKeyName(keyNameString, ref state, options);
                writer.WritePropertyName(keyNameString);
            }
        }
    }
}
