// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        public const uint SYMLINK_FLAG_RELATIVE = 1;

        // https://msdn.microsoft.com/library/windows/hardware/ff552012.aspx
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct REPARSE_DATA_BUFFER
        {
            [FieldOffset(0)]
            public readonly ulong ReparseTag;
            [FieldOffset(4)]
            public readonly ushort ReparseDataLength;
            [FieldOffset(6)]
            public readonly ushort Reserved;
            [FieldOffset(8)]
            public readonly SymbolicLinkReparseBuffer ReparseBufferSymbolicLink;

            // We only need SymbolicLinkReparseBuffer.PathBuffer and its respective offsets and lengths.
            // Commenting out the rest of the definition.

            //[FieldOffset(8)]
            //public readonly MountPointReparseBuffer ReparseBufferMountPoint;
            //[FieldOffset(8)]
            //public readonly GenericReparseBuffer ReparseBufferGeneric;

            [StructLayout(LayoutKind.Sequential)]
            public struct SymbolicLinkReparseBuffer
            {
                public readonly ushort SubstituteNameOffset;
                public readonly ushort SubstituteNameLength;
                public readonly ushort PrintNameOffset;
                public readonly ushort PrintNameLength;
                public readonly uint Flags;
                //public char PathBuffer;
            }

            //[StructLayout(LayoutKind.Sequential)]
            //public struct MountPointReparseBuffer
            //{
            //    private readonly ushort SubstituteNameOffset;
            //    private readonly ushort SubstituteNameLength;
            //    private readonly ushort PrintNameOffset;
            //    private readonly ushort PrintNameLength;
            //    private char _PathBuffer;
            //    // public ReadOnlySpan<char> SubstituteName => TrailingArray<char>.GetBufferInBytes(in _PathBuffer, SubstituteNameLength, SubstituteNameOffset);
            //    // public ReadOnlySpan<char> PrintName => TrailingArray<char>.GetBufferInBytes(in _PathBuffer, PrintNameLength, PrintNameOffset);
            //}

            //public struct GenericReparseBuffer
            //{
            //    public byte DataBuffer;
            //}
        }
    }
}
