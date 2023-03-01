// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed unsafe class PointerMemoryManager<T> : MemoryManager<T> where T : struct
    {
        private void* _pointer;
        private readonly int _length;
        private int _retainedCount;
        private bool _disposed;

        internal PointerMemoryManager(void* pointer, int length)
        {
            _pointer = pointer;
            _length = length;
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
        }

        public override Span<T> GetSpan()
        {
            return new Span<T>(_pointer, _length);
        }

        public override unsafe MemoryHandle Pin(int elementIndex = 0)
        {
            // Note that this intentionally allows elementIndex == _length to
            // support pinning zero-length instances.
            if ((uint)elementIndex > (uint)_length)
            {
                throw new ArgumentOutOfRangeException(nameof(elementIndex));
            }

            lock (this)
            {
                if (_retainedCount == 0 && _disposed)
                {
                    throw new Exception();
                }
                _retainedCount++;
            }

            void* pointer = ((byte*)_pointer + elementIndex);    // T = byte
            return new MemoryHandle(pointer, default, this);
        }

        public override void Unpin()
        {
            lock (this)
            {
                if (_retainedCount > 0)
                {
                    _retainedCount--;
                    if (_retainedCount == 0)
                    {
                        if (_disposed)
                        {
                            Marshal.FreeHGlobal((IntPtr)_pointer);
                            _pointer = null;
                        }
                    }
                }
            }
        }
    }
}
