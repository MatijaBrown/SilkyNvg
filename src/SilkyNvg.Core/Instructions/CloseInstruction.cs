﻿using SilkyNvg.Core.Paths;

namespace SilkyNvg.Core.Instructions
{
    internal struct CloseInstruction : IInstruction
    {

        private readonly PathCache _pathCache;

        public CloseInstruction(PathCache pathCache)
        {
            _pathCache = pathCache;
        }

        public void BuildPaths()
        {
            _pathCache.LastPath.Close();
        }

    }
}
