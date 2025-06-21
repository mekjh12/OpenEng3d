using OpenGL;
using System;
using System.Runtime.InteropServices;

namespace Renderer
{
    /// <summary>
    /// ** 버퍼의 생성조건 4가지 **
    /// (1) 최소 한 개이상의 버퍼(색상, 깊이, 스텐실)을 가져야 한다.
    /// (2) 적어도 하나의 색상버퍼를 가져야 한다.
    /// (3) 첨부된 버퍼는 완벽해야 한다.(메모리가 예약되어야 함)
    /// (4) 모든 버퍼의 해상도는 같아야 한다.
    /// </summary>
    public class SimpFrameBuffer
    {
        [Flags]
        public enum BufferType { COLOR = 1, DEPTH = 2, DEPTH_STENCIL = 4, RENDER = 8, DEPTH2 = 16 };

        uint _fbo;
        uint _texture;
        uint _depth;
        uint _depth2;
        uint _stencil;
        uint _rbo;
        BufferType _bufferType;

        protected int _width;
        protected int _height;

        public int Width => _width;

        public int Height => _height;

        public uint DepthTexture2ID => _depth2;

        public uint FrameBufferID => _fbo;

        public uint ColorTextureID => _texture;

        public uint DepthTextureID => _depth;

        public uint RenderTextureID => _rbo;

        public uint TextureID => _texture;

        public bool IsColorBuffer => _texture != 0;

        public bool IsDepthBuffer => _depth != 0;

        public bool IsRenderBuffer => _rbo != 0;

        public void CreateBuffer(int width, int height, BufferType bufferType, uint interpolatiion = Gl.NEAREST)
        {
            _width = width;
            _height = height;
            _bufferType = bufferType;

            if (_width <= 0 || _height <= 0)
                throw new Exception($"width={_width}, height={_height} 중 하나는 0일 수 없습니다.");

            // framebuffer configuration
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);

            // create a color attachment texture
            if (bufferType.HasFlag(BufferType.COLOR))
            {
                _texture = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, _texture);
                Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, interpolatiion);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, interpolatiion);
                Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _texture, 0);
                Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, _texture, 0);
                Gl.BindTexture(TextureTarget.Texture2d, 0);
            }

            // Destination depth map creation
            if (bufferType.HasFlag(BufferType.DEPTH2))
            {
                _depth2 = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, _depth2);
                Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgb16f, _width, _height, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
                Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2d, _depth2, 0);
                Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, _depth2, 0);
                Gl.BindTexture(TextureTarget.Texture2d, 0);
            }

            // Destination depth map creation
            if (bufferType.HasFlag(BufferType.DEPTH))
            {
                _depth = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, _depth);
                Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent, _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, interpolatiion);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, interpolatiion);
                Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, _depth, 0);
                Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, _depth, 0);
                Gl.BindTexture(TextureTarget.Texture2d, 0);
            }

            // Destination depth, stencil map creation
            if (bufferType.HasFlag(BufferType.DEPTH_STENCIL))
            {
                _depth = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, _depth);
                Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthStencil, _width, _height, 0, PixelFormat.DepthStencil, PixelType.Float, IntPtr.Zero);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, interpolatiion);
                Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, interpolatiion);
                Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2d, _depth, 0);
                Gl.BindTexture(TextureTarget.Texture2d, 0);
            }

            // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)
            if (bufferType.HasFlag(BufferType.RENDER))
            {
                _rbo = Gl.GenRenderbuffer();
                Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
                Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, _width, _height);
                Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);
                Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, _depth, 0);
            }

            // DrawBuffer를 설정한다.
            Gl.DrawBuffers(new int[] {
                Gl.COLOR_ATTACHMENT0,
                Gl.COLOR_ATTACHMENT1,
            });

            Console.WriteLine(Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete ?
                $"Frame Buffer(ID={_fbo}) is completed." :
                $"ERROR::FRAMEBUFFER:: Framebuffer is not complete!");

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Clear(ClearBufferMask mask)
        {
            Gl.Clear(mask);
        }

        public void Viewport(int width, int height)
        {
            Gl.Viewport(0, 0, width, height);
        }

        public void BindFrameBuffer()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        }

        public void UnbindFrameBuffer()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.BindBuffer(BufferTarget.PixelPackBuffer, _depth2);
            Gl.BufferData(BufferTarget.PixelPackBuffer, (uint)(_width * _height * sizeof(byte) * 3), IntPtr.Zero, BufferUsage.StreamRead);
            Gl.ReadPixels(0, 0, _width, _height,  PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            IntPtr depthBufferPTR = Gl.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);

            byte[] myPixels = new byte[_width * _height * 3];
            Marshal.Copy(depthBufferPTR, myPixels, 0, _width * _height * 3);

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int index = 3 * (i * _width + j);
                        Console.Write(myPixels[index] + " ");
                    }
                }
                Console.WriteLine("\r\n");
            }
        }

        public void SaveDepth(string filename)
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.BindBuffer(BufferTarget.PixelPackBuffer, _depth);
            Gl.BufferData(BufferTarget.PixelPackBuffer, (uint)(_width * _height * sizeof(float) * 1), IntPtr.Zero, BufferUsage.StreamRead);
            Gl.ReadPixels(0, 0, _width, _height, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            IntPtr depthBufferPTR = Gl.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);

            float[] myPixels = new float[_width * _height * 1];
            Marshal.Copy(depthBufferPTR, myPixels, 0, _width * _height * 1);

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    for (int k = 0; k < 1; k++)
                    {
                        int index = 1 * (i * _width + j);
                        Console.Write(myPixels[index] + " ");
                    }
                }
                Console.WriteLine("\r\n");
            }
        }

        public void SaveDepth2(string filename)
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.BindBuffer(BufferTarget.PixelPackBuffer, _depth2);
            Gl.BufferData(BufferTarget.PixelPackBuffer, (uint)(_width * _height * sizeof(float) * 3), IntPtr.Zero, BufferUsage.StreamRead);
            Gl.ReadPixels(0, 0, _width, _height, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            IntPtr depthBufferPTR = Gl.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);

            float[] myPixels = new float[_width * _height * 3];
            Marshal.Copy(depthBufferPTR, myPixels, 0, _width * _height * 3);

            for (int i = 0; i < _height; i++)
            {
                Console.WriteLine("jjj");
                for (int j = 0; j < _width; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int index = 3 * (i * _width + j);
                        Console.Write(myPixels[index] + " ");
                    }
                }
                Console.WriteLine("\r\n");
            }
        }
    }
}
