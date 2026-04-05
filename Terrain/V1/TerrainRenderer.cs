using Common;
using Common.Abstractions;
using Geometry;
using Model3d;
using Noise;
using Occlusion;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;

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

        AABBBoxShader _boxShader;
        Vertex4f _color = new Vertex4f(1, 0, 1, 1);

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

        // --------------------------------------------------
        // 매프레임 지형을 순회할 때 사용하는 좌표 리스트들
        // --------------------------------------------------
        private int _lastRegionX = int.MinValue;
        private int _lastRegionY = int.MinValue;
        private int _lastRenderedCount;

        // 리전 기준 전체 후보 리스트 (컬링 전)
        private TerrainHighCoord[] _travTerrainHighCoords;      // 항상 9개 고정
        private List<TerrainLowCoord[]> _travTerrainLowCoords;  // 타입별 최대 개수
        private int _travHighCount = 0;
        private int[] _travLowCounts = new int[5];

        // 컬링(뷰프러스텀+HiZ) 후 실제 렌더링 리스트
        private TerrainHighCoord[] _renderHighCoords;
        private List<TerrainLowCoord[]> _renderLowCoords;
        private int _renderHighCount = 0;
        private int[] _renderLowCounts = new int[5];


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

            ShaderManager.Instance.AddShader(new AABBBoxShader(StrRes.PROJECT_PATH));
            _boxShader = ShaderManager.Instance.GetShader<AABBBoxShader>();


            _shadowShader = new TerrainShadowMapShader(StrRes.PROJECT_PATH);
            _nshader = new TerrainNormalLineShader(StrRes.PROJECT_PATH);

            _streamingManager = streamingManager;

            CreateTerrainMesh(32, 8);
            CreateRiverMesh(32);

            // 매프레임 고해상도와 저해상도 리전을 순회하기 위한 리스트
            const uint HIGH_REGION_COUNT = 3 * 3;
            _travTerrainHighCoords = new TerrainHighCoord[HIGH_REGION_COUNT];   // 생성시 초기화됨

            // 생성자
            const int LOW_COUNT_DIRECTIONAL = 3;   // 타입 1,2,3,4
            const int LOW_COUNT_OUTER = 28;        // 타입 0

            // 후보 리스트 (컬링 전)
            _travTerrainLowCoords = new List<TerrainLowCoord[]>(5);
            _travTerrainLowCoords.Add(new TerrainLowCoord[LOW_COUNT_OUTER]);
            _travTerrainLowCoords.Add(new TerrainLowCoord[LOW_COUNT_DIRECTIONAL]);
            _travTerrainLowCoords.Add(new TerrainLowCoord[LOW_COUNT_DIRECTIONAL]);
            _travTerrainLowCoords.Add(new TerrainLowCoord[LOW_COUNT_DIRECTIONAL]);
            _travTerrainLowCoords.Add(new TerrainLowCoord[LOW_COUNT_DIRECTIONAL]);

            // 렌더링 리스트 (컬링 후)
            _renderHighCoords = new TerrainHighCoord[HIGH_REGION_COUNT];
            _renderLowCoords = new List<TerrainLowCoord[]>(5);
            _renderLowCoords.Add(new TerrainLowCoord[LOW_COUNT_OUTER]);
            _renderLowCoords.Add(new TerrainLowCoord[LOW_COUNT_DIRECTIONAL]);
            _renderLowCoords.Add(new TerrainLowCoord[LOW_COUNT_DIRECTIONAL]);
            _renderLowCoords.Add(new TerrainLowCoord[LOW_COUNT_DIRECTIONAL]);
            _renderLowCoords.Add(new TerrainLowCoord[LOW_COUNT_DIRECTIONAL]);
        }

        private void CreateRiverMesh(int patchGridSize = 32)
        {
            RawModel3d riverPlane = Loader3d.LoadPlaneNxN(patchGridSize / 2,
                (int)((1024 + 32) / patchGridSize));
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

        public void Update(float duration, bool isCameraFrameMoved, Matrix4x4f vp, Matrix4x4f view,
                   Polyhedron viewFrustum, HierarchyZBuffer hiZBuffer, bool isFridged = false)
        {
            _time += duration;

            int currentX = _streamingManager.CurrentRegionX;
            int currentY = _streamingManager.CurrentRegionY;

            if (currentX != _lastRegionX || currentY != _lastRegionY)
            {
                _lastRegionX = currentX;
                _lastRegionY = currentY;
                UpdateRegionTravCoords();

                // 리전이 바뀌면 isFridged여도 한 번은 컬링 갱신 필요
                if (!isFridged)
                    ApplyCulling(vp, view, viewFrustum, hiZBuffer);
            }
            else if (isCameraFrameMoved && !isFridged)
            {
                ApplyCulling(vp, view, viewFrustum, hiZBuffer);
            }
            // isFridged == true면 _renderLowCoords, _renderHighCoords 그대로 유지
        }

        /// <summary>
        /// 후보 리스트에서 프러스텀/HiZ 컬링을 적용하여 렌더링 리스트를 갱신한다.
        /// </summary>
        private void ApplyCulling(Matrix4x4f vp, Matrix4x4f view, Polyhedron viewFrustum, HierarchyZBuffer hiZBuffer, bool isFridged = false)
        {
            Plane[] frustumPlanes = viewFrustum?.Planes;

            // 1. 고해상도: 뷰프러스텀 컬링만 적용 (항상 최대 9개)
            _renderHighCount = 0;
            for (int i = 0; i < _travHighCount; i++)
            {
                ref TerrainHighCoord coord = ref _travTerrainHighCoords[i];

                // 프러스텀 컬링
                if (!IsTileVisible(coord.x, coord.y, frustumPlanes)) continue;

                _renderHighCoords[_renderHighCount] = coord;
                _renderHighCount++;
            }
            //Console.WriteLine($"frustumPassed={frustumPassed}");

            // 2. 저해상도: 컬링 적용
            int frustumCulled = 0;
            int hizCulled = 0;

            // 렌더링 리스트 초기화
            for (int typeIdx = 0; typeIdx < 5; typeIdx++)
                _renderLowCounts[typeIdx] = 0;

            // 저해상도 타일의 타입별(외곽에 따라 다름)로 사전 순회 정보를 검사하여 컬링 적용 후 렌더링 리스트에 추가
            for (int typeIdx = 0; typeIdx < 5; typeIdx++)
            {
                // 사전 순회 정보를 가져와서 컬링 적용
                int count = _travLowCounts[typeIdx];

                // 각 타입별로 순회할 타일을 컬링하여 렌더링 리스트에 추가
                for (int i = 0; i < count; i++)
                {
                    ref TerrainLowCoord coord = ref _travTerrainLowCoords[typeIdx][i];
                    AABB3f? aabb = _streamingManager.GetTileAABB(coord.x, coord.y);

                    // 기본색: 노랑(디버깅)
                    _streamingManager.SetTileAABBColor(coord.x, coord.y, new Vertex4f(1, 1, 0, 1));

                    // 프러스텀 컬링
                    if (!IsTileVisible(coord.x, coord.y, frustumPlanes))
                    {
                        // 디버깅: 컬링된 타일은 AABB를 빨강으로 표시
                        _streamingManager.SetTileAABBColor(coord.x, coord.y, new Vertex4f(0, 1, 0, 1));
                        frustumCulled++;
                        continue;
                    }

                    // HiZ 컬링
                    if (hiZBuffer != null)
                    {
                        hizCulled++;
                        if (aabb != null && !hiZBuffer.TestVisibility(vp, view, (AABB3f)aabb))
                        {
                            _streamingManager.SetTileAABBColor(coord.x, coord.y, new Vertex4f(1, 0, 0, 1));
                            continue;
                        }
                    }

                    // 모든 컬링을 통과한 타일을 렌더링 리스트에 추가
                    ref TerrainLowCoord r = ref _renderLowCoords[typeIdx][_renderLowCounts[typeIdx]++];
                    r = coord;
                }
            }

            int totalLow = 0;
            for (int i = 0; i < 5; i++) totalLow += _travLowCounts[i];
            int totalLowRendered = totalLow - frustumCulled - hizCulled;
            int totalRendered = _renderHighCount + totalLowRendered;

            if (totalRendered != _lastRenderedCount)
            {
                _lastRenderedCount = totalRendered;
                Console.WriteLine($"[Terrain] 렌더링: {totalRendered}/49  " +
                                  $"(고해상도: {_renderHighCount}/9  " +
                                  $"저해상도: {totalLowRendered}/{totalLow}  " +
                                  $"프러스텀컬링: {frustumCulled}  " +
                                  $"HiZ컬링: {hizCulled})");
            }
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

        /// <summary>
        /// 리전 기준 전체 후보 리스트를 갱신한다. (컬링 없음)
        /// </summary>
        private void UpdateRegionTravCoords()
        {
            int tileLowRadius = _streamingManager.LowTileRadius;
            int originX = _streamingManager.CurrentRegionX;
            int originY = _streamingManager.CurrentRegionY;

            _travHighCount = 0;
            for (int i = 0; i < 5; i++) _travLowCounts[i] = 0;

            for (int dx = -tileLowRadius; dx <= tileLowRadius; dx++)
            {
                for (int dy = -tileLowRadius; dy <= tileLowRadius; dy++)
                {
                    int absX = Math.Abs(dx);
                    int absY = Math.Abs(dy);
                    int cx = originX + dx;
                    int cy = originY + dy;

                    if (absX <= 1 && absY <= 1)
                    {
                        ref TerrainHighCoord h = ref _travTerrainHighCoords[_travHighCount++];
                        h.x = cx; h.y = cy; h.dx = dx; h.dy = dy;
                    }
                    else
                    {
                        int typeIdx = GetLowTileType(dx, dy, absX, absY);
                        ref TerrainLowCoord l = ref _travTerrainLowCoords[typeIdx][_travLowCounts[typeIdx]++];
                        l.x = cx; l.y = cy; l.dx = dx; l.dy = dy;
                    }
                }
            }
        }

        /// <summary>
        /// 타일의 AABB가 뷰 프러스텀 안에 있는지 검사한다.
        /// AABB가 아직 로드되지 않은 타일은 일단 보이는 것으로 처리한다.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool IsTileVisible(int cx, int cy, Plane[] frustumPlanes)
        {
            if (frustumPlanes == null) return true;

            AABB3f? aabb = _streamingManager.GetTileAABB(cx, cy);
            if (aabb == null) return true;  // 미로드 타일은 컬링하지 않음

            return ((AABB3f)aabb).Visible(frustumPlanes);
        }

        /// <summary>
        /// 저해상도 타일의 VAO 타입 인덱스를 반환한다.
        /// 타입 1(+X 인접), 2(+Y 인접), 3(-X 인접), 4(-Y 인접), 0(외곽)
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static int GetLowTileType(int dx, int dy, int absX, int absY)
        {
            if (absX == 2 && absY <= 1) return dx > 0 ? 1 : 3;
            if (absY == 2 && absX <= 1) return dy > 0 ? 2 : 4;
            return 0;  // 외곽 전체 (absX>=3 or absY>=3 or 모서리 ±2,±2)
        }

        public void RenderTerrain(Camera camera)
        {
            Matrix4x4f vp = camera.VPMatrix;
            Matrix4x4f view = camera.ViewMatrix;

            // ── 셰이더 바인드 & 공통 유니폼 (1회) ──
            _shader.Bind();
            _shader.LoadTime(_time);
            _shader.LoadDetailMap(_detailTexture == null ? 0 : _detailTexture.TextureID);
            _shader.LoadTerrainTextures(
                _groundTextures[0].TextureID, _groundTextures[1].TextureID,
                _groundTextures[2].TextureID, _groundTextures[3].TextureID,
                _groundTextures[4].TextureID);
            _shader.LoadMossRockTexture(_mossRockTexture.TextureID);
            _shader.LoadRockTexture(_rockTexture.TextureID);
            _shader.LoadFaultMap(_faultTexture.TextureID);
            _shader.LoadFaultParameters(_faultScale, _displacement, _zoneWidth, _intensity);
            _shader.LoadIsDetailMap(true);
            _shader.LoadHeightScale(Constants.TERRAIN_VERTICAL_SCALE);
            _shader.LoadBlendFactor(0.0f);
            _shader.LoadNormalMatrix(Matrix3x3f.Identity);
            Gl.PatchParameter(PatchParameterName.PatchVertices, 4);

            // ── 패스 1: 고해상도 타일 (3×3 = 9개 고정) ──
            Gl.BindVertexArray(_vao);
            Gl.EnableVertexAttribArray(0);
            Gl.EnableVertexAttribArray(1);
            Gl.EnableVertexAttribArray(2);
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo);

            // RenderTerrain 패스 1
            for (int i = 0; i < _renderHighCount; i++)
            {
                ref TerrainHighCoord coord = ref _renderHighCoords[i];
                DrawTerrainTile(coord.x, coord.y, coord.dx, coord.dy, _count, vp, view);
            }

            // RenderTerrain 패스 2
            for (int typeIdx = 0; typeIdx < 5; typeIdx++)
            {
                int tileCount = _renderLowCounts[typeIdx];  // ← _render
                if (tileCount == 0) continue;

                Gl.BindVertexArray(_vao1[typeIdx]);
                Gl.EnableVertexAttribArray(0);
                Gl.EnableVertexAttribArray(1);
                Gl.EnableVertexAttribArray(2);
                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _ibo1[typeIdx]);

                int count = _count1[typeIdx];
                for (int i = 0; i < tileCount; i++)
                {
                    ref TerrainLowCoord coord = ref _renderLowCoords[typeIdx][i];  // ← _render
                    DrawTerrainTile(coord.x, coord.y, coord.dx, coord.dy, count, vp, view);
                }
            }

            // ── 정리 ──
            Gl.DisableVertexAttribArray(2);
            Gl.DisableVertexAttribArray(1);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);
            _shader.Unbind();


            // RenderTerrain 패스 2
            /*
            _boxShader.Bind();
            for (int typeIdx = 0; typeIdx < 5; typeIdx++)
            {
                int tileCount = _renderLowCounts[typeIdx];  // ← _render
                if (tileCount == 0) continue;

                int count = _count1[typeIdx];
                for (int i = 0; i < tileCount; i++)
                {
                    ref TerrainLowCoord coord = ref _renderLowCoords[typeIdx][i];  // ← _render
                    AABB3f aabb = _streamingManager.GetTileAABB(coord.x, coord.y);
                    _boxShader.RenderAABB((AABB3f)aabb, camera, aabb.Color);
                }
            }
            _boxShader.Unbind();
            */

            // 이전 폴리곤 모드 저장
            int[] prevPolygonMode = new int[2];
            Gl.GetInteger(GetPName.PolygonMode, 0, out prevPolygonMode[0]);
            Gl.GetInteger(GetPName.PolygonMode, 1, out prevPolygonMode[1]);

            Gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            _boxShader.Bind();
            for (int i = 0; i < _travTerrainLowCoords.Count; i++)
            {
                TerrainLowCoord[] coords = _travTerrainLowCoords[i];
                foreach (TerrainLowCoord coord in coords)
                {
                    AABB3f aabb = _streamingManager.GetTileAABB(coord.x, coord.y);
                    _boxShader.RenderAABB((AABB3f)aabb, camera, aabb.Color);
                }
            }
            _boxShader.Unbind();

            // 이전 폴리곤 모드 복원
            Gl.PolygonMode(MaterialFace.FrontAndBack, (PolygonMode)prevPolygonMode[1]);

        }

        private void DrawTerrainTile(int cx, int cy, int dx, int dy, int count, Matrix4x4f vp, Matrix4x4f view)
        {
            uint textureId = _streamingManager.GetRegionTexture(cx, cy, dx, dy);
            SetRegion(cx, cy, textureId);

            _shader.LoadHeightHighResolutionMap(_heightMapTextureId);
            _shader.LoadHeightLowResolutionMap(_heightMapTextureId);
            _shader.LoadAdjacentHeightMaps(_streamingManager.GetAdjRegionTextures(cx, cy));
            _shader.LoadRiverRoadMap(_streamingManager.GetRiverRoadTexture(cx, cy));
            _shader.LoadModelMatrix(_worldMatrix);
            _shader.LoadNormalMap(_streamingManager.GetRegionNormalTexture(cx, cy));

            Gl.DrawElements(PrimitiveType.Patches, count, DrawElementsType.UnsignedInt, IntPtr.Zero);
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

        public TerrainDepthRenderData BuildDepthRenderData()
        {
            var tiles = new (uint, Matrix4x4f)[9];
            int idx = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int cx = _streamingManager.CurrentRegionX + dx;
                    int cy = _streamingManager.CurrentRegionY + dy;

                    uint textureId = _streamingManager.GetRegionTexture(cx, cy, dx, dy);
                    SetRegion(cx, cy, textureId); // _worldMatrix, _heightMapTextureId 갱신

                    tiles[idx++] = (_heightMapTextureId, _worldMatrix);
                }
            }

            return new TerrainDepthRenderData
            {
                VAO = _vao,
                IBO = _ibo,
                Count = _count,
                Tiles = tiles
            };
        }

        // --------------------------------------------------------
        // 내부 사용 지형 좌표 구조체
        // --------------------------------------------------------

        struct TerrainHighCoord
        {
            public int x; // 타일 좌표 X
            public int y; // 타일 좌표 Y
            public int dx; // 중심 타일로부터의 상대 좌표 X (-1, 0, 1)
            public int dy; // 중심 타일로부터의 상대 좌표 Y (-1, 0, 1)
        }

        struct TerrainLowCoord
        {
            public int x; // 타일 좌표 X
            public int y; // 타일 좌표 Y
            public int dx; // 중심 타일로부터의 상대 좌표 X (-1, 0, 1)
            public int dy; // 중심 타일로부터의 상대 좌표 Y (-1, 0, 1)
        }

    }


}