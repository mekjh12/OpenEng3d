using Common;
using OpenGL;
using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ZetaExt;

namespace Shader
{
    /// <summary>
    /// 3D 노이즈 생성을 위한 컴퓨트 셰이더 클래스
    /// </summary>
    public class CloudComputeShader : ShaderProgram<CloudComputeShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            time,
            resolution,
            numCellsPerAxis,
            Count
        }

        private uint _numCellsPerAxis = 10; // 조정 가능한 셀 개수

        const string COMPUTE_FILE = @"\Shader\CloudShader\noise3d.comp";
        const int WORK_GROUP_SIZE = 8;

        // 3D 텍스처 관련 변수
        private uint _texture3DHandle;
        private int _textureWidth;
        private int _textureHeight;
        private int _textureDepth;
        private float _time = 0.0f;

        // 프로퍼티
        public uint Texture3DHandle => _texture3DHandle;
        public int TextureWidth => _textureWidth;
        public int TextureHeight => _textureHeight;
        public int TextureDepth => _textureDepth;

        /// <summary>
        /// 3D 노이즈 컴퓨트 셰이더 생성자
        /// </summary>
        /// <param name="computeShaderPath">셰이더 파일 경로의 기본 디렉토리</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        /// <param name="depth">텍스처 깊이</param>
        public CloudComputeShader(string computeShaderPath, int width = 256, int height = 256, int depth = 256) : base()
        {
            _name = GetType().Name;
            _textureWidth = width;
            _textureHeight = height;
            _textureDepth = depth;

            // 셰이더 파일 경로 설정
            ComputeFileName = computeShaderPath + COMPUTE_FILE;

            // 텍스처 초기화
            Initialize3DTexture();

            // 셰이더 컴파일 및 초기화
            InitCompileShader();

            // 초기 실행 및 결과 확인
            Run();
            CheckTextureData();
        }

        /// <summary>
        /// 컴퓨트 셰이더 실행 메서드
        /// </summary>
        public void Run()
        {
            // 셰이더 프로그램 사용 시작
            Bind();
            GetError("after UseProgram");

            // 이미지 바인딩 (layered=true 설정 중요)
            Gl.BindImageTexture(0, _texture3DHandle, 0, true, 0, BufferAccess.WriteOnly, InternalFormat.Rgba32f);
            GetError("after BindImageTexture");

            // 유니폼 변수 설정
            LoadUniform(UNIFORM_NAME.time, _time);
            LoadUniform(UNIFORM_NAME.resolution, new Vertex3f(_textureWidth, _textureHeight, _textureDepth));
            LoadUniform(UNIFORM_NAME.numCellsPerAxis, (float)_numCellsPerAxis);

            // 워크 그룹 계산 및 디스패치
            int groupsX = (int)Math.Ceiling(_textureWidth / (float)WORK_GROUP_SIZE);
            int groupsY = (int)Math.Ceiling(_textureHeight / (float)WORK_GROUP_SIZE);
            int groupsZ = (int)Math.Ceiling(_textureDepth / (float)WORK_GROUP_SIZE);

            Console.WriteLine($"디스패치 그룹 크기: ({groupsX}, {groupsY}, {groupsZ})");

            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, (uint)groupsZ);
            GetError("after DispatchCompute");

            // 메모리 배리어로 쓰기 작업 완료 대기
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            GetError("after MemoryBarrier");

            // 셰이더 프로그램 사용 종료
            Unbind();
        }

        /// <summary>
        /// 3D 텍스처 초기화 메서드
        /// </summary>
        private void Initialize3DTexture()
        {
            // 텍스처 생성
            _texture3DHandle = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture3d, _texture3DHandle);

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapR, Gl.CLAMP_TO_EDGE);

            // 텍스처 데이터 할당 (초기 데이터 없이)
            Gl.TexImage3D(TextureTarget.Texture3d, 0, InternalFormat.Rgba32f,
                _textureWidth, _textureHeight, _textureDepth, 0,
                OpenGL.PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            // 초기값으로 텍스처 클리어 - 4개 컴포넌트로 변경
            float[] clearColor = new float[] { 0.5f, 0.0f, 0.0f, 1.0f }; // r, g, b, a
            Gl.ClearTexImage(_texture3DHandle, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, clearColor);
            GetError("after ClearTexImage");

            // 바인딩 해제
            Gl.BindTexture(TextureTarget.Texture3d, 0);
        }

        /// <summary>
        /// 텍스처 데이터를 CPU로 가져와 확인하는 메서드
        /// </summary>
        public void CheckTextureData()
        {
            Console.WriteLine("텍스처 데이터 확인 중...");

            // 데이터를 저장할 배열 생성 (Rgba32f 텍스처이므로 float 배열 크기를 4배로)
            float[] data = new float[_textureWidth * _textureHeight * _textureDepth * 4];

            // 텍스처 바인딩 및 데이터 읽기
            Gl.BindTexture(TextureTarget.Texture3d, _texture3DHandle);
            Gl.GetTexImage(TextureTarget.Texture3d, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, data);
            GetError("after GetTexImage");

            Gl.BindTexture(TextureTarget.Texture3d, 0);

            // z=0 평면의 일부 데이터 출력
            Console.WriteLine("샘플 데이터:");
            Console.WriteLine("z=0 평면:");
            PrintSamplePlane(data, 0);

            // z=1 평면의 일부 데이터 출력
            Console.WriteLine("\nz=1 평면:");
            PrintSamplePlane(data, 1);

            // 통계 정보 계산 및 출력
            CalculateStatistics(data);
        }

        /// <summary>
        /// 특정 z 평면의 샘플 데이터 출력 헬퍼 메서드
        /// </summary>
        private void PrintSamplePlane(float[] data, int z)
        {
            for (int y = 0; y < Math.Min(3, _textureHeight); y++)
            {
                for (int x = 0; x < Math.Min(3, _textureWidth); x++)
                {
                    int baseIndex = (z * _textureHeight * _textureWidth + y * _textureWidth + x) * 4;
                    Console.WriteLine($"({x}, {y}, {z}): R={data[baseIndex]}, G={data[baseIndex + 1]}, B={data[baseIndex + 2]}, A={data[baseIndex + 3]}");
                }
            }
        }

        /// <summary>
        /// 3D 텍스처의 각 슬라이스(층)를 PNG 파일로 저장합니다.
        /// </summary>
        /// <param name="outputFolderPath">저장할 폴더 경로</param>
        /// <param name="maxSlices">저장할 최대 슬라이스 수 (-1이면 모든 슬라이스)</param>
        /// <param name="separateChannels">채널별로 분리해서 저장할지 여부</param>
        public void SaveTextureToPngSlices(string outputFolderPath, int maxSlices = -1, bool separateChannels = false)
        {
            Console.WriteLine("3D 텍스처를 PNG 파일로 저장 중...");

            // 출력 폴더가 존재하는지 확인하고, 없으면 생성
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            // 채널별 분리 저장을 위한 서브폴더 생성
            if (separateChannels)
            {
                // Figure 4.8에 맞게 채널 이름 수정
                string[] channelFolders = { "PerlinWorleyFBM", "WorleyOctave1", "WorleyOctave2", "WorleyOctave3" };
                foreach (string folder in channelFolders)
                {
                    string channelPath = Path.Combine(outputFolderPath, folder);
                    if (!Directory.Exists(channelPath))
                    {
                        Directory.CreateDirectory(channelPath);
                    }
                }
            }

            // 데이터를 저장할 배열 생성 (변수명 오타 수정)
            float[] data = new float[_textureWidth * _textureHeight * _textureDepth * 4];

            // 텍스처 바인딩 및 데이터 읽기
            Gl.BindTexture(TextureTarget.Texture3d, _texture3DHandle);
            Gl.GetTexImage(TextureTarget.Texture3d, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, data);
            GetError("after GetTexImage");
            Gl.BindTexture(TextureTarget.Texture3d, 0);

            // 저장할 최대 슬라이스 수 설정 (변수명 오타 수정)
            int slicesToSave = maxSlices > 0 ? Math.Min(maxSlices, _textureDepth) : _textureDepth;
            slicesToSave = 1;

            // 각 z층에 대해 PNG 파일 생성
            for (int z = 0; z < slicesToSave; z++)
            {
                if (separateChannels)
                {
                    // 채널별로 분리해서 저장
                    SaveChannelsSeparately(data, z, outputFolderPath);
                }
                else
                {
                    // 기존 방식: RGBA 통합 저장
                    SaveCombinedRGBA(data, z, outputFolderPath);
                }

                if (z % 10 == 0 || z == slicesToSave - 1)
                {
                    Console.WriteLine($"슬라이스 {z + 1}/{slicesToSave} 저장 완료");
                }
            }

            Console.WriteLine($"모든 슬라이스가 {outputFolderPath} 폴더에 저장되었습니다.");
        }

        /// <summary>
        /// 채널별로 분리해서 저장하는 헬퍼 메서드 (Figure 4.8용으로 수정)
        /// </summary>
        private void SaveChannelsSeparately(float[] data, int z, string outputFolderPath)
        {
            // Figure 4.8에 맞게 채널 이름 수정
            //string[] channelNames = { "PerlinWorleyFBM", "WorleyOctave1", "WorleyOctave2", "WorleyOctave3" };
            string[] channelNames = { "", "", "", "" };

            for (int channel = 0; channel < 4; channel++)
            {
                using (Bitmap bitmap = new Bitmap(_textureWidth, _textureHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    BitmapData bmpData = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.WriteOnly,
                        bitmap.PixelFormat);

                    byte[] pixelBuffer = new byte[bmpData.Stride * bitmap.Height];

                    for (int y = 0; y < _textureHeight; y++)
                    {
                        for (int x = 0; x < _textureWidth; x++)
                        {
                            int texIndex = (z * _textureHeight * _textureWidth + y * _textureWidth + x) * 4;
                            int pixelIndex = y * bmpData.Stride + x * 4;

                            // 해당 채널의 값을 그레이스케일로 표시
                            byte value = (byte)(data[texIndex + channel].Clamp(0, 1) * 255);

                            // BGRA 순서로 저장 (그레이스케일)
                            pixelBuffer[pixelIndex] = value;     // B
                            pixelBuffer[pixelIndex + 1] = value; // G
                            pixelBuffer[pixelIndex + 2] = value; // R
                            pixelBuffer[pixelIndex + 3] = 255;   // A
                        }
                    }

                    Marshal.Copy(pixelBuffer, 0, bmpData.Scan0, pixelBuffer.Length);
                    bitmap.UnlockBits(bmpData);

                    // 채널별 폴더에 저장
                    string channelFolder = Path.Combine(outputFolderPath, channelNames[channel]);
                    string filePath = Path.Combine(channelFolder, $"{channel}_slice_z{z:D3}.png");
                    bitmap.Save(filePath, ImageFormat.Png);
                }
            }
        }

        /// <summary>
        /// RGBA 통합해서 저장하는 헬퍼 메서드
        /// </summary>
        private void SaveCombinedRGBA(float[] data, int z, string outputFolderPath)
        {
            using (Bitmap bitmap = new Bitmap(_textureWidth, _textureHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                byte[] pixelBuffer = new byte[bmpData.Stride * bitmap.Height];

                for (int y = 0; y < _textureHeight; y++)
                {
                    for (int x = 0; x < _textureWidth; x++)
                    {
                        int texIndex = (z * _textureHeight * _textureWidth + y * _textureWidth + x) * 4;
                        int pixelIndex = y * bmpData.Stride + x * 4;

                        // RGBA 값을 [0,1]에서 [0,255]로 변환
                        byte r = (byte)(data[texIndex].Clamp(0, 1) * 255);
                        byte g = (byte)(data[texIndex + 1].Clamp(0, 1) * 255);
                        byte b = (byte)(data[texIndex + 2].Clamp(0, 1) * 255);
                        byte a = (byte)(data[texIndex + 3].Clamp(0, 1) * 255);

                        // BGRA 순서로 픽셀 버퍼에 저장
                        pixelBuffer[pixelIndex] = b;
                        pixelBuffer[pixelIndex + 1] = g;
                        pixelBuffer[pixelIndex + 2] = r;
                        pixelBuffer[pixelIndex + 3] = a;
                    }
                }

                Marshal.Copy(pixelBuffer, 0, bmpData.Scan0, pixelBuffer.Length);
                bitmap.UnlockBits(bmpData);

                string filePath = Path.Combine(outputFolderPath, $"slice_z{z:D3}.png");
                bitmap.Save(filePath, ImageFormat.Png);
            }
        }

        /// <summary>
        /// 텍스처 데이터의 통계 정보 계산 및 출력 (R 채널만 고려)
        /// </summary>
        private void CalculateStatistics(float[] data)
        {
            // 최소/최대값 찾기 (R 채널만)
            float minValueR = float.MaxValue;
            float maxValueR = float.MinValue;
            float sumR = 0.0f;

            for (int i = 0; i < data.Length; i += 4) // R 채널은 4개 중 첫 번째
            {
                minValueR = Math.Min(minValueR, data[i]);
                maxValueR = Math.Max(maxValueR, data[i]);
                sumR += data[i];
            }

            float averageR = sumR / (data.Length / 4);
            Console.WriteLine($"\nR 채널 데이터 범위: 최소값={minValueR}, 최대값={maxValueR}");
            Console.WriteLine($"R 채널 평균값={averageR}");
        }

        /// <summary>
        /// OpenGL 오류 확인 메서드
        /// </summary>
        public void GetError(string param, uint computeShader = 0)
        {
            ErrorCode error = Gl.GetError();
            if (error != ErrorCode.NoError)
            {
                Console.WriteLine($"OpenGL Error {param}: {error}");

                if (computeShader != 0)
                {
                    int status;
                    Gl.GetShader(computeShader, ShaderParameterName.CompileStatus, out status);
                    if (status == 0)
                    {
                        StringBuilder infoLog = new StringBuilder(512);
                        Gl.GetShaderInfoLog(computeShader, 512, out int length, infoLog);
                        Console.WriteLine($"컴퓨트 셰이더 컴파일 에러: {infoLog}");
                    }
                }
            }
        }

        /// <summary>
        /// 컴퓨트 셰이더는 BindAttributes가 필요 없음
        /// </summary>
        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 구현 필요 없음
        }

        /// <summary>
        /// 모든 유니폼 위치 가져오기
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        /// <summary>
        /// 시간 업데이트 메서드
        /// </summary>
        public void UpdateTime(float deltaTime)
        {
            _time += deltaTime;



        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public override void CleanUp()
        {
            // 3D 텍스처 삭제
            if (_texture3DHandle != 0)
            {
                Gl.DeleteTextures(_texture3DHandle);
                _texture3DHandle = 0;
            }

            // 부모 클래스의 CleanUp 호출
            base.CleanUp();
        }
    }
}