﻿using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SilkyNvg.Blending;
using SilkyNvg.Renderer;
using SilkyNvg.Rendering.OpenGL.Blending;
using SilkyNvg.Rendering.OpenGL.Calls;
using SilkyNvg.Rendering.OpenGL.Shaders;
using SilkyNvg.Rendering.OpenGL.Utils;
using System;
using Shader = SilkyNvg.Rendering.OpenGL.Shaders.Shader;

namespace SilkyNvg.Rendering.OpenGL
{
    public sealed class OpenGLRenderer : INvgRenderer
    {

        private readonly CreateFlags _flags;
        private readonly VertexCollection _vertexCollection;
        private readonly CallQueue _callQueue;

        private VAO _vao;

        private Vector2D<float> _size;

        internal GL Gl { get; }

        internal bool StencilStrokes => _flags.HasFlag(CreateFlags.StencilStrokes);

        internal bool Debug => _flags.HasFlag(CreateFlags.Debug);

        internal StateFilter Filter { get; private set; }

        internal Shader Shader { get; private set; }

        public bool EdgeAntiAlias => _flags.HasFlag(CreateFlags.Antialias);

        public OpenGLRenderer(CreateFlags flags, GL gl)
        {
            _flags = flags;
            Gl = gl;

            _vertexCollection = new VertexCollection();
            _callQueue = new CallQueue();
        }

        internal void StencilMask(uint mask)
        {
            if (Filter.StencilMask != mask)
            {
                Filter.StencilMask = mask;
                Gl.StencilMask(mask);
            }
        }

        internal void StencilFunc(StencilFunction func, int @ref, uint mask)
        {
            if (Filter.StencilFunc != func ||
                Filter.StencilFuncRef != @ref ||
                Filter.StencilFuncMask != mask)
            {
                Filter.StencilFunc = func;
                Filter.StencilFuncRef = @ref;
                Filter.StencilFuncMask = mask;
                Gl.StencilFunc(func, @ref, mask);
            }
        }

        internal void CheckError(string str)
        {
            if (!Debug)
            {
                return;
            }

            GLEnum err = Gl.GetError();
            if (err != GLEnum.NoError)
            {
                Console.Error.WriteLine("Error " + err + " after" + Environment.NewLine + str);
                return;
            }
        }

        public bool Create()
        {
            CheckError("init");

            if (EdgeAntiAlias)
            {
                Shader = new Shader("SilkyNvg-Shader", "vertexShader", "fragmentShaderEdgeAA", Gl);
                if (!Shader.Status)
                {
                    return false;
                }
            }
            else
            {
                Shader = new Shader("SilkyNvg-Shader", "vertexShader", "fragmentShader", Gl);
                if (!Shader.Status)
                {
                    return false;
                }
            }

            CheckError("uniform locations");
            Shader.GetUniforms();

            _vao = new(Gl);
            _vao.Vbo = new(Gl);

            // TODO: Dummy texture

            Filter = new StateFilter();

            CheckError("create done!");

            Gl.Finish();

            return true;
        }

        public void Viewport(Vector2D<float> size, float devicePixelRatio)
        {
            _size = size;
        }

