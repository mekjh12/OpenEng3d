using Common;
using Common.Abstractions;
using Model3d;
using Noise;
using OpenGL;
using Shader;
using System;

namespace Terrain
{
    /// <summary>
    /// 
    /// </summary>
    public class TerrainRenderer
    {
        const float WORLD_POSITION_OFFSET = (Constants.TERRAIN_TILE_SIZE - 1) / 2;
        const int regionSize = (Constants.TERRAIN_TILE_SIZE - 1);

        TerrainTessellationShader _shader;
        TerrainNormalLineShader _nshader;
        TerrainShadowMapShader _shadowShader;
        RiverTessellationShader _riverShader;

        // 강 메시 (AABB 쿼드 패치)
        uint _riverVao = 0;
        uint _riverIbo = 0;
        int _riverCount = 0;

        uint _heightMapTextureId = 0;

        Texture[] _groundTextures;
        Texture _detailTexture;
        uint _normalTexture;
        Texture _rockTexture;
        Texture _faultTexture;
        Texture _riverMap;
        Texture _mossRockTexture;

        float _time = 0.0f;

        // 단층 파라미터 설정
        float _faultScale = 0.0001f;           // UV 스케일 (값이 작을수록 넓은 범위 표현)
        float _displacement = 80.0f;           // 단층으로 인한 상하/좌우 변위량 (미터 단위)
        float _zoneWidth = 0.05f;              // 단층 경계면(각력암)의 폭
        float _intensity = 0.1f;               // 단층 흔적의 시각적 강도

        // 고해상도 지형 렌더링 변수들
        uint _vao = 0;
        uint _ibo = 0;
        int _count = 0;

        // 저해상도 지형 변수(6개)
        uint[] _vao1 = new uint[6];
        uint[] _ibo1 = new uint[6];
        int[] _count1 = new int[6];

        Matrix4x4f _worldMatrix;

        TerrainStreamingManager _streamingManager;

        ShadowMap _shadowmap;


        // --------------------------------------------------------
        // 속성
        // --------------------------------------------------------


        // --------------------------------------------------------
        // 생성자
        // --------------------------------------------------------

        public TerrainRenderer(TerrainStreamingManager streamingManager)
        {            
            _shader = ShaderManager.Instance.GetShader<TerrainTessellationShader>();
            _shadowmap = new ShadowMap(2048, 2048);  // 해상도 상향 권장

            _riverShader = ShaderManager.Instance.GetShader<RiverTessellationShader>();

            _shadowShader = new TerrainShadowMapShader(StrRes.PROJECT_PATH);
            _nshader = new TerrainNormalLineShader(StrRes.PROJECT_PATH);

            _streamingManager = streamingManager;

            CreateTerrainMesh(32, 8);
            CreateRiverMesh(32);
        }

        public void CreateRiverMesh(int patchGridSize = 32)
        {
            RawModel3d riverPlane = Loader3d.LoadPlaneNxN(patchGridSize / 2,
                (int)((1024+32) / patchGridSize));
            _riverVao = riverPlane.VAO;
            _riverIbo = riverPlane.IBO;
            _riverCount = riverPlane.IndexCount;
        }

        private void CreateTerrainMesh(int resHigh = 32, int resLow = 2)
        {
            // 영역 크기 계산
            int tileSize = Constants.TERRAIN_TILE_SIZE - 1;

            // 기본 평면 메시 생성 (//.LoadPlaneNxN(resLow / 2, tileSize / resLow);)
            RawModel3d planeNxN = Loader3d.LoadPlaneNxN(resHigh / 2, tileSize / resHigh);
            _vao = planeNxN.VAO;
            _ibo = planeNxN.IBO;
            _count = planeNxN.IndexCount;

            for (int i = 0; i < 5; i++)
            {
                CreateAdaptivePlane(i, resHigh, resLow);
            }

            _worldMatrix = Matrix4x4f.Identity;
        }

