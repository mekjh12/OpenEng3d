using OpenGL;
using System;

namespace Model3d
{
    /// <summary>
    /// OpenGL 렌더 타겟용 2D 텍스처 클래스
    /// </summary>
    /// <remarks>
    /// 이 클래스는 OpenGL에서 오프스크린 렌더링을 위한 렌더 타겟을 생성하고 관리합니다.
    /// 
    /// 사용법:
    /// 1. 인스턴스 생성:
    ///    RenderTarget2D renderTarget = new RenderTarget2D(width, height, generateMips, format, depthFormat);
    /// 
    /// 2. 렌더 타겟에 렌더링:
    ///    Gl.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.FrameBuffer);
    ///    // 여기서 원하는 렌더링 작업 수행
    ///    Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    /// 
    /// 3. 렌더 타겟의 텍스처 사용:
    ///    Gl.BindTexture(TextureTarget.Texture2d, renderTarget.TextureHandle);
    ///    // 텍스처를 사용하는 렌더링 작업 수행
    /// 
    /// 4. 렌더 타겟의 데이터 읽기:
    ///    byte[] pixelData = new byte[width * height * 4];
    ///    renderTarget.GetData(pixelData);
    /// 
    /// 5. 리소스 해제:
    ///    renderTarget.Dispose();
    /// 
    /// 주의: 이 클래스는 IDisposable을 구현하므로, using 문을 사용하거나 
    /// 명시적으로 Dispose()를 호출하여 리소스를 해제해야 합니다.
    /// </remarks>
    public class RenderTarget2D : IDisposable
    {
        private uint _frameBuffer;      // 프레임버퍼 객체(FBO)
        private uint _textureHandle;    // 컬러 텍스처 핸들
        private uint _depthHandle;      // 깊이 버퍼 핸들
        private bool _hasDepth;         // 깊이 버퍼 사용 여부

        public int Width { get; private set; }
        public int Height { get; private set; }
        public SurfaceFormat Format { get; private set; }
        public DepthFormat DepthFormat { get; private set; }

        public uint FrameBuffer
        {
            get => _frameBuffer; 
            set => _frameBuffer = value;
        }

        public uint TextureHandle
        {
            get => _textureHandle; 
            set => _textureHandle = value;
        }

        public uint DepthHandle
        {
            get => _depthHandle;
            set => _depthHandle = value;
        }

        /// <summary>
        /// 렌더 타겟 초기화
        /// </summary>
        public RenderTarget2D(int width, int height, bool generateMips,
            SurfaceFormat format, DepthFormat depthFormat)
        {
            Width = width;
            Height = height;
            Format = format;
            DepthFormat = depthFormat;
            _hasDepth = depthFormat != DepthFormat.None;

            CreateRenderTarget(generateMips);
        }

        /// <summary>
        /// OpenGL 렌더 타겟 생성
        /// </summary>
        private void CreateRenderTarget(bool generateMips)
        {
            // 프레임버퍼 생성 및 바인딩
            FrameBuffer = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);

            // 컬러 텍스처 생성 및 설정
            TextureHandle = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, TextureHandle);

            InternalFormat internalFormat = GetInternalFormat();
            PixelFormat pixelFormat = GetPixelFormat();
            PixelType pixelType = GetPixelType();

            Gl.TexImage2D(TextureTarget.Texture2d, 0, internalFormat,
                Width, Height, 0, pixelFormat, pixelType, IntPtr.Zero);

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter,
                (int)(generateMips ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.Linear));
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge);

            if (generateMips)
            {
                Gl.GenerateMipmap(TextureTarget.Texture2d);
            }

            // 프레임버퍼에 텍스처 연결
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2d, TextureHandle, 0);

            // 깊이 버퍼 생성 (필요한 경우)
            if (_hasDepth)
            {
                _depthHandle = Gl.GenRenderbuffer();
                Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthHandle);

                InternalFormat depthStorage = GetDepthStorage();
                Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, depthStorage, Width, Height);

                Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer, _depthHandle);
            }

            // 프레임버퍼 완성도 체크
            FramebufferStatus status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                throw new Exception($"Framebuffer creation failed: {status}");
            }

            // 바인딩 해제
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
            if (_hasDepth)
            {
                Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            }
        }

        /// <summary>
        /// 렌더 타겟의 픽셀 데이터를 읽어옴
        /// </summary>
        public unsafe void GetData<T>(T[] data) where T : unmanaged
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);

            PixelFormat format = GetPixelFormat();
            PixelType type = GetPixelType();

            fixed (void* ptr = data)
            {
                Gl.ReadPixels(0, 0, Width, Height, format, type, (IntPtr)ptr);
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // 내부 포맷 반환
        private InternalFormat GetInternalFormat()
        {
            switch (Format)
            {
                case SurfaceFormat.Color:
                    return InternalFormat.Rgba8;
                case SurfaceFormat.Rgba32:
                    return InternalFormat.Rgba32f;
                default:
                    throw new ArgumentException($"Unsupported surface format: {Format}");
            }
        }

        // 픽셀 포맷 반환
        private PixelFormat GetPixelFormat()
        {
            switch (Format)
            {
                case SurfaceFormat.Color:
                case SurfaceFormat.Rgba32:
                    return PixelFormat.Rgba;
                default:
                    throw new ArgumentException($"Unsupported surface format: {Format}");
            }
        }

        // 픽셀 타입 반환
        private PixelType GetPixelType()
        {
            switch (Format)
            {
                case SurfaceFormat.Color:
                    return PixelType.UnsignedByte;
                case SurfaceFormat.Rgba32:
                    return PixelType.Float;
                default:
                    throw new ArgumentException($"Unsupported surface format: {Format}");
            }
        }

        // 깊이 버퍼 저장 포맷 반환
        private InternalFormat GetDepthStorage()
        {
            switch (DepthFormat)
            {
                case DepthFormat.Depth24:
                    return InternalFormat.DepthComponent24;
                case DepthFormat.Depth32:
                    return InternalFormat.DepthComponent32;
                default:
                    throw new ArgumentException($"Unsupported depth format: {DepthFormat}");
            }
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            if (FrameBuffer != 0)
            {
                Gl.DeleteFramebuffers(new uint[] { FrameBuffer });
                FrameBuffer = 0;
            }

            if (TextureHandle != 0)
            {
                Gl.DeleteTextures(new uint[] { TextureHandle });
                TextureHandle = 0;
            }

            if (_depthHandle != 0)
            {
                Gl.DeleteRenderbuffers(new uint[] { _depthHandle });
                _depthHandle = 0;
            }
        }
    }

    /// <summary>
    /// 표면 포맷 열거형
    /// </summary>
    public enum SurfaceFormat
    {
        Color,
        Rgba32,
        // 필요한 포맷 추가
    }

    /// <summary>
    /// 깊이 버퍼 포맷 열거형
    /// </summary>
    public enum DepthFormat
    {
        None,
        Depth16,
        Depth24,
        Depth32,
        // 필요한 포맷 추가
    }
}