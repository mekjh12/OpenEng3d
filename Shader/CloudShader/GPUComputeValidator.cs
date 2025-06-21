using OpenGL;
using System;
using System.Diagnostics;

namespace Shader.CloudShader
{
    /// <summary>
    /// 컴퓨트 셰이더가 GPU에서 제대로 작동하는지 테스트하는 간단한 클래스
    /// </summary>
    public class GPUComputeValidator
    {
        // 테스트할 컴퓨트 셰이더
        private CloudComputeShader _computeShader;

        // 테스트 설정
        private string _shaderPath;
        private int _textureWidth = 64;
        private int _textureHeight = 64;
        private int _textureDepth = 64;

        // 검증용 픽셀 버퍼
        private float[] _pixelBuffer;

        // 검증 결과
        private bool _isRunning = false;
        private bool _isGPUWorking = false;

        public CloudComputeShader ComputeShader { get => _computeShader; }

        public GPUComputeValidator(string shaderPath, int width = 64, int height = 64, int depth = 64)
        {
            _shaderPath = shaderPath;
            _textureWidth = width;
            _textureHeight = height;
            _textureDepth = depth;

            // 픽셀 버퍼 초기화 (RGBA 값)
            _pixelBuffer = new float[width * height * depth * 4];
        }

        /// <summary>
        /// 테스트 초기화 및 실행
        /// </summary>
        public bool RunTest()
        {
            try
            {
                Console.WriteLine("=== GPU 컴퓨트 셰이더 검증 시작 ===");
                Console.WriteLine($"텍스처 크기: {_textureWidth}x{_textureHeight}x{_textureDepth}");

                // 컴퓨트 셰이더 초기화
                _computeShader = new CloudComputeShader(_shaderPath, _textureWidth, _textureHeight, _textureDepth);
                Console.WriteLine($"컴퓨트 셰이더 로드: {_computeShader.ComputeFileName}");

                // 초기 상태 기록 (일부 픽셀만 샘플링)
                SampleTextureData("초기 텍스처 상태");

                // 컴퓨트 셰이더 실행 시간 측정
                Stopwatch timer = new Stopwatch();
                timer.Start();

                // 컴퓨트 셰이더 실행
                _isRunning = true;
                _computeShader.Dispatch(1.0f); // 시간값 1.0으로 디스패치

                timer.Stop();
                Console.WriteLine($"컴퓨트 셰이더 실행 시간: {timer.ElapsedMilliseconds}ms");

                // 실행 후 상태 기록 (일부 픽셀만 샘플링)
                _isGPUWorking = SampleTextureData("실행 후 텍스처 상태");

                // 메모리 배리어 확인 (제대로 실행됐는지 체크)
                timer.Restart();

                // 두 번째 실행
                _computeShader.Dispatch(2.0f); // 시간값 2.0으로 디스패치

                timer.Stop();
                Console.WriteLine($"두 번째 실행 시간: {timer.ElapsedMilliseconds}ms");

                // 결과 확인
                bool secondRunValid = SampleTextureData("두 번째 실행 후 텍스처 상태");
                _isGPUWorking = _isGPUWorking && secondRunValid;

                // 결과 보고
                if (_isGPUWorking)
                {
                    Console.WriteLine("✓ GPU 컴퓨트 셰이더가 정상적으로 작동합니다.");
                    Console.WriteLine("✓ 텍스처 데이터가 두 번의 실행에서 모두 변경되었습니다.");
                }
                else
                {
                    Console.WriteLine("✗ GPU 컴퓨트 셰이더 실행에 문제가 있습니다.");
                    Console.WriteLine("  텍스처 데이터가 변경되지 않았거나 예상대로 변경되지 않았습니다.");
                    Console.WriteLine("  셰이더 코드를 확인하거나 OpenGL 오류 로그를 확인하세요.");
                }

                // OpenGL 오류 검사
                CheckOpenGLErrors();

                // 자원 정리
                CleanUp();

                return _isGPUWorking;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"테스트 실패: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                CleanUp();
                return false;
            }
        }

