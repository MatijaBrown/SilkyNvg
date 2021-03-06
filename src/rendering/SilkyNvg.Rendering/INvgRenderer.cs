﻿using Silk.NET.Maths;
using SilkyNvg.Blending;
using SilkyNvg.Images;
using System;
using System.Collections.Generic;

namespace SilkyNvg.Rendering
{
    public interface INvgRenderer : IDisposable
    {

        bool EdgeAntiAlias { get; }

        bool Create();

        int CreateTexture(Texture type, Vector2D<uint> size, ImageFlags imageFlags, byte[] data);

        bool DeleteTexture(int image);

        bool UpdateTexture(int image, Vector4D<uint> bounds, byte[] data);

        bool GetTextureSize(int image, out Vector2D<uint> size);

        void Viewport(Vector2D<float> size, float devicePixelRatio);

        void Cancel();

        void Flush();

        void Fill(Paint paint, CompositeOperationState compositeOperation, Scissor scissor, float fringe, Vector4D<float> bounds, IReadOnlyList<Path> paths);

        void Stroke(Paint strokePaint, CompositeOperationState compositeOperation, Scissor scissor, float fringeWidth, float strokeWidth, IReadOnlyList<Path> paths);

        void Triangles(Paint paint, CompositeOperationState compositeOperation, Scissor scissor, Vertex[] vertices, float fringeWidth);

    }
}