        private void CreateAdaptivePlane(int index, int resHigh = 32, int resLow = 2)
        {
            int tileSize = Constants.TERRAIN_TILE_SIZE - 1;

            bool[] edgeFlags = new bool[4];

            if (index == 0) edgeFlags = new bool[] { false, false, false, false };
            if (index == 1) edgeFlags = new bool[] { false, false, true, false };
            if (index == 2) edgeFlags = new bool[] { false, false, false, true };
            if (index == 3) edgeFlags = new bool[] { true, false, false, false };
            if (index == 4) edgeFlags = new bool[] { false, true, false, false };
            if (index == 5) edgeFlags = null;

            // 저해상도 적응형 평면 메시 생성 (엣지만 고해상도)
            RawModel3d adaptivePlane = Loader3d.LoadAdaptivePlane(
                innerN: resLow / 2,              // 내부는 저해상도 (예: 32/4 = 8)
                edgeN: resHigh / 2,              // 엣지는 고해상도 (예: 128/4 = 32)
                unitSize: tileSize / resLow,    // 고해상도와 동일한 unitSize 사용
                edgeFlags: edgeFlags // 모든 엣지 세분화
            );

            _vao1[index] = adaptivePlane.VAO;
            _ibo1[index] = adaptivePlane.IBO;
            _count1[index] = adaptivePlane.IndexCount;
        }

        public void CreateFaultTexture()
        {
            _faultTexture = FaultMapGenerator.Generate(512, 512, 45);
            //FaultMapGenerator.SaveTexture(_faultTexture, @"C:\Users\mekjh\OneDrive\바탕 화면\fault.png", 512, 512);
        }

        public void LoadMossRockTexture(string fileName)
        {
            _mossRockTexture = new Texture(fileName);
        }

        public void LoadRiverMapTexture(string fileName)
        {
            _riverMap = new Texture(fileName);
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

        public void SetRegion(int coordX, int coordY, uint heightMapTextureId)
        {
            _worldMatrix[3, 0] = coordX * regionSize + WORLD_POSITION_OFFSET;
            _worldMatrix[3, 1] = coordY * regionSize + WORLD_POSITION_OFFSET; ;
            _heightMapTextureId = heightMapTextureId;
        }

        /// <summary>
        /// 태양 관점에서 Shadow Map을 렌더링합니다.
        /// </summary>
        /// <param name="sunDirection">태양에서 지표로 향하는 벡터</param>
        /// <param name="heightScale"></param>
        public void RenderShadowMap(Vertex3f sunDirection, float heightScale = 200.0f, bool isClearBuffer = false)
        {
            // 그림자맵을 렌더링한다.
            Vertex3f terrainCenter = new Vertex3f(0, 0, 0);
            float terrainSize = 1024.0f;
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
            _shadowShader.LoadModelMatrix(_worldMatrix);
            _shadowShader.LoadHeightScale(heightScale);

            Gl.BindVertexArray(_vao);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);

            _shadowShader.LoadHeightMap(_heightMapTextureId);

            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo);
            Gl.PatchParameter(PatchParameterName.PatchVertices, 4);
            Gl.DrawElements(PrimitiveType.Patches, _count, DrawElementsType.UnsignedInt, IntPtr.Zero);

            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);

            _shadowShader.Unbind();

            // 앞면만 렌더링으로 복원 (뒷면 컬링)
            Gl.CullFace(CullFaceMode.Back);

