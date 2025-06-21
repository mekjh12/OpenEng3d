using OpenGL;
using System;
using System.IO;
using Common;

namespace Shader
{
    public class WorleyNoiseGenShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        const string VERTEX_FILE = @"\Shader\WorleyNoiseGenShader\worleyGen.vert";
        const string FRAGMENT_FILE = @"\Shader\WorleyNoiseGenShader\worleyGen.frag";

        public WorleyNoiseGenShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        protected override void GetAllUniformLocations()
        {
            UniformLocations("model", "view", "proj");
            UniformLocations("numCellsPerAxis");
        }

        public void BindSSBO(uint bufferIndex, int numCellsPerAxis)
        {
            uint size = ((uint)(numCellsPerAxis * numCellsPerAxis * numCellsPerAxis * Vertex3f.Size));
            uint loc = Gl.GetProgramResourceIndex(_programID, ProgramInterface.ShaderStorageBlock, "shader_data");
            Gl.ShaderStorageBlockBinding(_programID, bufferIndex, loc);
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, loc, bufferIndex);
            Gl.BindBufferRange(BufferTarget.ShaderStorageBuffer, loc, bufferIndex, IntPtr.Zero, size);
        }

        public void LoadTexture(string textureUniformName, TextureUnit textureUnit, uint texture)
        {
            base.LoadInt(_location[textureUniformName], textureUnit - TextureUnit.Texture0);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadTexture3d(uint texture)
        {
            Gl.BindTexture(TextureTarget.Texture3d, texture);
        }

        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["proj"], matrix);
        }

        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["view"], matrix);
        }

        public void LoadModelMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["model"], matrix);
        }

        public void LoadNumOfCellPerAxis(int numCellsPerAxis)
        {
            base.LoadInt(_location["numCellsPerAxis"], numCellsPerAxis);            
        }

    }
}
