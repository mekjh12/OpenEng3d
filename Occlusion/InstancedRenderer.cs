using Common;
using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Runtime.InteropServices;

namespace Occlusion
{
    public class InstancedRenderer : IDisposable
    {
        private BaseModel3d[] model;
        private int _instanceCount;
        private Matrix4x4f[] _modelMatrices;

        private uint instanceSSBO;
        private bool isDirty;
        private int maxInstances;
        private bool isDrawOneSide;

        private Matrix4x4f[] _buffer;
        private AABB3f[] _bufferAABBs;
        private int _bufferCount;

        // GPU 데이터 변환용 버퍼 (AABB와 동일한 방식)
        private Matrix4x4fGpu[] _gpuMatrixBuffer;
        private int _gpuMatrixBufferCapacity = 0;

        // SSBO 바인딩 포인트
        private const int INSTANCE_BUFFER_BINDING = 0;

        // 속성
        public int InstanceCount => _instanceCount;
        public int MaxInstances => maxInstances;
        public bool IsDrawOneSide { get => isDrawOneSide; set => isDrawOneSide = value; }
        public int BufferCount => _bufferCount;
        public ref Matrix4x4f[] Buffer => ref _buffer;
        public ref AABB3f[] BufferAABBs => ref _bufferAABBs;

        public InstancedRenderer(BaseModel3d[] model, int maxInstances = 100000, bool isDrawOneSide = true)
        {
            this.model = model;
            this.maxInstances = maxInstances;
            this.isDrawOneSide = isDrawOneSide;
            _modelMatrices = new Matrix4x4f[maxInstances];
            _buffer = new Matrix4x4f[maxInstances];
            _bufferAABBs = new AABB3f[maxInstances];
            _instanceCount = 0;
            _bufferCount = 0;
            this.instanceSSBO = Gl.GenBuffer();
            this.isDirty = false;

            // SSBO 초기 생성 (최대 크기로)
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, instanceSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(maxInstances * Marshal.SizeOf<Matrix4x4fGpu>()),
                IntPtr.Zero,
                BufferUsage.DynamicDraw);
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        public void ClearVisibleInstances()
        {
            _bufferCount = 0;
            isDirty = true;
        }

        public void AddInstance(Matrix4x4f worldMatrix)
        {
            if (_instanceCount >= maxInstances)
            {
                throw new InvalidOperationException($"최대 인스턴스 수({maxInstances})를 초과했습니다.");
            }

            _modelMatrices[_instanceCount] = worldMatrix;
            _instanceCount++;
        }

        public void AddVisibleInstance(Matrix4x4f worldMatrix, AABB3f aabb)
        {
            if (_bufferCount >= maxInstances)
            {
                throw new InvalidOperationException($"최대 인스턴스 수({maxInstances})를 초과했습니다.");
            }

            _buffer[_bufferCount] = worldMatrix;
            _bufferAABBs[_bufferCount] = aabb;
            _bufferCount++;
            isDirty = true;
        }

        public void SetInstance(int index, Matrix4x4f worldMatrix)
        {
            if (index < 0 || index >= maxInstances)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _modelMatrices[index] = worldMatrix;

            if (index >= _instanceCount)
            {
                _instanceCount = index + 1;
            }

            isDirty = true;
        }

        public void ClearInstances()
        {
            _instanceCount = 0;
            isDirty = true;
        }

        /// <summary>
        /// SSBO 버퍼 업데이트 (AABB와 동일한 방식)
        /// </summary>
        private void UpdateInstanceBuffer()
        {
            if (!isDirty || _bufferCount == 0)
                return;

            // GPU 데이터 버퍼 크기 확인 및 필요시 확장
            if (_gpuMatrixBuffer == null || _gpuMatrixBufferCapacity < _bufferCount)
            {
                _gpuMatrixBufferCapacity = _bufferCount;
                _gpuMatrixBuffer = new Matrix4x4fGpu[_bufferCount];
            }

            // Matrix4x4f를 GPU 호환 형식으로 변환
            for (int i = 0; i < _bufferCount; i++)
            {
                _gpuMatrixBuffer[i] = new Matrix4x4fGpu(_buffer[i]);
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, instanceSSBO);

            unsafe
            {
                fixed (Matrix4x4fGpu* ptr = _gpuMatrixBuffer)
                {
                    int sizeInBytes = Marshal.SizeOf<Matrix4x4fGpu>() * _bufferCount;
                    Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                        (uint)sizeInBytes,
                        new IntPtr(ptr),
                        BufferUsage.DynamicDraw);
                }
            }

