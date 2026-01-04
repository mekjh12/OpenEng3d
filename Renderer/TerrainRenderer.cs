using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;

namespace Renderer
{
    public class TerrainRenderer
    {
        TerrainTessellationShader _shader;
        TerrainNormalLineShader _nshader;
        Entity _entity;
        Texture[] _groundTextures;
        Texture _detailTexture;
        bool _isNormalVisualization = false;

        public TerrainRenderer(TerrainTessellationShader shader, string projectPath)
        {
            _shader = shader;
            _nshader = new TerrainNormalLineShader(projectPath);
        }

        public void SetTerrain(Entity entity)
        {
            _entity = entity;
        }

        public void SetGroundTextures(Texture[] groundTextures, Texture detailTexture)
        {
            _groundTextures = groundTextures;
            _detailTexture = detailTexture;
        }

        public void Render(bool isDetailMap = true, float heightScale = 1.0f)
        {
            if (_entity is null) return;
            if (_entity.Model == null) return;

            if (_groundTextures is null || _groundTextures.Length < 5)
            {
                throw new Exception("지형 텍스처가 설정되지 않았습니다.");
            }

            _shader.Bind();

            foreach (RawModel3d rawModel in _entity.Model)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);

                // 지형 텍스처 바인딩
                TexturedModel modelTextured = rawModel as TexturedModel;
                _shader.SetInt("gHeightMap", 0);
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2d, modelTextured.Texture.TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

                _shader.SetInt("gDetailMap", 1);
                Gl.ActiveTexture(TextureUnit.Texture1);
                Gl.BindTexture(TextureTarget.Texture2d, _detailTexture == null ? 0 : _detailTexture.TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.NEAREST);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.NEAREST);

                for (int i = 0; i < 5; i++)
                {
                    _shader.SetInt($"gTextureHeight{i}", i + 2);
                    Gl.ActiveTexture(TextureUnit.Texture2 + i);
                    Gl.BindTexture(TextureTarget.Texture2d, _groundTextures[i] == null ? 0 : _groundTextures[i].TextureID);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.MIRRORED_REPEAT);
                    Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.MIRRORED_REPEAT);
                }

                // 지형 기초정보 유니폼
                _shader.LoadIsDetailMap(isDetailMap);
                _shader.LoadHeightScale(heightScale);
                _shader.LoadColor(_entity.Material.Ambient);
                _shader.LoadModelMatrix(_entity.ModelMatrix);

                // 지형 렌더링
                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, rawModel.IBO);
                Gl.PatchParameter(PatchParameterName.PatchVertices, 4);
                Gl.DrawElements(PrimitiveType.Patches, rawModel.VertexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

                Gl.DisableVertexAttribArray(2);
                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            _shader.Unbind();

            // 노멀 시각화 렌더링
            if (_isNormalVisualization)
            {
                RenderTerrainNormals(_entity, _nshader, normalLength:5f, heightScale: heightScale);
            }
        }

        /// <summary>
        /// 지형의 법선 벡터를 RGB 라인으로 시각화하여 렌더링합니다.
        /// Vertex 0: 빨강, Vertex 1: 녹색, Vertex 2: 파랑
        /// </summary>
        public void RenderTerrainNormals(
            Entity terrainEntity,
            TerrainNormalLineShader normalShader,
            float normalLength = 5.0f,
            float heightScale = 200.0f)  // ⭐ 기본값 수정
        {
            if (terrainEntity is null) return;
            if (terrainEntity.Model == null) return;

            // 라인이 지형 위에 보이도록
            Gl.Disable(EnableCap.CullFace);

            normalShader.Bind();  // ⭐ Bind → Start

            // 전역 유니폼 설정 (한 번만)
            normalShader.LoadHeightScale(heightScale);
            normalShader.LoadNormalLength(normalLength);
            normalShader.LoadModelMatrix(terrainEntity.ModelMatrix);  // ⭐ terrainEntity 사용

            foreach (RawModel3d rawModel in terrainEntity.Model)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0); // position
                Gl.EnableVertexAttribArray(1); // texCoord

                TexturedModel modelTextured = rawModel as TexturedModel;

                // 높이맵만 바인딩
                normalShader.SetInt("gHeightMap", 0);
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2d, modelTextured.Texture.TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

                // 지형 렌더링 (Patches)
                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, rawModel.IBO);
                Gl.PatchParameter(PatchParameterName.PatchVertices, 4);
                Gl.DrawElements(PrimitiveType.Patches, rawModel.VertexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            normalShader.Unbind();

            Gl.Enable(EnableCap.CullFace);
        }

        public void SetNormalVisualization(bool isEnable)
        {
            _isNormalVisualization = isEnable;
        }
    }
}
