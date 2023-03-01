// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO.Tests
{
    public static class StreamMemoryExtensions
    {
        // most fundamental extension: bytes
        public static Stream AsStream(this Memory<byte> instance) => AsStreamCore(instance);

        public static Stream AsStream(this ReadOnlyMemory<byte> instance) => AsStreamCore(instance);

        public static unsafe Stream AsStreamCore(ReadOnlyMemory<byte> instance)
        {
            if (instance.IsEmpty)
            {
                // Return an empty stream if the memory was empty
                return null;//new MemoryStream<ArrayOwner>(ArrayOwner.Empty, isReadOnly);
            }

            if (MemoryMarshal.TryGetArray(instance, out ArraySegment<byte> segment))
            {
                return new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false);
            }

            // TODO: should we use MemoryManager.Pin or Memory.Pin suffice?
            //if (MemoryMarshal.TryGetMemoryManager(instance, out MemoryManager<byte>? memoryManager, out int start, out int length))
            //{
            //}

            return new UnmanagedMemoryStreamWrapper(instance, writable: false);
        }

        public static Stream AsStream(this ReadOnlySequence<byte> instance)
        {
            return new ReadOnlySequenceStream(instance);
        }

        public static Stream AsStream(this ReadOnlyMemory<char> instance, Encoding? encoding = null)
        {
            return new StringStream(instance, encoding);
        }

        public static Stream AsStream(this string instance, Encoding? encoding = null)
        {
            return new StringStream(instance, encoding ?? Encoding.Default);
        }

        // then, chars, those would require of an encoding.
        //... TODO

        internal class StringStream : Stream
        {
            //private readonly StreamWriter _streamWriter;
            private readonly Encoder _encoder;
            private readonly ReadOnlyMemory<char> _chars;
            private int _charsEncoded;
            private bool _completed;

            internal StringStream(string str, Encoding encoding) : this(str.AsMemory(), encoding) { }

            internal StringStream(ReadOnlyMemory<char> chars, Encoding encoding)
            {
                _chars = chars;
                _encoder = encoding.GetEncoder();
            }

            public override bool CanRead => throw new NotImplementedException();

            public override bool CanSeek => throw new NotImplementedException();

            public override bool CanWrite => throw new NotImplementedException();

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush() => throw new NotImplementedException();

            // From https://learn.microsoft.com/en-us/dotnet/api/system.text.encoder.convert?view=net-7.0
            // GetBytes will throw an exception if the output buffer isn't large enough, but Convert will fill as much space as possible and return the chars read and bytes written.
            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_completed)
                    return 0;

                Span<byte> bufferSpan = buffer.AsSpan(offset, count);

                if (_charsEncoded == _chars.Length)
                {
                    _encoder.Convert(ReadOnlySpan<char>.Empty, bufferSpan, flush: true, out int charsUsed, out int bytesUsed, out _completed);

                    Debug.Assert(charsUsed == 0);
                    return bytesUsed;
                }
                else
                {
                    ReadOnlySpan<char> remaining = _chars.Span.Slice(_charsEncoded);
                    _encoder.Convert(remaining, bufferSpan, flush: false, out int charsUsed, out int bytesUsed, out _completed);
                    _charsEncoded += charsUsed;

                    return bytesUsed;
                }
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }

        internal class ReadOnlySequenceStream : Stream
        {
            private ReadOnlySequence<byte> _sequence;
            private long _position;

            internal ReadOnlySequenceStream(ReadOnlySequence<byte> sequence)
            {
                _sequence = sequence;
            }

            public override bool CanRead => throw new NotImplementedException();

            public override bool CanSeek => throw new NotImplementedException();

            public override bool CanWrite => throw new NotImplementedException();

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush() => throw new NotImplementedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                long remaining = _sequence.Length - _position;

                long bytesToCopy = Math.Min(count, remaining);

                Span<byte> bufferSpan = buffer.AsSpan(offset, count);

                _sequence.Slice(_position, bytesToCopy).CopyTo(bufferSpan);

                _position += bytesToCopy;

                return (int)bytesToCopy;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }

        internal unsafe class UnmanagedMemoryStreamWrapper : Stream
        {
            private readonly MemoryHandle _memoryHandle;
            private readonly UnmanagedMemoryStream _stream;

            public override bool CanRead => _stream.CanRead;

            public override bool CanSeek => _stream.CanSeek;

            public override bool CanWrite => _stream.CanWrite;

            public override long Length => _stream.Length;

            public override long Position { get => _stream.Position; set => _stream.Position = value; }

            public UnmanagedMemoryStreamWrapper(ReadOnlyMemory<byte> memory, bool writable) : base()
            {
                _memoryHandle = memory.Pin();

                long length = memory.Length;
                FileAccess access = writable ? FileAccess.ReadWrite : FileAccess.Read;
                _stream = new UnmanagedMemoryStream((byte*)_memoryHandle.Pointer, length, capacity: length, access);
            }

            protected override void Dispose(bool disposing)
            {
                _stream.Dispose();
                _memoryHandle.Dispose();
                base.Dispose(disposing);
            }

            public override void Flush() => _stream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
            public override void SetLength(long value) => _stream.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
        }
    }
}
