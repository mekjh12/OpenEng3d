using OpenGL;
using System;

namespace Occlusion
{
    public class HierarchicalGpuZBuffer : HierarchicalAbstracZBuffer
    {
        // 셰이더
        protected HzbComputeShader _computeShader;          // 컴퓨트 셰이더

        public HierarchicalGpuZBuffer(int width, int height, string projectPath)
            : base(width, height, projectPath)
        {
            // 셰이더 초기화
            if (_computeShader == null) _computeShader = new HzbComputeShader(projectPath);
        }

        protected void TransferDepthBufferToLevel0()
        {
            _mipmapShader.Bind();
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 
                TextureTarget.Texture2d, _hzbTextures[0], 0);
            Gl.Viewport(0, 0, _width, _height);
            _mipmapShader.LoadDepthBuffer(TextureUnit.Texture0, _depthTexture);
            _mipmapShader.LoadLastMipSize(new Vertex2i(_width, _height));
            Gl.Disable(EnableCap.DepthTest);
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            Gl.Enable(EnableCap.DepthTest);
            _mipmapShader.Unbind();

            // 프레임버퍼 상태 복원
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 
                TextureTarget.Texture2d, _colorTexture, 0);
        }

        /// <summary>
        /// GPU Compute Shader를 사용하여 계층적 밉맵을 생성합니다.
        /// 레벨 0은 Fragment Shader로, 레벨 1 이상은 Compute Shader로 순차 생성합니다.
        /// </summary>
        /// <param name="maxLevel">생성할 최대 레벨 (-1이면 모든 레벨)</param>
        public void GenerateMipmapsUsingCompute(int maxLevel = -1)
        {
            if (_computeShader == null)
                throw new InvalidOperationException("컴퓨트 셰이더가 초기화되지 않았습니다.");

            if (maxLevel < 0)
                maxLevel = _levels - 1;

            // 1단계: 레벨 0 생성
            BindFramebuffer();
            TransferDepthBufferToLevel0();
            UnbindFramebuffer();

            //Gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);

            // 3단계: Compute Shader로 나머지 레벨 생성
            if (maxLevel > 0)
            {
                _computeShader.Bind();

                for (int level = 1; level <= maxLevel; level++)
                {
                    int inputWidth = _width >> (level - 1);
                    int inputHeight = _height >> (level - 1);
                    int outputWidth = _width >> level;
                    int outputHeight = _height >> level;

                    _computeShader.LoadInputSize(new Vertex2i(inputWidth, inputHeight));
                    _computeShader.LoadOutputSize(new Vertex2i(outputWidth, outputHeight));

                    Gl.BindImageTexture(0, _hzbTextures[level - 1], 0, false, 0, BufferAccess.ReadOnly, (InternalFormat)0x822E);
                    Gl.BindImageTexture(1, _hzbTextures[level], 0, false, 0, BufferAccess.WriteOnly, (InternalFormat)0x822E);

                    const int WORK_GROUP_SIZE = 16;
                    int groupsX = (outputWidth + WORK_GROUP_SIZE - 1) / WORK_GROUP_SIZE;
                    int groupsY = (outputHeight + WORK_GROUP_SIZE - 1) / WORK_GROUP_SIZE;

                    Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
                    Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
                }

                _computeShader.Unbind();
            }

            // 4단계: CPU 전송
            if (_zbuffer != null)
            {
                TransferDepthDataToCPU(maxLevel);
            }
        }

    }
}