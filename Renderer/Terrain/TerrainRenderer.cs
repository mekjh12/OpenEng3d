using Common.Abstractions;
using Model3d;
using Noise;
using OpenGL;
using Shader;
using System;
using Terrain;

namespace Renderer
{
    /// <summary>
    /// 
    /// </summary>
    public class TerrainRenderer
    {
        // 높이기반 텍스처 소스 : https://polyhaven.com/textures/terrain

        TerrainTessellationShader _shader;
        TerrainNormalLineShader _nshader;
        TerrainShadowMapShader _shadowShader;
        ShadowMap _shadowmap;

        Entity _entity;

        Texture[] _groundTextures;
        Texture _detailTexture;
        uint _normalTexture;
        Texture _rockTexture;
        Texture _faultTexture;
        Texture _riverMap;
        Texture _mossRockTexture;

        float _time = 0.0f;
        bool _isNormalVisualization = false;

        // 단층 파라미터 설정
        float _faultScale = 0.0001f;           // UV 스케일 (값이 작을수록 넓은 범위 표현)
        float _displacement = 80.0f;           // 단층으로 인한 상하/좌우 변위량 (미터 단위)
        float _zoneWidth = 0.05f;              // 단층 경계면(각력암)의 폭
        float _intensity = 0.1f;               // 단층 흔적의 시각적 강도

        // 테스트 기능 온오프
        bool _onFunc = true;

        // --------------------------------------------------------
        // 속성
        // --------------------------------------------------------

        public uint ShadowMapTextureID => _shadowmap.DepthTextureID;
        public ShadowMap ShadowMap => _shadowmap;
        public Matrix4x4f LightViewMatrix => _shadowmap.LightViewMatrix;
        public Matrix4x4f LightProjMatrix => _shadowmap.LightProjMatrix;

        // --------------------------------------------------------
        // 생성자
        // --------------------------------------------------------

        public TerrainRenderer(TerrainTessellationShader shader, string projectPath)
        {
            _shader = shader;
            _nshader = new TerrainNormalLineShader(projectPath);
            _shadowShader = new TerrainShadowMapShader(projectPath);  // ⭐ 추가
            _shadowmap = new ShadowMap(2048, 2048);  // 해상도 상향 권장
        }

        /// <summary>
        /// 태양 관점에서 Shadow Map을 렌더링합니다.
        /// </summary>
        /// <param name="sunDirection">태양에서 지표로 향하는 벡터</param>
        /// <param name="heightScale"></param>
        public void RenderShadowMap(Vertex3f sunDirection, float heightScale = 200.0f, bool isClearBuffer = false)
        {
            if (_entity is null || _entity.Model == null) return;

            // 그림자맵을 렌더링한다.
            Vertex3f terrainCenter = new Vertex3f(0, 0, 0);
            float terrainSize = 1000.0f;
            _shadowmap.Update(sunDirection, terrainCenter, terrainSize);

            // Shadow Map FBO 바인딩
            _shadowmap.Bind();

            // 지우기 옵션
            if (isClearBuffer) _shadowmap.Clear();

            // 앞면만 렌더링 (뒷면 컬링)
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(CullFaceMode.Back);

            _shadowShader.Bind();
            _shadowShader.LoadLightProjMatrix(_shadowmap.LightProjMatrix);
            _shadowShader.LoadLightViewMatrix(_shadowmap.LightViewMatrix);
            _shadowShader.LoadModelMatrix(_entity.ModelMatrix);
            _shadowShader.LoadHeightScale(heightScale);

            foreach (RawModel3d rawModel in _entity.Model)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);

                TexturedModel modelTextured = rawModel as TexturedModel;
                _shadowShader.LoadHeightMap(modelTextured.Texture.TextureID);

                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, rawModel.IBO);
                Gl.PatchParameter(PatchParameterName.PatchVertices, 4);
                Gl.DrawElements(PrimitiveType.Patches, rawModel.VertexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

                Gl.DisableVertexAttribArray(1);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }

