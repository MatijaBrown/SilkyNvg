﻿using Silk.NET.Maths;
using SilkyNvg.Blending;

namespace SilkyNvg.Renderer
{
    public interface INvgRenderer
    {

        bool EdgeAntiAlias { get; }

        bool Create();

        void Viewport(Vector2D<float> size, float devicePixelRatio);

        void Flush();

        void Fill(Paint paint, CompositeOperationState compositeOperation, Scissor scissor, float fringe, Vector4D<float> bounds, Path[] paths);

        void Stroke(Paint strokePaint, CompositeOperationState compositeOperation, Scissor scissor, float fringeWidth, float strokeWidth, Path[] paths);

    }
}