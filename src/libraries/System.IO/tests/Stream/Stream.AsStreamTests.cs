// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Memory.Tests.SequenceReader;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class StreamAsStreamTests
    {
        // since we are inspecting internal properties of Memory/Span, we want to check that
        // all instances created by the public AsMemory()/AsSpan() methods are able to roundtrip.

        // TODO: test that readonly* types make an unwrittable Stream.

        [Fact]
        public unsafe void AsStreamRoundtrips_Memory() // happy path.
        {
            Memory<byte> memory = new byte[1024];
            Random.Shared.NextBytes(memory.Span);

            using Stream s = memory.AsStream();
            VerifyStreamRoundtrips(s, memory.Span);
        }

        [Fact]
        public unsafe void AsStreamRoundtrips_ReadOnlyMemory() // happy path 2.
        {
            var bytes = new byte[1024];
            ReadOnlyMemory<byte> memory = bytes;
            Random.Shared.NextBytes(bytes);

            using Stream s = memory.AsStream();
            VerifyStreamRoundtrips(s, memory.Span);
        }

        [Fact]
        public unsafe void AsStreamRoundtrips_NativeMemoryManager()
        {
            const int Length = 1024;
            using (MemoryManager<byte> manager = new NativeMemoryManager(Length))
            {
                Memory<byte> memory = manager.Memory;
                Random.Shared.NextBytes(memory.Span);
                Stream s = ((ReadOnlyMemory<byte>)manager.Memory).AsStream();
                VerifyStreamRoundtrips(s, memory.Span);
            }
        }

        [Fact]
        public unsafe void AsStreamRoundtrips_PointerMemoryManager()
        {
            Span<byte> span = stackalloc byte[1024];
            Random.Shared.NextBytes(span);

            fixed (byte* ptr = &MemoryMarshal.GetReference(span))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, span.Length))
                {
                    using Stream s = ((ReadOnlyMemory<byte>)manager.Memory).AsStream();
                    VerifyStreamRoundtrips(s, span);
                }
            }
        }

        [Fact]
        public unsafe void AsStreamRoundtrips_ReadOnlySequence()
        {
            byte[] buf1 = new byte[512];
            byte[] buf2 = new byte[512];
            Random.Shared.NextBytes(buf1);
            Random.Shared.NextBytes(buf2);

            byte[] expected = new byte[1024];
            Array.Copy(buf1, expected, buf1.Length);
            Array.Copy(buf2, 0, expected, buf1.Length, buf2.Length);

            ReadOnlySequence<byte> sequence = SequenceFactory.Create(buf1, buf2);

            using (Stream s = sequence.AsStream())
            {
                VerifyStreamRoundtrips(s, expected);
            }
        }

        [Fact]
        public unsafe void AsStreamRoundtrips_ReadOnlyMemoryChar()
        {
            string expected = "foobarbaz👩👨👨";
            ReadOnlyMemory<char> memory = expected.AsMemory();

            using (Stream s = memory.AsStream(Encoding.UTF8))
            {
                VerifyStreamRoundtrips(s, Encoding.UTF8.GetBytes(expected));
            }
        }

        [Fact]
        public unsafe void AsStreamRoundtrips_String()
        {
            string expected = "foobarbaz👩👨👨";

            using (Stream s = expected.AsStream(Encoding.UTF8))
            {
                VerifyStreamRoundtrips(s, Encoding.UTF8.GetBytes(expected));
            }
        }

        private static void VerifyStreamRoundtrips(Stream s, ReadOnlySpan<byte> expected)
        {
            byte[] actual = new byte[expected.Length];
            s.ReadExactly(actual, 0, actual.Length);
            AssertExtensions.SequenceEqual(expected, actual);
        }
    }
}
