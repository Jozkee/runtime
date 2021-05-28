// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.Win32.SafeHandles;

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    public abstract partial class FileSystemInfo : MarshalByRefObject, ISerializable
    {
        // FullPath and OriginalPath are documented fields
        protected string FullPath = null!;          // fully qualified path of the file or directory
        protected string OriginalPath = null!;      // path passed in by the user

        internal string _name = null!; // Fields initiated in derived classes

        protected FileSystemInfo(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        // Full path of the directory/file
        public virtual string FullName => FullPath;

        public string Extension
        {
            get
            {
                int length = FullPath.Length;
                for (int i = length; --i >= 0;)
                {
                    char ch = FullPath[i];
                    if (ch == '.')
                        return FullPath.Substring(i, length - i);
                    if (PathInternal.IsDirectorySeparator(ch) || ch == Path.VolumeSeparatorChar)
                        break;
                }
                return string.Empty;
            }
        }

        public virtual string Name => _name;

        // Whether a file/directory exists
        public virtual bool Exists
        {
            get
            {
                try
                {
                    return ExistsCore;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Delete a file/directory
        public abstract void Delete();

        public DateTime CreationTime
        {
            get => CreationTimeUtc.ToLocalTime();
            set => CreationTimeUtc = value.ToUniversalTime();
        }

        public DateTime CreationTimeUtc
        {
            get => CreationTimeCore.UtcDateTime;
            set => CreationTimeCore = File.GetUtcDateTimeOffset(value);
        }


        public DateTime LastAccessTime
        {
            get => LastAccessTimeUtc.ToLocalTime();
            set => LastAccessTimeUtc = value.ToUniversalTime();
        }

        public DateTime LastAccessTimeUtc
        {
            get => LastAccessTimeCore.UtcDateTime;
            set => LastAccessTimeCore = File.GetUtcDateTimeOffset(value);
        }

        public DateTime LastWriteTime
        {
            get => LastWriteTimeUtc.ToLocalTime();
            set => LastWriteTimeUtc = value.ToUniversalTime();
        }

        public DateTime LastWriteTimeUtc
        {
            get => LastWriteTimeCore.UtcDateTime;
            set => LastWriteTimeCore = File.GetUtcDateTimeOffset(value);
        }

        public string? LinkTarget => _linkTarget ??= RefreshLinkTarget();
        private string? _linkTarget;

        private string? RefreshLinkTarget()
        {
            // Catch all non-links here.
            if ((Attributes & FileAttributes.ReparsePoint) == 0)
            {
                return null;
            }

            // Get the raw link target.

            return null!;
        }

        // DeviceIoControl in Windows.
        // readlink in Unix.
        private unsafe string? GetLinkTargetCore()
        {
            // this needs to be moved to .Windows.cs and the parity method needs to be added on .Unix.cs
            SafeFileHandle handle = Interop.Kernel32.CreateFile(
                FullName,
                dwDesiredAccess: 0,
                FileShare.ReadWrite | FileShare.Delete,
                lpSecurityAttributes: (Interop.Kernel32.SECURITY_ATTRIBUTES*) IntPtr.Zero,
                FileMode.Open,
                dwFlagsAndAttributes: Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS | Interop.Kernel32.FileOperations.FILE_FLAG_OPEN_REPARSE_POINT,
                hTemplateFile: IntPtr.Zero);


            // TODO: Need to find a way to pass lpOutBuffer and then cast it to REPARSE_DATA_BUFFER so we can extract the link target path from it.

            //Interop.Kernel32.DeviceIoControl(
            //    handle,
            //    Interop.Kernel32.FSCTL_GET_REPARSE_POINT,
            //    lpInBuffer: IntPtr.Zero, nInBufferSize: 0,
            //    lpOutBuffer: )
            return null!;
        }

        /// <summary>
        /// Returns the original path. Use FullName or Name properties for the full path or file/directory name.
        /// </summary>
        public override string ToString() => OriginalPath ?? string.Empty;
    }
}
