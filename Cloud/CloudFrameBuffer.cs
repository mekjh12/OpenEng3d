using OpenGL;
using System;
using System.Runtime.InteropServices;

namespace Cloud
{
    public class CloudFrameBuffer
    {
        uint _fbo;
        uint _texture;
        uint _depth;
        uint _rbo;

        protected int _width;
        protected int _height;

        public int Width => _width;

        public int Height => _height;

        public uint FrameBufferID => _fbo;

        public uint ColorTextureID => _texture;

        public uint DepthTextureID => _depth;


        public void CreateBuffer(int width, int height)
        {
            _width = width;
            _height = height;

            if (_width * _height == 0)
                throw new Exception($"width={_width}, height={_height} 중 하나는 0일 수 없습니다.");

            // framebuffer configuration
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);

            // create a color attachment texture
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _texture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R16f, _width, _height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _texture, 0);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // Destination depth map creation
            _depth = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _depth);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R16f, _width, _height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2d, _depth, 0);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)
            _rbo = Gl.GenRenderbuffer();
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
            Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, _width, _height);
            Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rbo);
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            // DrawBuffer를 설정한다.
            Gl.DrawBuffers(new int[] { Gl.COLOR_ATTACHMENT0, Gl.COLOR_ATTACHMENT1 });

            Console.WriteLine(Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferStatus.FramebufferComplete ?
                $"Frame Buffer(ID={_fbo}) is completed." :
                $"ERROR::FRAMEBUFFER:: Framebuffer is not complete!");

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void CreateBuffer2(int width, int height)
        {
            _width = width;
            _height = height;

            if (_width * _height == 0)
                throw new Exception($"width={_width}, height={_height} 중 하나는 0일 수 없습니다.");

            // framebuffer configuration
            _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);

            // create a color attachment texture
            _texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _texture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _texture, 0);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // Destination depth map creation
            _depth = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _depth);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R16f, _width, _height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2d, _depth, 0);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // create a renderbuffer object for depth and stencil attachment (we won't be sampling these)
            _rbo = Gl.GenRenderbuffer();
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
            Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, _width, _height);
            Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rbo);
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

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
            Gl.DeleteTextures(new uint[] { _texture, _depth });
            Gl.DeleteFramebuffers(new uint[] { _fbo });
        }

        public void SaveDepth()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.BindBuffer(BufferTarget.PixelPackBuffer, _depth);
            Gl.BufferData(BufferTarget.PixelPackBuffer, (uint)(_width * _height * sizeof(float) * 4), IntPtr.Zero, BufferUsage.StreamRead);
            Gl.ReadPixels(0, 0, _width, _height, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            IntPtr depthBufferPTR = Gl.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);

            float[] myPixels = new float[_width * _height * 4];
            Marshal.Copy(depthBufferPTR, myPixels, 0, _width * _height * 4);

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    for (int k = 0; k < 1; k++)
                    {
                        int index = 4 * (i * _width + j);
                        Console.Write(myPixels[index] + " ");
                    }
                }
                Console.WriteLine("\r\n");
            }

        }

        public void Save()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.BindBuffer(BufferTarget.PixelPackBuffer, _texture);
            Gl.BufferData(BufferTarget.PixelPackBuffer, (uint)(_width * _height * sizeof(byte) * 3), IntPtr.Zero, BufferUsage.StreamRead);
            Gl.ReadPixels(0, 0, _width, _height, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            IntPtr depthBufferPTR = Gl.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);

            byte[] myPixels = new byte[_width * _height * 3];
            Marshal.Copy(depthBufferPTR, myPixels, 0, _width * _height * 3);

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int index = 3 * (i * _width + j) + 1;
                        Console.Write(myPixels[index] + " ");
                    }
                }
                Console.WriteLine("\r\n");
            }
        }
    }
}