            _shadowmap.Unbind();
        }

        public void Render(Camera camera)
        {
            RenderTerrain(camera);
            //RenderRivers(camera);
        }

        public void RenderTerrain(Camera camera)
        {
            int tileLowRadius = _streamingManager.LowTileRadius;
            int tileHighRadius = _streamingManager.HighTileRadius;

            // ── 셰이더 바인드 & 공통 유니폼은 한 번만 ──
            _shader.Bind();
            _shader.LoadTime(_time);
            _shader.LoadDetailMap(_detailTexture == null ? 0 : _detailTexture.TextureID);
            _shader.LoadTerrainTextures(
                _groundTextures[0].TextureID,
                _groundTextures[1].TextureID,
                _groundTextures[2].TextureID,
                _groundTextures[3].TextureID,
                _groundTextures[4].TextureID
            );
            _shader.LoadMossRockTexture(_mossRockTexture.TextureID);
            _shader.LoadRockTexture(_rockTexture.TextureID);
            _shader.LoadFaultMap(_faultTexture.TextureID);
            _shader.LoadFaultParameters(_faultScale, _displacement, _zoneWidth, _intensity);
            _shader.LoadIsDetailMap(true);
            _shader.LoadHeightScale(Constants.TERRAIN_VERTICAL_SCALE);
            _shader.LoadBlendFactor(0.0f);
            _shader.LoadNormalMatrix(Matrix3x3f.Identity);

            Gl.PatchParameter(PatchParameterName.PatchVertices, 4);

            // ── 패스 1: 고해상도 타일 (absX <= 1 && absY <= 1) ──
            Gl.BindVertexArray(_vao);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
            Gl.EnableVertexAttribArray(2);
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int cx = _streamingManager.CurrentRegionX + dx;
                    int cy = _streamingManager.CurrentRegionY + dy;
                    uint textureId = _streamingManager.GetRegionTexture(cx, cy, dx, dy);
                    SetRegion(cx, cy, textureId);

                    _shader.LoadHeightHighResolutionMap(_heightMapTextureId);
                    _shader.LoadHeightLowResolutionMap(_heightMapTextureId);
                    
                    _shader.LoadAdjacentHeightMaps(_streamingManager.GetAdjRegionTextures(cx, cy));
                    _shader.LoadRiverRoadMap(_streamingManager.GetRiverRoadTexture(cx, cy));

                    _shader.LoadModelMatrix(_worldMatrix);
                    _shader.LoadNormalMap(_streamingManager.GetRegionNormalTexture(cx, cy));

                    Gl.DrawElements(PrimitiveType.Patches, _count, DrawElementsType.UnsignedInt, IntPtr.Zero);
                }
            }

            // ── 패스 2: 저해상도 타일 (VAO 타입별로 묶어서) ──
            for (int i = 0; i < 5; i++) // 0~4 lowMapTypeIndex
            {
                uint vao = _vao1[i];
                uint ibo = _ibo1[i];
                int count = _count1[i];

                Gl.BindVertexArray(vao);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);
                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);

                for (int x = -tileLowRadius; x <= tileLowRadius; x++)
                {
                    for (int y = -tileLowRadius; y <= tileLowRadius; y++)
                    {
                        // 고해상도 영역은 이미 처리됨. 저해상도 영역만 아래는 처리함.
                        int absX = Math.Abs(x); 
                        int absY = Math.Abs(y);
                        if (absX <= 1 && absY <= 1) continue;

                        // 이 타일의 lowMapTypeIndex 계산
                        int idx = 0;
                        if (absX == 2 && absY <= 1)
                            idx = x > 0 ? 1 : 3;
                        else if (absY == 2 && absX <= 1)
                            idx = y > 0 ? 2 : 4;

                        if (idx != i) continue;

                        // 렌더링
                        int cx = _streamingManager.CurrentRegionX + x;
                        int cy = _streamingManager.CurrentRegionY + y;
                        uint textureId = _streamingManager.GetRegionTexture(cx, cy, x, y);
                        SetRegion(cx, cy, textureId);

                        _shader.LoadHeightHighResolutionMap(_heightMapTextureId);
                        _shader.LoadHeightLowResolutionMap(_heightMapTextureId);
                        _shader.LoadRiverRoadMap(_streamingManager.GetRiverRoadTexture(cx, cy));

                        _shader.LoadAdjacentHeightMaps(_streamingManager.GetAdjRegionTextures(cx, cy));
                        _shader.LoadModelMatrix(_worldMatrix);
                        _shader.LoadNormalMap(_streamingManager.GetRegionNormalTexture(cx, cy));

                        Gl.DrawElements(PrimitiveType.Patches, count, DrawElementsType.UnsignedInt, IntPtr.Zero);
                    }
                }
            }

            // ── 정리 ──
            Gl.DisableVertexAttribArray(2);
            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            _shader.Unbind();
        }

        public void RenderRivers(Camera camera)
        {
            if (_riverVao == 0) return;
            if (_riverShader == null) return;

            _riverShader.Bind();
            _riverShader.LoadHeightScale(Constants.TERRAIN_VERTICAL_SCALE);
            _riverShader.LoadBlendFactor(0.0f);
            _riverShader.LoadRiverHeightOffset(0.5f);
            _riverShader.LoadModelMatrix(_worldMatrix);

            Gl.BindVertexArray(_riverVao);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _riverIbo);
            Gl.PatchParameter(PatchParameterName.PatchVertices, 4);

            // 강이 있는 청크만 순회
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int cx = _streamingManager.CurrentRegionX + dx;
                    int cy = _streamingManager.CurrentRegionY + dy;

                    uint riverMaskTex = _streamingManager.GetRiverRoadTexture(cx, cy);
                    if (riverMaskTex == 0) continue;

                    uint heightTex = _streamingManager.GetRegionTexture(cx, cy, dx, dy);
                    SetRegion(cx, cy, heightTex);

                    _riverShader.LoadModelMatrix(_worldMatrix);
                    _riverShader.LoadHeightHighResMap(_heightMapTextureId);
                    _riverShader.LoadHeightLowResMap(_heightMapTextureId);
                    _riverShader.LoadRiverMask(riverMaskTex);

                    Gl.DrawElements(PrimitiveType.Patches, _riverCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
                }

            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            _riverShader.Unbind();
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
    }
}
