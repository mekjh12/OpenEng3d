using OpenGL;
using Shader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Occlusion
{
    public class HierarchicalGpuZBuffer : HierarchicalAbstracZBuffer
    {
        private HzbComputeShader _computeShader;


        public HierarchicalGpuZBuffer(int width, int height, string projectPath) : base(width, height, projectPath)
        {
            // 셰이더 초기화
            if (_computeShader == null) _computeShader = new HzbComputeShader(projectPath);
        }


        /// <summary>
        /// 컴퓨트 셰이더를 사용하여 계층적 Z-버퍼의 모든 밉맵 레벨을 GPU에서 생성합니다.<br/>
        /// 큰 해상도(512x512 이상)에서 더 효율적인 방식입니다.<br/>
        /// </summary>
        /// <param name="maxLevel">생성할 최대 레벨 (-1은 모든 레벨)</param>
        /// <remarks>
        /// 성능 특성:<br/>
        /// - 작은 해상도에서는 GenerateZBuffer()보다 느릴 수 있음<br/>
        /// - GPU-GPU 메모리 전송 오버헤드와 메모리 배리어 비용 발생<br/>
        /// - 모든 레벨에 대한 별도 디스패치 오버헤드 발생<br/>
        /// - 1024x1024 이상 해상도에서 확실한 성능 이점 발휘<br/>
        /// </remarks>
        [Obsolete("아직 정상 작동하지 않습니다.")]
        public void GenerateMipmapsUsingCompute(int maxLevel = -1)
        {
            if (_computeShader == null)
                throw new InvalidOperationException("컴퓨트 셰이더가 초기화되지 않았습니다.");

            if (maxLevel < 0)
                maxLevel = _levels - 1;

            // ✅ 레벨 0: GPU에서 렌더링
            BindFramebuffer();
            GenerateHierachyZBufferOnGPU(maxDepth: 0);
            UnbindFramebuffer();

            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            _computeShader.Bind();

            // ✅ 레벨 1부터 시작
            for (int level = 1; level <= maxLevel; level++)
            {
                int inputWidth = _width >> (level - 1);
                int inputHeight = _height >> (level - 1);
                int outputWidth = _width >> level;
                int outputHeight = _height >> level;

                // ✅ 유니폼 설정
                _computeShader.LoadUniform(HzbComputeShader.UNIFORM_NAME.inputSize,
                    new Vertex2i(inputWidth, inputHeight));
                _computeShader.LoadUniform(HzbComputeShader.UNIFORM_NAME.outputDepth, 
                    new Vertex2i(outputWidth, outputHeight));

                // ✅ 이미지 바인딩 (명확한 주석)
                Gl.BindImageTexture(0, _hzbTextures[level - 1], 0, false, 0,
                    BufferAccess.ReadOnly, InternalFormat.R32f);      // 읽기
                Gl.BindImageTexture(1, _hzbTextures[level], 0, false, 0,
                    BufferAccess.WriteOnly, InternalFormat.R32f);     // 쓰기

                // ✅ 배리어: 이전 레벨이 완료되었음을 보장
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                // ✅ 컴퓨트 디스패치
                int groupsX = (int)Math.Ceiling(outputWidth / 16.0);
                int groupsY = (int)Math.Ceiling(outputHeight / 16.0);
                Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);

                // ✅ 배리어: 현재 레벨 쓰기 완료
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            }

            _computeShader.Unbind();

            // 필요한 경우 CPU로 모든 레벨의 데이터 읽기
            if (_zbuffer != null)
            {
                TransferDepthDataToCPU();
            }
        }

    }
}
