using Camera3d;
using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;

namespace Terrain
{
    public class TerrainCluster
    {
        TerrainImposterShader _terrainImposterShader;
        Entity _patchEntity;

        public TerrainCluster(string path)
        {
            _terrainImposterShader = new TerrainImposterShader(path);

        }

        public void BakeEntity(TerrainMap terrainMap, int x, int y)
        {
            RawModel3d patch = terrainMap.BakeRawModel3dFromPatchIndex(x, y);
            patch.GenerateBoundingBox();
            _patchEntity = new Entity("patch", "", patch);
            _patchEntity.UpdateBoundingBox();
        }

        public void Update(Camera camera)
        {

        }

        public void Render(TerrainMap terrainMap, Camera camera)
        {
            Gl.Disable(EnableCap.Blend);

            Matrix4x4f minusTrans = Matrix4x4f.Translated(0, 0, -_patchEntity.Position.z);

            TerrainImposterShader shader = _terrainImposterShader;

            shader.Bind();

            //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.fogColor, Vertex3f.One);
            //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.fogDensity, 0.01f);
            //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.fogPlane, new Vertex3f(0, 0, 10));
            //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.isFogEnable, false);

            foreach (RawModel3d rawModel in _patchEntity.Models)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);

                TexturedModel modelTextured = rawModel as TexturedModel;
                shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.gIsDetailMap, true);
                shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.gReversedLightDir, Vertex3f.One);

                shader.SetInt("gHeightMap", 0);
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2d, terrainMap.HeightMapTexture.TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

                shader.SetInt("gDetailMap", 1);
                Gl.ActiveTexture(TextureUnit.Texture1);
                Gl.BindTexture(TextureTarget.Texture2d, terrainMap.DetailMap.TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.NEAREST);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.NEAREST);

                for (int i = 0; i < 5; i++)
                {
                    shader.SetInt($"gTextureHeight{i}", i + 2);
                    Gl.ActiveTexture(TextureUnit.Texture2 + i);
                    Gl.BindTexture(TextureTarget.Texture2d, terrainMap.GroundTextures[i].TextureID);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.MIRRORED_REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.MIRRORED_REPEAT);
                }

                //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.color, _patchEntity.Material.Ambient);
                //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.proj, camera.ProjectiveMatrix);
                //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.view, camera is OrbitCamera ? (camera as OrbitCamera).ViewMatrix : camera.ViewMatrix);
                //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.model, minusTrans * _patchEntity.ModelMatrix);
                //shader.LoadUniform(TerrainImposterShader.UNIFORM_NAME.camPos, camera.Position);

                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, rawModel.IBO);
                Gl.PatchParameter(PatchParameterName.PatchVertices, 4);
                Gl.DrawElements(PrimitiveType.Patches, rawModel.VertexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

                Gl.DisableVertexAttribArray(2);
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }


            shader.Unbind();

            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

    }
}
