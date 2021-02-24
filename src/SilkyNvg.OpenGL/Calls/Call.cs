﻿namespace SilkyNvg.OpenGL.Calls
{
    public class Call
    {

        private CallType _type;
        // TODO: Image
        private int _pathOffset;
        private int _pathCount;
        private int _triangleOffset;
        private int _triangleCount;
        private int _uniformOffset;
        private Blend _blend;

        public int PathOffset { get => _pathOffset; set => _pathOffset = value; }
        public int PathCount { get => _pathCount; set => _pathCount = value; }
        public int TriangleOffset { get => _triangleOffset; set => _triangleOffset = value; }
        public int TriangleCount { get => _triangleCount; set => _triangleCount = value; }
        public int UniformOffset { get => _uniformOffset; set => _uniformOffset = value; }
        public Blend Blend { get => _blend; set => _blend = value; }
        internal CallType Type { get => _type; set => _type = value; }

    }
}