        public void Flush()
        {
            if (_callQueue.HasCalls)
            {
                Shader.Start();

                Gl.Enable(EnableCap.CullFace);
                Gl.CullFace(CullFaceMode.Back);
                Gl.FrontFace(FrontFaceDirection.Ccw);
                Gl.Enable(EnableCap.Blend);
                Gl.Disable(EnableCap.DepthTest);
                Gl.Disable(EnableCap.ScissorTest);
                Gl.ColorMask(true, true, true, true);
                Gl.StencilMask(0xffffffff);
                Gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
                Gl.StencilFunc(StencilFunction.Always, 0, 0xffffffff);
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2D, 0);
                Filter.BoundTexture = 0;
                Filter.StencilMask = 0xffffffff;
                Filter.StencilFunc = StencilFunction.Always;
                Filter.StencilFuncRef = 0;
                Filter.StencilFuncMask = 0xffffffff;
                Filter.BlendFunc = new Blend(GLEnum.InvalidEnum, GLEnum.InvalidEnum, GLEnum.InvalidEnum, GLEnum.InvalidEnum);

                _vao.Bind();
                _vao.Vbo.Update(_vertexCollection.Vertices);

                Shader.LoadInt(UniformLoc.Tex, 0);
                Shader.LoadVector(UniformLoc.ViewSize, _size);

                _callQueue.Run();

                Gl.DisableVertexAttribArray(0);
                Gl.DisableVertexAttribArray(1);

                _vao.Unbind();

                Gl.Disable(EnableCap.CullFace);
                Shader.Stop();
                // TODO: Unbind texture
            }

            _vertexCollection.Clear();
            _callQueue.Clear();
        }

        public void Fill(Paint paint, CompositeOperationState compositeOperation, Scissor scissor, float fringe, Vector4D<float> bounds, Renderer.Path[] paths)
        {
            int offset = _vertexCollection.CurrentsOffset;
            Path[] renderPaths = new Path[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                Renderer.Path path = paths[i];
                renderPaths[i] = new Path(
                    _vertexCollection.CurrentsOffset, path.Fill.Count,
                    _vertexCollection.CurrentsOffset + path.Fill.Count, path.Stroke.Count
                );
                _vertexCollection.AddVertices(path.Fill);
                _vertexCollection.AddVertices(path.Stroke);
                offset += path.Fill.Count;
                offset += path.Stroke.Count;
            }


            FragUniforms uniforms = new(paint, scissor, fringe, fringe, -1.0f);
            Call call;
            if ((paths.Length == 1) && paths[0].Convex) // Convex
            {
                call = new ConvexFillCall(paint.Image, renderPaths, uniforms, compositeOperation, this);
            }
            else
            {
                _vertexCollection.AddVertex(new Vertex(bounds.Z, bounds.W, 0.5f, 1.0f));
                _vertexCollection.AddVertex(new Vertex(bounds.Z, bounds.Y, 0.5f, 1.0f));
                _vertexCollection.AddVertex(new Vertex(bounds.X, bounds.W, 0.5f, 1.0f));
                _vertexCollection.AddVertex(new Vertex(bounds.X, bounds.Y, 0.5f, 1.0f));

                FragUniforms stencilUniforms = new(-1.0f, Shaders.ShaderType.Simple);

                call = new FillCall(paint.Image, renderPaths, offset, stencilUniforms, uniforms, compositeOperation, this);
            }

            _callQueue.Add(call);
        }

        public void Stroke(Paint paint, CompositeOperationState compositeOperation, Scissor scissor, float fringe, float strokeWidth, Renderer.Path[] paths)
        {
            int offset = _vertexCollection.CurrentsOffset;
            Path[] renderPaths = new Path[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i].Stroke.Count > 0)
                {
                    renderPaths[i] = new Path(0, 0, offset, paths[i].Stroke.Count);
                }
                else
                {
                    renderPaths[i] = default;
                }
                _vertexCollection.AddVertices(paths[i].Stroke);
                offset += paths[i].Stroke.Count;
            }

            Call call;
            if (StencilStrokes)
            {
                FragUniforms uniforms = new(paint, scissor, strokeWidth, fringe, -1.0f);
                FragUniforms stencilUniforms = new(paint, scissor, strokeWidth, fringe, 1.0f - 0.5f / 255.0f);

                call = new StencilStrokeCall(paint.Image, renderPaths, stencilUniforms, uniforms, compositeOperation, this);
            }
            else
            {
                FragUniforms uniforms = new(paint, scissor, strokeWidth, fringe, -1.0f);

                call = new StrokeCall(paint.Image, renderPaths, uniforms, compositeOperation, this);
            }
            _callQueue.Add(call);
        }

    }
}
