using OpenGL;
using System;
using System.Text;

namespace Shader.CloudShader
{
    public class SimpleComputeShader3D
    {
        private uint _computeProgram;
        private uint _texture3DHandle;
        private int _textureWidth = 64;
        private int _textureHeight = 64;
        private int _textureDepth = 64;

        private const string ComputeShaderSource = @"
#version 430 core
layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;
layout(binding = 0, r32f) uniform image3D outputTexture;
void main()
{
    ivec3 texelCoord = ivec3(gl_GlobalInvocationID.xyz);
    float value = 0.0f;
    
    value = (texelCoord.z == 0) ? float(texelCoord.x) / 64.0f: float(texelCoord.x) / 64.0f * float(texelCoord.z);
    imageStore(outputTexture, texelCoord, vec4(value, 0, 0, 1));
}
";

        public void Run()
        {
            Gl.Initialize();

            _texture3DHandle = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture3d, _texture3DHandle);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapR, Gl.CLAMP_TO_EDGE);
            Gl.TexImage3D(TextureTarget.Texture3d, 0, InternalFormat.R32f, _textureWidth, _textureHeight, _textureDepth, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);

            float clearValue = 0.5f;
            Gl.ClearTexImage(_texture3DHandle, 0, PixelFormat.Red, PixelType.Float, new float[] { clearValue });

            uint computeShader = Gl.CreateShader(ShaderType.ComputeShader);
            Gl.ShaderSource(computeShader, new string[] { ComputeShaderSource });
            Gl.CompileShader(computeShader);

            int status;
            Gl.GetShader(computeShader, ShaderParameterName.CompileStatus, out status);
            if (status == 0)
            {
                StringBuilder infoLog = new StringBuilder(512);
                Gl.GetShaderInfoLog(computeShader, 512, out int length, infoLog);
                Console.WriteLine($"컴퓨트 셰이더 컴파일 에러: {infoLog.ToString()}");
                return;
            }

            _computeProgram = Gl.CreateProgram();
            Gl.AttachShader(_computeProgram, computeShader);
            Gl.LinkProgram(_computeProgram);

            Gl.GetProgram(_computeProgram, ProgramProperty.LinkStatus, out status);
            if (status == 0)
            {
                StringBuilder infoLog = new StringBuilder(512);
                Gl.GetProgramInfoLog(_computeProgram, 512, out int length, infoLog);
                Console.WriteLine($"셰이더 프로그램 링크 에러: {infoLog.ToString()}");
                return;
            }

            Gl.DeleteShader(computeShader);

            Gl.UseProgram(_computeProgram);

            Gl.BindImageTexture(0, _texture3DHandle, 0, true, 0, BufferAccess.ReadWrite, InternalFormat.R32f);

            ErrorCode error = Gl.GetError();
            if (error != ErrorCode.NoError)
            {
                Console.WriteLine($"OpenGL Error after BindImageTexture: {error}");
            }

            //Gl.Uniform3(Gl.GetUniformLocation(_computeProgram, "resolution"), _textureWidth, _textureHeight, _textureDepth);

            int groupsX = (int)Math.Ceiling(_textureWidth / 8.0);
            int groupsY = (int)Math.Ceiling(_textureHeight / 8.0);
            int groupsZ = (int)Math.Ceiling(_textureDepth / 8.0);
            Console.WriteLine($"디스패치 그룹 크기: ({groupsX}, {groupsY}, {groupsZ})");
            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, (uint)groupsZ);

            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Gl.UseProgram(0);

            ReadTextureData();

            CleanUp();
        }

        private void ReadTextureData()
        {
            float[] data = new float[_textureWidth * _textureHeight * _textureDepth];
            Gl.BindTexture(TextureTarget.Texture3d, _texture3DHandle);
            Gl.GetTexImage(TextureTarget.Texture3d, 0, PixelFormat.Red, PixelType.Float, data);
            Gl.BindTexture(TextureTarget.Texture3d, 0);

            Console.WriteLine("Sample Points:");
            // z 값이 0인 경우와 그렇지 않은 경우를 모두 출력하여 비교
            Console.WriteLine("z=0 평면:");
            for (int y = 0; y < Math.Min(3, _textureHeight); y++)
            {
                for (int x = 0; x < Math.Min(3, _textureWidth); x++)
                {
                    int index = 0 * _textureHeight * _textureWidth + y * _textureWidth + x;
                    Console.WriteLine($"({x}, {y}, 0): R={data[index]}");
                }
            }

            Console.WriteLine("\nz=1 평면:");
            for (int y = 0; y < Math.Min(3, _textureHeight); y++)
            {
                for (int x = 0; x < Math.Min(3, _textureWidth); x++)
                {
                    int index = 1 * _textureHeight * _textureWidth + y * _textureWidth + x;
                    Console.WriteLine($"({x}, {y}, 1): R={data[index]}");
                }
            }

            Console.WriteLine("...");
        }

        private void CleanUp()
        {
            if (_computeProgram != 0)
            {
                Gl.DeleteProgram(_computeProgram);
            }
            if (_texture3DHandle != 0)
            {
                Gl.DeleteTextures(_texture3DHandle);
            }
        }
    }
}