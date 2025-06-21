using OpenGL;
using System;
using System.IO;
using Common;

namespace Shader
{
    public class WorleyNoiseShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        const string VERTEX_FILE = @"\Shader\WorleyNoiseShader\worley.vert";
        const string FRAGMENT_FILE = @"\Shader\WorleyNoiseShader\worley.frag";

        public WorleyNoiseShader(string projectPath) : base()
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
            UniformLocations("viewport_size", "focal_length", "aspect_ratio", "gamma", "step_length", "ray_origin", "volume", "densityPower");
            UniformLocations("absorption", "centerPosition", "boundSize", "inSide");
        }

        public void LoadCameraInsideCube(bool inSide)
        {
            base.LoadBoolean(_location["inSide"], inSide);
        }

        public void LoadCenterPosition(Vertex3f centerPosition)
        {
            base.LoadVector(_location["centerPosition"], centerPosition);
        }


        public void LoadBoundSize(Vertex3f boundSize)
        {
            base.LoadVector(_location["boundSize"], boundSize);
        }


        public void LoadAbsorption(float absorption)
        {
            base.LoadFloat(_location["absorption"], absorption);
        }

        public void LoadDensityPower(float densityPower)
        {
            base.LoadFloat(_location["densityPower"], densityPower);
        }

        public void LoadStepLength(float stepLength)
        {
            base.LoadFloat(_location["step_length"], stepLength);
        }

        public void LoadGamma(float gamma)
        {
            base.LoadFloat(_location["gamma"], gamma);
        }

        public void LoadAspectRatio(float aspectRatio)
        {
            base.LoadFloat(_location["aspect_ratio"], aspectRatio);
        }

        public void LoadFocalLength(float focalLength)
        {
            base.LoadFloat(_location["focal_length"], focalLength);
        }

        public void LoadViewportSize(Vertex2f viewportSize)
        {
            base.LoadVector(_location["viewport_size"], viewportSize);
        }

        public void LoadRayOrgin(Vertex3f orgin)
        {
            base.LoadVector(_location["ray_origin"], orgin);
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