            _shadowShader.Unbind();

            // 앞면만 렌더링으로 복원 (뒷면 컬링)
            Gl.CullFace(CullFaceMode.Back);

            _shadowmap.Unbind();
        }

        public bool ToggleFunction()
        {
            _onFunc = !_onFunc;
            return _onFunc;
        }

        public void SetTerrain(Entity entity)
        {
            _entity = entity;
        }

        public void CreateFaultTexture()
        {

            _faultTexture = FaultMapGenerator.Generate(512, 512, 45);
            FaultMapGenerator.SaveTexture(_faultTexture, @"C:\Users\mekjh\OneDrive\바탕 화면\fault.png", 512, 512);
        }

        public void LoadMossRockTexture(string fileName)
        {
            _mossRockTexture = new Texture(fileName);
        }

        public void LoadRiverMapTexture(string fileName)
        {
            _riverMap = new Texture(fileName);
        }

        [Obsolete("폐기할 예정입니다.")]
        public void SetGroundTextures(Texture[] groundTextures, Texture normalTexture, Texture detailTexture)
        {
            _groundTextures = groundTextures;
            _detailTexture = detailTexture;
            _normalTexture = normalTexture.TextureID;
        }

        public void LoadRockTexture(string fileName)
        {
            _rockTexture = new Texture(fileName);
        }

        public void Update(float duration)
        {
            _time += duration;
        }

        public void LoadTerrainLevelTextures(string path, string[] fileNames)
        {
            _groundTextures = new Texture[fileNames.Length];
            for (int i = 0; i < fileNames.Length; i++)
            {
                _groundTextures[i] = new Texture(path + fileNames[i]);
            }
        }

        public void LoadDetailTexture(string fileName)
        {
            _detailTexture = new Texture(fileName);
        }

        public void LoadTerrainNormalMap(uint textureId)
        {
            _normalTexture = textureId;
        }

        public void Render(Camera camera, bool isDetailMap = true, float heightScale = 1.0f)
        {
            if (_entity is null) return;
            if (_entity.Model == null) return;

            if (_groundTextures is null || _groundTextures.Length < 5)
            {
                throw new Exception("지형 텍스처가 설정되지 않았습니다.");
            }

            _shader.Bind();
            _shader.LoadTime(_time);
            _shader.LoadDetailMap(_detailTexture == null ? 0 : _detailTexture.TextureID);

            // 지형 텍스처들
            _shader.LoadTerrainTextures(
                _groundTextures[0].TextureID,
                _groundTextures[1].TextureID,
                _groundTextures[2].TextureID,
                _groundTextures[3].TextureID,
                _groundTextures[4].TextureID
            );

            _shader.LoadEnableFunc(_onFunc);

            // 강줄기 맵
            if (_riverMap != null) _shader.LoadRiverRoadMap(_riverMap.TextureID);
            _shader.LoadMossRockTexture(_mossRockTexture.TextureID);

            // 지형 높이 임계값
            _shader.LoadRockTexture(_rockTexture.TextureID);

            // 단층 보로누이맵
            _shader.LoadFaultMap(_faultTexture.TextureID);
            _shader.LoadFaultParameters(_faultScale, _displacement, _zoneWidth, _intensity);

            // 지형 기초정보 유니폼
            _shader.LoadIsDetailMap(isDetailMap);
            _shader.LoadHeightScale(heightScale);

            foreach (RawModel3d rawModel in _entity.Model)
            {
                Gl.BindVertexArray(rawModel.VAO);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);

                // 지형 텍스처 바인딩
                TexturedModel modelTextured = rawModel as TexturedModel;

                _shader.LoadHeightHighResolutionMap(modelTextured.Texture.TextureID);
                _shader.LoadHeightLowResolutionMap(modelTextured.Texture.TextureID);

                // 모델행렬과 법선행렬을 바인딩
                _shader.LoadNormalMatrix(_entity.NormalMatrix);
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
