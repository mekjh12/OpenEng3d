using OpenGL;
using Shader;
using System;
using System.Collections.Generic;

namespace Renderer
{
    /// <summary>
    /// BillboardCloud 렌더러
    /// 인스턴싱을 사용하여 대량의 Billboard Cloud를 효율적으로 렌더링
    /// </summary>
    public class CrossBillboardRenderer : IDisposable
    {
        private uint _instanceVAO;
        private uint _instanceVBO;
        private int _instanceCount;

        // Atlas 영역 데이터 (캐싱)
        private float[] _atlasOffsets;
        private float[] _atlasSizes;

        public CrossBillboardRenderer()
        {
            InitializeAtlasData();
        }

        /// <summary>
        /// Atlas 영역 데이터를 float 배열로 변환 (Shader에 전달용)
        /// </summary>
        private void InitializeAtlasData()
        {
            _atlasOffsets = new float[6];  // 6 * vec2
            _atlasSizes = new float[6];

            for (int i = 0; i < 3; i++)
            {
                _atlasOffsets[i * 2 + 0] = 0.25f * i;
                _atlasOffsets[i * 2 + 1] = 0.0f;
                _atlasSizes[i * 2 + 0] = 0.25f;
                _atlasSizes[i * 2 + 1] = 1.0f;
            }
        }

        /// <summary>
        /// 인스턴스 데이터 설정
        /// </summary>
        public void SetInstances(List<TreeInstance> instances)
        {
            _instanceCount = instances.Count;

            // 인스턴스 데이터 배열 생성
            // Position(vec3) + Scale(float) = 4 floats per instance
            float[] instanceData = new float[instances.Count * 4];

            for (int i = 0; i < instances.Count; i++)
            {
                instanceData[i * 4 + 0] = instances[i].Position.x;
                instanceData[i * 4 + 1] = instances[i].Position.y;
                instanceData[i * 4 + 2] = instances[i].Position.z;
                instanceData[i * 4 + 3] = instances[i].Scale;
            }

            // VAO 생성
            _instanceVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_instanceVAO);

            // VBO 생성
            _instanceVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer,
                (uint)(instanceData.Length * sizeof(float)),
                instanceData,
                BufferUsage.StaticDraw);

            // Attribute 설정
            int stride = 4 * sizeof(float);

            // Position (location = 0)
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, stride, IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribDivisor(0, 1);  // 인스턴스마다 변경

            // Scale (location = 1)
            Gl.VertexAttribPointer(1, 1, VertexAttribType.Float, false, stride,
                new IntPtr(3 * sizeof(float)));
            Gl.EnableVertexAttribArray(1);
            Gl.VertexAttribDivisor(1, 1);

            Gl.BindVertexArray(0);
        }

        /// <summary>
        /// 렌더링
        /// </summary>
        public void Render(CrossBillboardShader shader, Matrix4x4f vp,
            float objWidth, float objHeight, uint textureId)
        {
            if (_instanceCount == 0) return;

            shader.Bind();

            // Uniform 설정 (atlasRegions 제거)
            shader.LoadVPMatrix(vp);
            shader.LoadObjectSize(objWidth, objHeight);
            shader.LoadAtlasTexture(textureId);
            shader.EnableEdgeLine(false);

            // 렌더링 상태
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Disable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);

            // 인스턴스 렌더링
            Gl.BindVertexArray(_instanceVAO);
            Gl.DrawArraysInstanced(PrimitiveType.Points, 0, 1, _instanceCount);
            Gl.BindVertexArray(0);

            shader.Unbind();
        }

        public void Dispose()
        {
            if (_instanceVAO != 0)
            {
                Gl.DeleteVertexArrays(_instanceVAO);
                _instanceVAO = 0;
            }
            if (_instanceVBO != 0)
            {
                Gl.DeleteBuffers(_instanceVBO);
                _instanceVBO = 0;
            }
        }
    }

    /// <summary>
    /// 개별 나무 인스턴스 데이터
    /// </summary>
    public struct TreeInstance
    {
        public Vertex3f Position;
        public float Scale;

        public TreeInstance(Vertex3f position, float scale)
        {
            Position = position;
            Scale = scale;
        }
    }
}