// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts
{
    public class PatternContextLinearInclude : PatternContextLinear
    {
        public PatternContextLinearInclude(ILinearPattern pattern)
            : base(pattern)
        {
        }

        public override void Declare(Action<IPathSegment, bool> onDeclare)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException(SR.CannotDeclarePathSegment);
            }

            if (Frame.IsNotApplicable)
            {
                return;
            }

            ////PatternBuilder.LinearPattern p = (PatternBuilder.LinearPattern)Pattern;
            ////int segmentIdx = Path.IsPathRooted(p._pattern) ? Frame.RootDir;
            //// if pattern is absolute, get the proper segment index based on the current root.
            //// need to add RootDir.GetSegments().GetPosition(pattern) to SegmentIndex so it can match properly.
            //string[] rootDirSegments = Frame.RootDir!.Split(Path.DirectorySeparatorChar);
            //// Todo: check if need to compare each segment vs the splitted rootdir.
            //// for now just advance to the SegmentIndex + rootdirSegment.Length.


            //// then test with Ragged patterns e.g. C:\*\bar, C:\**\bar
            //int segmentIdx = Frame.SegmentIndex + rootDirSegments.Length;
            //if (segmentIdx < Pattern.Segments.Count) // is this necessary?
            //{
            //    onDeclare(Pattern.Segments[segmentIdx], IsLastSegment());
            //}

            ////for (int i = Frame.SegmentIndex; i < Pattern.Segments.Count; i++)
            ////{
            ////    onDeclare(Pattern.Segments[i], IsLastSegment());
            ////}

            if (Frame.SegmentIndex < Pattern.Segments.Count)
            {
                onDeclare(Pattern.Segments[Frame.SegmentIndex], IsLastSegment());
            }
        }

        public override bool Test(DirectoryInfoBase directory)
        {
            if (IsStackEmpty())
            {
                throw new InvalidOperationException(SR.CannotTestDirectory);
            }

            if (Frame.IsNotApplicable)
            {
                return false;
            }

            return !IsLastSegment() && TestMatchingSegment(directory.Name);
        }
    }
}
