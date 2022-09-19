// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarWriter_WriteEntry_Roundtrip_Tests : TarTestsBase
    {
        private const char OneByteCharacter = 'a';
        private const char TwoBytesCharacter = 'ö';
        private const string FourBytesCharacter = "😒";
        private const char Separator = '/';
        private const int MaxPathComponent = 255; // although this is the max path component allowed on most file systems, tar itself does not have limitations.

        private static IEnumerable<string> GetNamesRootedTestData(NameCapabilities max)
        {
            // rooted
            yield return Separator + Repeat(OneByteCharacter, 99);
            //yield return Separator + Repeat(OneByteCharacter, 100); // this should throw on v7.
            if (OperatingSystem.IsWindows())
                yield return "C:/" + Repeat(OneByteCharacter, 97);

            if (max == NameCapabilities.Name)
                yield break;

            yield return Separator + Repeat(OneByteCharacter, 154) + Separator + Repeat(OneByteCharacter, 100);
            if (OperatingSystem.IsWindows())
                yield return "C:/" + Repeat(OneByteCharacter, 152) + Separator + Repeat(OneByteCharacter, 100);

            // Pax and Gnu support unlimited paths.
            if (max == NameCapabilities.NameAndPrefix)
                yield break;

            yield return Separator + Repeat(OneByteCharacter, MaxPathComponent) + Separator + Repeat(OneByteCharacter, MaxPathComponent);
            if (OperatingSystem.IsWindows())
                yield return "C:/" + Repeat(OneByteCharacter, MaxPathComponent) + Separator + Repeat(OneByteCharacter, MaxPathComponent);
        }

        private static IEnumerable<string> GetNamesNonAsciiTestData(NameCapabilities max)
        {
            Assert.True(Enum.IsDefined(max));

            yield return Repeat(OneByteCharacter, 100);
            yield return Repeat(TwoBytesCharacter, 100 / 2);
            yield return Repeat(OneByteCharacter, 2) + Repeat(TwoBytesCharacter, 49);

            yield return Repeat(FourBytesCharacter, 100 / 4);
            yield return Repeat(OneByteCharacter, 4) + Repeat(FourBytesCharacter, 24);

            if (max == NameCapabilities.Name)
                yield break;

            // no prefix, these tests need to go on throw cases, because path can't be split.
            //yield return new string(OneByteCharacter, 255); // This test won't work. // TODO add opposite test where 256 would throw.
            // add 2 bytes char
            // add 4 bytes char

            // prefix + name
            // this is 256 but is supported because prefix is not required to end in separator.
            yield return Repeat(OneByteCharacter, 155) + Separator + Repeat(OneByteCharacter, 100);

            // non-ascii prefix + name 
            yield return Repeat(TwoBytesCharacter, 155 / 2) + Separator + Repeat(OneByteCharacter, 100);
            yield return Repeat(FourBytesCharacter, 155 / 4) + Separator + Repeat(OneByteCharacter, 100);

            // prefix + non-ascii name
            yield return Repeat(OneByteCharacter, 155) + Separator + Repeat(TwoBytesCharacter, 100 / 2);
            yield return Repeat(OneByteCharacter, 155) + Separator + Repeat(FourBytesCharacter, 100 / 4);

            // non-ascii prefix + non-ascii name
            yield return Repeat(TwoBytesCharacter, 155 / 2) + Separator + Repeat(TwoBytesCharacter, 100 / 2);
            yield return Repeat(FourBytesCharacter, 155 / 4) + Separator + Repeat(FourBytesCharacter, 100 / 4);

            // Pax and Gnu support unlimited paths.
            if (max == NameCapabilities.NameAndPrefix)
                yield break;

            yield return Repeat(OneByteCharacter, MaxPathComponent);
            yield return Repeat(TwoBytesCharacter, MaxPathComponent / 2);
            yield return Repeat(FourBytesCharacter, MaxPathComponent / 4);

            yield return Repeat(OneByteCharacter, MaxPathComponent) + Separator + Repeat(OneByteCharacter, MaxPathComponent);
            yield return Repeat(TwoBytesCharacter, MaxPathComponent / 2) + Separator + Repeat(TwoBytesCharacter, MaxPathComponent / 2);
            yield return Repeat(FourBytesCharacter, MaxPathComponent / 4) + Separator + Repeat(FourBytesCharacter, MaxPathComponent / 4);
        }

        private static string Repeat(char c, int count)
        {
            return new string(c, count);
        }

        private static string Repeat(string c, int count)
        {
            return string.Concat(Enumerable.Repeat(c, count));
        }

        private enum NameCapabilities
        {
            Name,
            NameAndPrefix,
            Unlimited
        }

        public static IEnumerable<object[]> NameRoundtripsTheoryData()
        {
            foreach (TarEntryType entryType in new[] { TarEntryType.RegularFile, TarEntryType.Directory })
            {
                TarEntryType v7EntryType = entryType is TarEntryType.RegularFile ? TarEntryType.V7RegularFile : entryType;
                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesRootedTestData(NameCapabilities.Name)))
                {
                    yield return new object[] { TarEntryFormat.V7, v7EntryType, name };
                }

                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.NameAndPrefix).Concat(GetNamesRootedTestData(NameCapabilities.NameAndPrefix)))
                {
                    yield return new object[] { TarEntryFormat.Ustar, entryType, name };
                }

                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Unlimited).Concat(GetNamesRootedTestData(NameCapabilities.Unlimited)))
                {
                    yield return new object[] { TarEntryFormat.Pax, entryType, name };
                    yield return new object[] { TarEntryFormat.Gnu, entryType, name };
                }
            }
        }

        [Theory]
        [MemberData(nameof(NameRoundtripsTheoryData))]
        public void NameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, string name)
        {
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.Name = name;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = reader.GetNextEntry();
            Assert.Null(reader.GetNextEntry());
            Assert.Equal(name, entry.Name);
        }

        public static IEnumerable<object[]> LinkNameRoundtripsTheoryData()
        {
            foreach (TarEntryType entryType in new[] { TarEntryType.SymbolicLink, TarEntryType.HardLink })
            {
                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Name).Concat(GetNamesRootedTestData(NameCapabilities.Name)))
                {
                    yield return new object[] { TarEntryFormat.V7, entryType, name };
                    yield return new object[] { TarEntryFormat.Ustar, entryType, name };
                }

                foreach (string name in GetNamesNonAsciiTestData(NameCapabilities.Unlimited).Concat(GetNamesRootedTestData(NameCapabilities.Unlimited)))
                {
                    yield return new object[] { TarEntryFormat.Pax, entryType, name };
                    yield return new object[] { TarEntryFormat.Gnu, entryType, name };
                }
            }
        }

        [Theory]
        [MemberData(nameof(LinkNameRoundtripsTheoryData))]
        public void LinkNameRoundtrips(TarEntryFormat entryFormat, TarEntryType entryType, string linkName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, name);
            entry.LinkName = linkName;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = reader.GetNextEntry();
            Assert.Null(reader.GetNextEntry());
            Assert.Equal(name, entry.Name);
            Assert.Equal(linkName, entry.LinkName);
        }


        public static IEnumerable<object[]> UserNameGroupNameRoundtripsTheoryData()
        {
            foreach (TarEntryFormat entryType in new[] { TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                yield return new object[] { entryType, Repeat(OneByteCharacter, 32) };
                yield return new object[] { entryType, Repeat(TwoBytesCharacter, 32 / 2) };
                yield return new object[] { entryType, Repeat(FourBytesCharacter, 32 / 4) };
            }
        }

        [Theory]
        [MemberData(nameof(UserNameGroupNameRoundtripsTheoryData))]
        public void UserNameGroupNameRoundtrips(TarEntryFormat entryFormat, string userGroupName)
        {
            string name = "foo";
            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, TarEntryType.RegularFile, name);
            PosixTarEntry posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            posixEntry.UserName = userGroupName;
            posixEntry.GroupName = userGroupName;

            MemoryStream ms = new();
            using (TarWriter writer = new(ms, leaveOpen: true))
            {
                writer.WriteEntry(posixEntry);
            }

            ms.Position = 0;
            using TarReader reader = new(ms);

            entry = reader.GetNextEntry();
            posixEntry = Assert.IsAssignableFrom<PosixTarEntry>(entry);
            Assert.Null(reader.GetNextEntry());

            Assert.Equal(name, posixEntry.Name);
            Assert.Equal(userGroupName, posixEntry.UserName);
            Assert.Equal(userGroupName, posixEntry.GroupName);
        }
    }
}