            // SSBO를 바인딩 포인트에 연결
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, INSTANCE_BUFFER_BINDING, instanceSSBO);
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            isDirty = false;
        }

        /// <summary>
        /// 깊이만 렌더링 (Hi-Z 버퍼용) - SSBO 사용
        /// </summary>
        public void RenderDepth(InstancedDepthShader shader, Camera camera)
        {
            if (_bufferCount == 0) return;

            UpdateInstanceBuffer();

            shader.Bind();
            shader.LoadViewMatrix(camera.ViewMatrix);
            shader.LoadProjectionMatrix(camera.ProjectiveMatrix);
            shader.SetupRenderState();

            for (int i = 0; i < model.Length; i++)
            {
                TexturedModel texturedModel = model[i] as TexturedModel;

                // ✅ 텍스처 설정
                if (texturedModel != null)
                {
                    if (texturedModel.Texture.TextureType.HasFlag(Texture.TextureMapType.Diffuse))
                    {
                        shader.LoadTexture(TextureUnit.Texture0, texturedModel.Texture.DiffuseMapID);
                    }
                }

                Gl.BindVertexArray(model[i].VAO);

                // ✅ position과 texcoord 모두 활성화
                Gl.EnableVertexAttribArray(0);  // position
                Gl.EnableVertexAttribArray(1);  // texcoord

                Gl.DrawArraysInstanced(
                    PrimitiveType.Triangles,
                    0,
                    model[i].VertexCount,
                    _bufferCount
                );

                // ✅ 정리
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            shader.RestoreRenderState();
            shader.Unbind();
        }

        /// <summary>
        /// 인스턴스 렌더링 - SSBO 사용
        /// </summary>
        public void Render(InstancedShader shader, Camera camera)
        {
            if (_bufferCount == 0) return;

            UpdateInstanceBuffer();

            shader.Bind();

            shader.LoadVPMatrix(camera.VPMatrix);

            if (isDrawOneSide)
            {
                Gl.Enable(EnableCap.CullFace);
                Gl.CullFace(CullFaceMode.Back);
            }
            else
            {
                Gl.Disable(EnableCap.CullFace);
            }

            for (int i = 0; i < model.Length; i++)
            {
                TexturedModel texturedModel = model[i] as TexturedModel;

                if (texturedModel != null)
                {
                    if (texturedModel.Texture.TextureType.HasFlag(Texture.TextureMapType.Diffuse))
                    {
                        shader.LoadTexture(TextureUnit.Texture0, texturedModel.Texture.DiffuseMapID);
                    }
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                }

                Gl.BindVertexArray(model[i].VAO);

                // ✅ Vertex Attribute 활성화 (RenderDepth와 동일)
                Gl.EnableVertexAttribArray(0); // position
                Gl.EnableVertexAttribArray(1); // texcoord

                Gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, model[i].VertexCount, _bufferCount);

                // ✅ 정리
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            shader.Unbind();
        }

        public void Dispose()
        {
            if (instanceSSBO != 0)
            {
                Gl.DeleteBuffers(instanceSSBO);
                instanceSSBO = 0;
            }
        }
    }

    /// <summary>
    /// GPU SSBO와 호환되는 Matrix4x4 구조체 (std430 레이아웃)
    /// GLSL mat4와 호환되도록 열 우선(column-major) 순서로 저장
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct Matrix4x4fGpu
    {
        // 열 우선 순서로 16개 float
        // GLSL: mat4[0] = 첫 번째 열, mat4[1] = 두 번째 열, ...
        public float m00, m10, m20, m30;  // 첫 번째 열
        public float m01, m11, m21, m31;  // 두 번째 열
        public float m02, m12, m22, m32;  // 세 번째 열
        public float m03, m13, m23, m33;  // 네 번째 열
                                          // 총 64 bytes

        public Matrix4x4fGpu(in Matrix4x4f matrix)
        {
            // ✅ 행과 열을 바꿈 - [col, row]로 읽기
            m00 = matrix[0, 0]; m10 = matrix[0, 1]; m20 = matrix[0, 2]; m30 = matrix[0, 3];
            m01 = matrix[1, 0]; m11 = matrix[1, 1]; m21 = matrix[1, 2]; m31 = matrix[1, 3];
            m02 = matrix[2, 0]; m12 = matrix[2, 1]; m22 = matrix[2, 2]; m32 = matrix[2, 3];
            m03 = matrix[3, 0]; m13 = matrix[3, 1]; m23 = matrix[3, 2]; m33 = matrix[3, 3];
        }
    }
}