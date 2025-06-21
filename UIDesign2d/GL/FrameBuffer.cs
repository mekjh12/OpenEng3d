using OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Ui2d
{
    public class FrameBuffer
    {
        private uint _fbo;
        private uint _rbo;
        private uint _texture;
        private uint _depth;
        private uint _stencil;

        protected int _width;
        protected int _height;

        public int Width => _width;

        public int Height => _height;

        public uint DepthMap => _depth;

        public uint FrameBufferID => _fbo;

        public uint ColorTextureID => _texture;

        public uint DepthTextureID => _depth;

        public uint TextureID => _texture;

        public FrameBuffer(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void Clear(ClearBufferMask mask)
        {
            Gl.Clear(mask);
        }

        public void Viewport(int width, int height)
        {
            Gl.Viewport(0, 0, width, height);
        }

        public void Bind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        }

        public void Unbind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void CreateCubeMapDepthBuffer()
        {
            if (_width <= 0 || _height <= 0)
            {
                throw new Exception("_width=0, _height=0 Error!");
            }

            // // create the fbo
            // -------------------------
            _fbo = Gl.GenFramebuffer();

            // create a color attachment texture
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.TextureCubeMap, _texture);

            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, Gl.NEAREST);

            for (int i = 0; i < 6; i++)
            {
                Gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0,
                    InternalFormat.DepthComponent32f,
               _width, _height,
               0, PixelFormat.DepthComponent, PixelType.UnsignedByte, IntPtr.Zero);
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                _texture, 0);
            Gl.DrawBuffer(DrawBufferMode.None);
            //Gl.ReadBuffer( ReadBufferMode.);

            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete)
            {
                //Debug.WriteLine($"Frame CubeMap Depth Buffer(ID={_fbo}) is completed.");
            }
            else
            {
                //Debug.WriteLine("Frame CubeMap Depth Buffer is errored!");
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.BindTexture(TextureTarget.TextureCubeMap, 0);
        }

        public void CreateDepthArrayBuffer(int depthMapResolution, int depth)
        {
            if (_width <= 0 || _height <= 0)
            {
                throw new Exception("_width=0, _height=0 Error!");
            }

            // framebuffer configuration
            // -------------------------
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            // create a color attachment texture
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2dArray, _texture);
            Gl.TexImage3D(
                TextureTarget.Texture2dArray,
                0,
                InternalFormat.DepthComponent32f,
                depthMapResolution,
                depthMapResolution,
                depth,
                0,
                PixelFormat.DepthComponent,
                PixelType.Float,
                IntPtr.Zero);

            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_BORDER);
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_BORDER);

            // prevent to over sampling.
            var borderColor = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
            Gl.TexParameter(TextureTarget.Texture2dArray, TextureParameterName.TextureBorderColor, borderColor);
            Gl.BindTexture(TextureTarget.Texture2dArray, 0);

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, _texture, 0);
            Gl.DrawBuffer(DrawBufferMode.None);
            //Gl.ReadBuffer( ReadBufferMode.);

            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete)
            {
                //Debug.WriteLine($"Frame Depth Array Buffer(ID={_fbo}) is completed.");
            }
            else
            {
                //Debug.WriteLine("Frame Depth Array Buffer is errored!");
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void CreateBuffer()
        {
            // framebuffer configuration
            // -------------------------
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            // create a color attachment texture
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _texture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb, _width, _height, 0, PixelFormat.Rgba,
                 PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _texture, 0);

            // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)
            _rbo = Gl.GenRenderbuffer();
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
            Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, _width, _height);
            Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete)
            {
                Debug.WriteLine($"Framebuffer(ID={_fbo}) is completed.");
            }
            else
            {
                Debug.WriteLine("Framebuffer is errored!");
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void CreateColorBuffer()
        {
            if (_width <= 0 || _height <= 0)
                throw new Exception($"width={_width}, height={_height} 중 하나는 0일 수 없습니다.");

            // framebuffer configuration
            // -------------------------
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);

            // create a color attachment texture
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _texture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _texture, 0);

            // Destination depth map creation
            _depth = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _depth);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent, _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, _depth, 0);

            // Destination framebuffer creation (이것 누락하지 않도록!)
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _texture, 0);
            Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, _depth, 0);

            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete)
            {
                Debug.WriteLine($"Frame Color Buffer(ID={_fbo}) is completed.");
            }
            else
            {
                Debug.WriteLine($"Frame Color Buffer is errored!");
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void CreateDepthBuffer()
        {
            if (_width <= 0 || _height <= 0)
                throw new Exception($"width={_width}, height={_height} 중 하나는 0일 수 없습니다.");

            // framebuffer를 생성하고 바인딩한다.
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            // 깊이버퍼를 Texture2d로 생성한다.
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _texture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent,
                _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, _texture, 0);

            // 깊이버퍼 텍스처 해제
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 프레임버퍼 해제
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 정상생성유무 확인
            CheckFrameBuffer("DepthAttachment");
        }

        /// <summary>
        /// Depth24Stencil8 버퍼를 생성한다.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void CreateDepthStencilBuffer()
        {
            if (_width <= 0 || _height <= 0)
                throw new Exception($"width={_width}, height={_height} 중 하나는 0일 수 없습니다.");

            // 프레임버퍼를 생성하고 바인딩한다.
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            //  depth and stencil 렌더 버퍼를 생성한다.
            // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)
            _rbo = Gl.GenRenderbuffer();
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
            Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, _width, _height);
            Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                RenderbufferTarget.Renderbuffer, _rbo);

            // 렌더버퍼 해제
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            // 프레임버퍼 해제
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 정상생성유무 확인
            CheckFrameBuffer("Depth24Stencil8");
        }

        /// <summary>
        /// FrameBuffer가 정상적으로 생성되었는지 검사한다.
        /// </summary>
        /// <param name="bufferName"></param>
        public void CheckFrameBuffer(string bufferName)
        {
            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete)
            {
                Console.WriteLine($"Frame {bufferName} Buffer(ID={_fbo}) is completed.");
                Debug.WriteLine($"Frame {bufferName} Buffer(ID={_fbo}) is completed.");
            }
            else
            {
                Debug.WriteLine($"Frame {bufferName} Buffer is errored!");
            }
        }

        /// <summary>
        /// FrameBuffer를 지운다. 버퍼에 소속된 하위 버퍼들도 모두 지운다.
        /// </summary>
        public void Clear()
        {
            Gl.DeleteTextures(new uint[] { _texture });
            Gl.DeleteFramebuffers(new uint[] { _fbo });
            Gl.DeleteRenderbuffers(new uint[] { _rbo });
        }

        public void Save(string filename)
        {
            Bitmap bitmap = new Bitmap(_width, _height);

            uint[] depthBufferID = new uint[1];
            Gl.GenBuffers(depthBufferID);
            Gl.BindBuffer(BufferTarget.PixelPackBuffer, depthBufferID[0]);
            Gl.BufferData(BufferTarget.PixelPackBuffer, (uint)(_width * _height * sizeof(float)), IntPtr.Zero, BufferUsage.StreamRead);
            Gl.ReadPixels(0, 0, _width, _height, OpenGL.PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            IntPtr depthBufferPTR = Gl.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);

            // depth buffer은 가까이는 0이고 멀리는 1이다. 기본값은 1이다.
            float[] myPixels = new float[_width * _height];
            Marshal.Copy(depthBufferPTR, myPixels, 0, _width * _height);

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    float z = myPixels[i * _width + j];
                    float value = (float)Math.Log(255 * z);
                    int c = (int)(value.Clamp(0, 255));
                    bitmap.SetPixel(j, i, Color.FromArgb(c, c, c)); // 
                }
            }

            bitmap.Save(filename);
        }
    }
}