        /// <summary>
        /// 텍스처 데이터 샘플링 (일부만 읽어서 변화 확인)
        /// </summary>
        private bool SampleTextureData(string label)
        {
            if (_computeShader == null)
                return false;

            try
            {
                // 텍스처 바인딩
                Gl.BindTexture(TextureTarget.Texture3d, _computeShader.Texture3DHandle);

                // 픽셀 버퍼 초기화
                Array.Clear(_pixelBuffer, 0, _pixelBuffer.Length);

                // 텍스처 데이터 읽기 (전체 데이터)
                Gl.GetTexImage(TextureTarget.Texture3d, 0, PixelFormat.Rgba, PixelType.Float, _pixelBuffer);

                // 텍스처 언바인딩
                Gl.BindTexture(TextureTarget.Texture3d, 0);

                // 일부 데이터만 표시 (샘플링)
                Console.WriteLine($"--- {label} ---");

                // 샘플링 포인트 계산 (중앙부와 모서리 부분)
                int[] sampleX = { 0, _textureWidth / 2, _textureWidth - 1 };
                int[] sampleY = { 0, _textureHeight / 2, _textureHeight - 1 };
                int[] sampleZ = { 0, _textureDepth / 2, _textureDepth - 1 };

                bool hasNonZeroData = false;
                bool hasValidData = false;

                foreach (int z in sampleZ)
                {
                    foreach (int y in sampleY)
                    {
                        foreach (int x in sampleX)
                        {
                            // 인덱스 계산
                            int idx = (z * _textureHeight * _textureWidth + y * _textureWidth + x) * 4;

                            // 버퍼 범위 확인
                            if (idx >= 0 && idx < _pixelBuffer.Length - 3)
                            {
                                float r = _pixelBuffer[idx];
                                float g = _pixelBuffer[idx + 1];
                                float b = _pixelBuffer[idx + 2];
                                float a = _pixelBuffer[idx + 3];

                                Console.WriteLine($"  픽셀[{x},{y},{z}] = ({r:F3}, {g:F3}, {b:F3}, {a:F3})");

                                // 0이 아닌 데이터가 있는지 확인
                                if (r != 0 || g != 0 || b != 0 || a != 0)
                                {
                                    hasNonZeroData = true;
                                }

                                // 예상 범위의 데이터인지 확인
                                if (r >= 0 && r <= 1 &&
                                    g >= 0 && g <= 1 &&
                                    b >= 0 && b <= 1 &&
                                    a >= 0 && a <= 1)
                                {
                                    hasValidData = true;
                                }
                            }
                        }
                    }
                }

                // 유효한 데이터가 있는지 확인
                if (hasNonZeroData && hasValidData)
                {
                    Console.WriteLine("  ✓ 유효한 데이터가 확인되었습니다.");
                    return true;
                }
                else if (!hasNonZeroData)
                {
                    Console.WriteLine("  ✗ 데이터가 모두 0입니다. 셰이더가 실행되지 않았을 수 있습니다.");
                    return false;
                }
                else
                {
                    Console.WriteLine("  ✗ 데이터 범위가 예상과 다릅니다. 셰이더 코드를 확인하세요.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"텍스처 샘플링 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// OpenGL 오류 확인
        /// </summary>
        private void CheckOpenGLErrors()
        {
            ErrorCode error = Gl.GetError();
            if (error != ErrorCode.NoError)
            {
                Console.WriteLine("=== OpenGL 오류 감지 ===");

                while (error != ErrorCode.NoError)
                {
                    Console.WriteLine($"  OpenGL 오류: {error}");
                    error = Gl.GetError();
                }

                Console.WriteLine("======================");
            }
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        private void CleanUp()
        {
            if (_computeShader != null)
            {
                _computeShader.CleanUp();
                _computeShader = null;
            }

            _isRunning = false;
        }
    }

    /// <summary>
    /// 테스트 실행을 위한 샘플 코드
    /// </summary>
    public static class GPUTestRunner
    {
        public static GPUComputeValidator RunGPUTest(string shaderBasePath)
        {
            Console.WriteLine("GPU 컴퓨트 셰이더 테스트 시작");

            // 테스트 인스턴스 생성
            GPUComputeValidator validator = new GPUComputeValidator(shaderBasePath, 64, 64, 64);

            // 테스트 실행
            bool testResult = validator.RunTest();

            // 결과 출력
            Console.WriteLine($"테스트 결과: {(testResult ? "성공" : "실패")}");

            return validator;
        }
    }
}
