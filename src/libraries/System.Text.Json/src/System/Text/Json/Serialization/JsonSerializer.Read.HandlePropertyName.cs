﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // AggressiveInlining used although a large method it is only called from one locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandlePropertyName(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            if (state.Current.Drain)
            {
                return;
            }

            Debug.Assert(state.Current.ReturnValue != null || state.Current.TempDictionaryValues != null);
            Debug.Assert(state.Current.JsonClassInfo != null);

            bool isProcessingDictObject = state.Current.IsProcessingObject(ClassType.Dictionary);
            if ((isProcessingDictObject || state.Current.IsProcessingProperty(ClassType.Dictionary)) &&
                state.Current.JsonClassInfo.DataExtensionProperty != state.Current.JsonPropertyInfo)
            {
                if (isProcessingDictObject)
                {
                    state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.PolicyProperty;
                }

                if (options.ReferenceHandling.ShouldReadPreservedReferences())
                {
                    ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                    MetadataPropertyName metadata = GetMetadataProperty(propertyName, ref state, ref reader);
                    ResolveMetadataOnDictionary(metadata, ref state);
                }

                state.Current.KeyName = reader.GetString();
            }
            else
            {
                Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

                state.Current.EndProperty();

                ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                if (options.ReferenceHandling.ShouldReadPreservedReferences())
                {
                    MetadataPropertyName metadata = GetMetadataProperty(propertyName, ref state, ref reader);
                    ResolveMetadataOnObject(propertyName, metadata, ref state, ref reader, options);
                }
                else
                {
                    HandlePropertyNameDefault(propertyName, ref state, ref reader, options);
                }
            }
        }

        private static void CreateDataExtensionProperty(
            JsonPropertyInfo jsonPropertyInfo,
            ref ReadStack state)
        {
            Debug.Assert(jsonPropertyInfo != null);
            Debug.Assert(state.Current.ReturnValue != null);

            IDictionary extensionData = (IDictionary)jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);
            if (extensionData == null)
            {
                // Create the appropriate dictionary type. We already verified the types.
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.IsGenericType);
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments().Length == 2);
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[0].UnderlyingSystemType == typeof(string));
                Debug.Assert(
                    jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[1].UnderlyingSystemType == typeof(object) ||
                    jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[1].UnderlyingSystemType == typeof(JsonElement));

                extensionData = (IDictionary)jsonPropertyInfo.RuntimeClassInfo.CreateObject();
                jsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, extensionData);
            }

            // We don't add the value to the dictionary here because we need to support the read-ahead functionality for Streams.
        }

        private static MetadataPropertyName GetMetadataProperty(ReadOnlySpan<byte> propertyName, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (state.Current.ShouldHandleReference)
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
            }

            MetadataPropertyName metadata = GetMetadataPropertyName(propertyName, ref state, ref reader);
            state.Current.MetadataProperty = metadata;
            return metadata;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandlePropertyNameDefault(ReadOnlySpan<byte> propertyName, ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader._stringHasEscaping)
            {
                int idx = propertyName.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                propertyName = GetUnescapedString(propertyName, idx);
            }

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonClassInfo.GetProperty(propertyName, ref state.Current);

            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                JsonPropertyInfo dataExtProperty = state.Current.JsonClassInfo.DataExtensionProperty;
                if (dataExtProperty == null)
                {
                    state.Current.JsonPropertyInfo = JsonPropertyInfo.s_missingProperty;
                }
                else
                {
                    state.Current.JsonPropertyInfo = dataExtProperty;
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    state.Current.KeyName = JsonHelpers.Utf8GetString(propertyName);
                    state.Current.CollectionPropertyInitialized = true;

                    CreateDataExtensionProperty(dataExtProperty, ref state);
                }
            }
            else
            {
                // Support JsonException.Path.
                Debug.Assert(
                    jsonPropertyInfo.JsonPropertyName == null ||
                    options.PropertyNameCaseInsensitive ||
                    propertyName.SequenceEqual(jsonPropertyInfo.JsonPropertyName));

                state.Current.JsonPropertyInfo = jsonPropertyInfo;

                if (jsonPropertyInfo.JsonPropertyName == null)
                {
                    byte[] propertyNameArray = propertyName.ToArray();
                    if (options.PropertyNameCaseInsensitive)
                    {
                        // Each payload can have a different name here; remember the value on the temporary stack.
                        state.Current.JsonPropertyName = propertyNameArray;
                    }
                    else
                    {
                        // Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
                        // so it will match the incoming payload except when case insensitivity is enabled (which is handled above).
                        state.Current.JsonPropertyInfo.JsonPropertyName = propertyNameArray;
                    }
                }
            }

            // Increment the PropertyIndex so JsonClassInfo.GetProperty() starts with the next property.
            state.Current.PropertyIndex++;
        }

        private static void ResolveMetadataOnDictionary(MetadataPropertyName metadata, ref ReadStack state)
        {
            bool isPreserved = state.Current.IsProcessingProperty(ClassType.Dictionary) ?
                state.Current.DictionaryPropertyIsPreserved : state.Current.IsPreserved;

            if (metadata == MetadataPropertyName.Id)
            {
                // Check we are not parsing into immutable.
                if (state.Current.TempDictionaryValues != null)
                {
                    ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(state.Current.JsonPropertyInfo.DeclaredPropertyType);
                }

                if (state.Current.KeyName != null || isPreserved)
                {
                    state.Current.KeyName = null;
                    ThrowHelper.ThrowJsonException_MetadataIdIsNotFirstProperty();
                }

                SetAsPreserved(ref state.Current);
                state.Current.ReadMetadataValue = true;
            }
            else if (metadata == MetadataPropertyName.Ref)
            {
                if (state.Current.KeyName != null || isPreserved)
                {
                    ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                }

                state.Current.ShouldHandleReference = true;
                state.Current.ReadMetadataValue = true;
            }
        }

        private static void ResolveMetadataOnObject(ReadOnlySpan<byte> propertyName, MetadataPropertyName meta, ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (meta == MetadataPropertyName.NoMetadata)
            {
                if (state.Current.IsPreservedArray)
                {
                    ThrowOnRegularPropertyInPreservedArray(propertyName, ref state, ref reader);
                }
                // Regular property, call main logic for HandlePropertyName.
                HandlePropertyNameDefault(propertyName, ref state, ref reader, options);
            }
            else if (meta == MetadataPropertyName.Id)
            {
                if (state.Current.PropertyIndex > 0 || state.Current.IsPreserved)
                {
                    ThrowHelper.ThrowJsonException_MetadataIdIsNotFirstProperty();
                }

                JsonPropertyInfo info = JsonPropertyInfo.s_metadataProperty;
                info.JsonPropertyName = propertyName.ToArray();
                state.Current.JsonPropertyInfo = info;

                state.Current.ReadMetadataValue = true;
                SetAsPreserved(ref state.Current);
            }
            else if (meta == MetadataPropertyName.Values)
            {
                // Preserved JSON arrays are wrapped into JsonPreservedReference<T> where T is the original type of the enumerable
                // and Values is the actual enumerable instance being preserved.
                JsonPropertyInfo info = state.Current.JsonClassInfo.PropertyCache["Values"];
                info.JsonPropertyName = propertyName.ToArray();
                state.Current.JsonPropertyInfo = info;

                if (!state.Current.IsPreserved)
                {
                    ThrowHelper.ThrowJsonException_MetadataMissingIdBeforeValues();
                }
            }
            else // $ref case
            {
                if (state.Current.JsonClassInfo.Type.IsValueType)
                {
                    ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(state.Current.JsonClassInfo.Type);
                }

                if (state.Current.PropertyIndex > 0 || state.Current.IsPreserved)
                {
                    ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                }

                JsonPropertyInfo info = JsonPropertyInfo.s_metadataProperty;
                info.JsonPropertyName = propertyName.ToArray();
                state.Current.JsonPropertyInfo = info;

                state.Current.ReadMetadataValue = true;
                state.Current.ShouldHandleReference = true;
            }
        }

        private static void ThrowOnRegularPropertyInPreservedArray(ReadOnlySpan<byte> propertyName, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (reader._stringHasEscaping)
            {
                int idx = propertyName.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                propertyName = GetUnescapedString(propertyName, idx);
            }

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonClassInfo.GetProperty(propertyName, ref state.Current);
            jsonPropertyInfo.JsonPropertyName = propertyName.ToArray();
            state.Current.JsonPropertyInfo = jsonPropertyInfo;

            ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(state.Current.JsonClassInfo.PropertyCache["Values"].DeclaredPropertyType);
        }
    }
}
