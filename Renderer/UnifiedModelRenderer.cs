using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Renderer
{
    /// <summary>
    /// 통합 모델 렌더러
    /// </summary>
    public class UnifiedModelRenderer
    {
        private UnifiedModel _model;
        private uint _shaderProgram;

        public UnifiedModelRenderer(UnifiedModel model, uint shaderProgram)
        {
            _model = model;
            _shaderProgram = shaderProgram;

            // Texture2DArray 생성
            CreateTextureArray();
        }

        /// <summary>
        /// 2의 제곱수로 올림
        /// </summary>
        private int RoundUpToPowerOfTwo(int value)
        {
            if (value <= 0) return 1;

            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;

            return value;
        }

        /// <summary>
        /// 모든 텍스처를 Texture2DArray로 생성
        /// </summary>
        private void CreateTextureArray()
        {
            if (_model.Textures.Count == 0)
            {
                Console.WriteLine("텍스처가 없습니다.");
                return;
            }

            // 1단계: 모든 텍스처 크기 확인 및 통일할 크기 결정
            int maxWidth = 0;
            int maxHeight = 0;

            foreach (var tex in _model.Textures)
            {
                try
                {
                    using (Bitmap bmp = new Bitmap(tex.FileName))
                    {
                        if (bmp.Width > maxWidth) maxWidth = bmp.Width;
                        if (bmp.Height > maxHeight) maxHeight = bmp.Height;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"텍스처 크기 확인 실패 {tex.FileName}: {ex.Message}");
                }
            }

            // 2의 제곱수로 올림 (OpenGL 효율성)
            int targetWidth = RoundUpToPowerOfTwo(maxWidth);
            int targetHeight = RoundUpToPowerOfTwo(maxHeight);

            // 최대 크기 제한 (선택사항)
            targetWidth = Math.Min(targetWidth, 2048);
            targetHeight = Math.Min(targetHeight, 2048);

            Console.WriteLine($"Texture2DArray 생성: {_model.Textures.Count}개 레이어, 크기={targetWidth}x{targetHeight}");

            // 2단계: Texture2DArray 생성
            uint textureArrayID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2dArray, textureArrayID);

            // 텍스처 스토리지 할당
            Gl.TexStorage3D(
                TextureTarget.Texture2dArray,
                1,                              // mipmap levels
                InternalFormat.Rgba8,
                targetWidth,
                targetHeight,
                _model.Textures.Count           // 레이어 수
            );

            // 3단계: 각 텍스처를 레이어에 업로드
            for (int i = 0; i < _model.Textures.Count; i++)
            {
                Texture tex = _model.Textures[i];

                // 텍스처 이미지 로드 (리사이즈)
                byte[] imageData = LoadAndResizeTexture(tex.FileName, targetWidth, targetHeight);

                if (imageData != null)
                {
                    // 특정 레이어에 텍스처 업로드
                    Gl.TexSubImage3D(
                        TextureTarget.Texture2dArray,
                        0,                      // mipmap level
                        0, 0, i,               // x, y, z offset (z = 레이어 인덱스)
                        targetWidth,
                        targetHeight,
                        1,                      // depth (레이어 1개)
                         OpenGL.PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        imageData
                    );

                    Console.WriteLine($"텍스처 업로드: 레이어 {i} - {tex.FileName}");
                }
            }

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            Gl.BindTexture(TextureTarget.Texture2dArray, 0);

            _model.TextureArrayID = textureArrayID;

            Console.WriteLine($"Texture2DArray 생성 완료: ID={textureArrayID}");
        }

        /// <summary>
        /// 텍스처 파일 로드 및 리사이즈
        /// </summary>
        private byte[] LoadAndResizeTexture(string filepath, int targetWidth, int targetHeight)
        {
            try
            {
                using (Bitmap original = new Bitmap(filepath))
                {
                    // 리사이즈
                    using (Bitmap resized = new Bitmap(targetWidth, targetHeight))
                    {
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(original, 0, 0, targetWidth, targetHeight);
                        }

                        // RGBA 바이트 배열로 변환
                        BitmapData data = resized.LockBits(
                            new Rectangle(0, 0, targetWidth, targetHeight),
                            ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppArgb
                        );

                        byte[] imageData = new byte[targetWidth * targetHeight * 4];
                        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, imageData, 0, imageData.Length);

                        resized.UnlockBits(data);

                        // BGRA -> RGBA 변환
                        for (int i = 0; i < imageData.Length; i += 4)
                        {
                            byte temp = imageData[i];
                            imageData[i] = imageData[i + 2];
                            imageData[i + 2] = temp;
                        }

                        return imageData;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"텍스처 로드 실패 {filepath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 모델 렌더링
        /// </summary>
        public void Render(UnlitShader shader, Camera camera)
        {
            // 셰이더 활성화
            Gl.UseProgram(shader.ProgramID);

            // Texture2DArray 바인딩
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2dArray, _model.TextureArrayID);

            // 셰이더에 텍스처 유니폼 설정
            int texLocation = Gl.GetUniformLocation(_shaderProgram, "texArray");
            Gl.Uniform1(texLocation, 0);  // Texture unit 0

            shader.LoadMVPMatrix(camera.VPMatrix);

            // VAO 바인딩
            Gl.BindVertexArray(_model.VaoID);

            // 한 번의 드로우콜로 전체 렌더링
            Gl.DrawElements(
                PrimitiveType.Triangles,
                _model.IndexCount,
                DrawElementsType.UnsignedInt,
                IntPtr.Zero
            );

            // 언바인딩
            Gl.BindVertexArray(0);
            Gl.BindTexture(TextureTarget.Texture2dArray, 0);
            Gl.UseProgram(0);
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            if (_model.TextureArrayID != 0)
            {
                Gl.DeleteTextures(_model.TextureArrayID);
            }

            if (_model.VaoID != 0)
            {
                Gl.DeleteVertexArrays(_model.VaoID);
            }
        }
    }

}