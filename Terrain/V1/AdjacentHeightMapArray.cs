using OpenGL;

namespace Terrain
{
    public class AdjacentHeightMapArray
    {
        public uint TextureArrayId { get; private set; }
        private const int LAYER_COUNT = 8;

        /// <summary>
        /// 8개 인접 높이맵을 하나의 Texture2DArray로 생성합니다.
        /// 모든 높이맵은 동일한 해상도여야 합니다.
        /// </summary>
        public void Create(int width, int height)
        {
            uint texId = Gl.GenTexture();
            TextureArrayId = texId;

            Gl.BindTexture(TextureTarget.Texture2dArray, TextureArrayId);

            // 8레이어짜리 배열 할당 (R16F 또는 R32F)
            Gl.TexStorage3D(
                TextureTarget.Texture2dArray,
                1,                          // mipmap 레벨
                InternalFormat.R16f,        // 높이맵이니까 단일 채널이면 충분
                width, height,
                LAYER_COUNT                 // 8개 레이어
            );

            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            Gl.BindTexture(TextureTarget.Texture2dArray, 0);
        }

        /// <summary>
        /// 특정 레이어(방향)의 높이맵 데이터를 업데이트합니다.
        /// layerIndex: 0=R, 1=RU, 2=U, 3=LU, 4=L, 5=LD, 6=D, 7=RD
        /// </summary>
        public void UpdateLayer(int layerIndex, int width, int height, float[] heightData)
        {
            Gl.BindTexture(TextureTarget.Texture2dArray, TextureArrayId);

            Gl.TexSubImage3D(
                TextureTarget.Texture2dArray,
                0,                  // mipmap level
                0, 0, layerIndex,   // xoffset, yoffset, 레이어 인덱스
                width, height, 1,   // 너비, 높이, 레이어 수(1개씩)
                PixelFormat.Red,
                PixelType.Float,
                heightData
            );

            Gl.BindTexture(TextureTarget.Texture2dArray, 0);
        }

        /// <summary>
        /// 기존 2D 텍스처에서 픽셀을 읽어서 레이어에 복사합니다.
        /// FBO를 통해 GPU→GPU 복사하는 방식.
        /// </summary>
        public void CopyFromTexture2D(uint sourceTexId, int layerIndex, int width, int height)
        {
            // 임시 FBO로 소스 텍스처를 읽어서 배열에 복사
            uint fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fbo);
            Gl.FramebufferTexture2D(
                FramebufferTarget.ReadFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2d,
                sourceTexId, 0
            );

            // 배열 텍스처의 해당 레이어에 복사
            Gl.BindTexture(TextureTarget.Texture2dArray, TextureArrayId);
            Gl.CopyTexSubImage3D(
                TextureTarget.Texture2dArray,
                0,
                0, 0, layerIndex,  // 대상 레이어
                0, 0,
                width, height
            );

            Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            Gl.DeleteFramebuffers(fbo);
        }

        public void Dispose()
        {
            if (TextureArrayId != 0)
            {
                uint id = TextureArrayId;
                Gl.DeleteTextures(id);
                TextureArrayId = 0;
            }
        }
    }
}
