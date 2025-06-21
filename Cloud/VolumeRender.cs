using Camera3d;
using Cloud;
using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using System.IO;


namespace Cloud
{
    public class VolumeRender
    {
        int _width = 0;
        int _height = 0;
        uint _textureID = 0;

        public VolumeRender()
        {
           
        }

        public void Init(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void LoadNoise3d()
        {
            BindTexture3d(128, 128, 128, FractalBrownianNoise3D.Generate(128));
        }

        public void LoadSphere()
        {
            int size = 128;
            byte[] data = new byte[size * size * size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    for (int k = 0; k < size; k++)
                    {
                        int idx = i * size * size + j * size + k;
                        float dd = (i - 64) * (i - 64) + (j - 64) * (j - 64) + (k - 64) * (k - 64);
                        float dist = (float)Math.Sqrt(dd);
                        data[idx] = (byte)(dd < 64*64 ? 1 : 0);
                    }
                }
            }

            BindTexture3d(size, size, size, data);
        }

        public void LoadRaw(string filename)
        {
            byte[] rawData = null;
            FileStream fs = null;
            try
            {
                // 파일 열기
                using (fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    rawData = new byte[fs.Length];
                    fs.Read(rawData, 0, rawData.Length); // 파일 전체를 바이트 배열로 읽기
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine("파일을 읽는 중 오류 발생: " + ex.Message);
            }
            fs.Close();

            int w = rawData[0];
            int h = rawData[2];
            int d = rawData[4];

            byte[] data = new byte[w * h * d];

            for (int i = 0; i < w * h * d; i++)
            {
                int idx = 7 + i * 2;
                data[i] = rawData[idx];
            }

            BindTexture3d(w, h, d, data);
        }

        private void BindTexture3d(int w, int h, int d, byte[] data)
        {
            _textureID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture3d, _textureID);

            // 텍스처 필터링 및 래핑 설정
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // 바이트 배열로 텍스처 데이터 채우기
            Gl.TexImage3D(TextureTarget.Texture3d, 0, InternalFormat.R8, w, h, d, 0, PixelFormat.Red, PixelType.UnsignedByte, data);

            // 텍스처 바인딩 해제
            Gl.BindTexture(TextureTarget.Texture3d, 0);
        }

        public void Update(int width, int height, float duration)
        {

        }

        public void Render(VolumeRenderShader shader, uint vao, int count, Camera camera, float stepLength)
        {
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcColor, BlendingFactor.OneMinusDstColor);

            shader.Bind();

            /*
            shader.LoadTexture3d(_textureID);

            shader.LoadFocalLength(camera.FocalLength);
            shader.LoadViewportSize(new Vertex2f(_width, _height));
            shader.LoadRayOrgin(camera.Position);
            shader.LoadAspectRatio(camera.AspectRatio);
            shader.LoadGamma(0.7f);
            shader.LoadStepLength(stepLength);

            shader.LoadProjMatrix(camera.ProjectiveMatrix);
            shader.LoadViewMatrix(camera.ViewMatrix);
            shader.LoadModelMatrix(Matrix4x4f.Identity);
            */

            Gl.BindVertexArray(vao);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, count);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            shader.Unbind();
        }
    }
}
