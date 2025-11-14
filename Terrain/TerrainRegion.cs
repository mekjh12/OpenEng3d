using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Terrain
{
    /// <summary>
    /// 지형 시스템을 총괄하는 메인 클래스입니다.
    /// 높이맵 기반의 지형 생성, 텍스처 관리, 가시성 처리 및 렌더링을 담당합니다.
    /// </summary>
    public class TerrainRegion
    {
        // 핵심 컴포넌트들
        private TerrainData _terrainData;                // 높이맵 기반의 지형 데이터를 관리하는 컴포넌트
        private TerrainCulling _terrainCulling;          // 지형 패치들의 가시성 처리를 담당하는 컴포넌트
        private TerrainPatchFactory _terrainPatchFactory; // 지형 패치들의 생성과 초기화를 담당하는 팩토리 컴포넌트
        private Entity _terrainEntity;                    // 전체 지형을 대표하는 단일 엔티티
        private EntityBatchProcessor _terrainBatchProcessor;
        private RegionState _regionState = RegionState.Unloaded;
        private RegionCoord _regionCoord;                 // 리전 좌표
        private int _totalDivideCount = 0;                // 지형을 가로, 세로 균등하게 청크로 나눈 갯수
        private int _chunkSize = 100;                     // 청크의 가로, 세로의 크기
        private float _regionSize = 2000;                 // 리전의 크기
        private float _regionHalfSize;
        private int _n;

        // 텍스처 전환 블렌딩을 위한 변수들
        private float _blendFactor = 0.0f;        // 블렌딩 계수 (0: 저해상도, 1: 고해상도)
        private bool _isBlending = false;         // 현재 블렌딩 중인지 여부
        private float _blendDuration = 1.5f;      // 블렌딩에 걸리는 시간(초)
        private float _blendTimer = 0.0f;         // 경과 시간 추적
        private TerrainChunkShader _terrainChunkShader;

        private Vertex3f _regionOffset;                   // 리전의 월드좌표 오프셋
        private VegetationMap _vegetationMap;
        private int _entityCount = 0;
        private Texture _lowTerrainHeightMap;
        private Texture _highTerrainHeightMap;
        private ChunkCreator _chunkCreator;
        private Action _onCompleted;

        /// <summary>지형의 전체 가로 크기(픽셀 단위)</summary>
        public int Width => _terrainData.Width;
        /// <summary>지형의 전체 세로 크기(픽셀 단위)</summary>
        public int Height => _terrainData.Height;
        /// <summary>지형의 실제 크기(월드 좌표계 단위)</summary>
        public float Size => _terrainData.Size;
        /// <summary>전체 지형 엔티티</summary>
        public Entity TerrainEntity { get => _terrainEntity; set => _terrainEntity = value; }
        /// <summary>현재 프레임에서 보이는 패치들</summary>
        public List<Chunk> FrustumedChunk => ((ITerrainCulling)_terrainCulling).FrustumedChunk;
        /// <summary>현재 프레임에서 보이는 패치들의 AABB</summary>
        public List<AABB> FrustumedChunkAABB => ((ITerrainCulling)_terrainCulling).FrustumedChunkAABB;
        /// <summary>공간 분할 구조</summary>
        public BVH BoundingVolumeHierarchy => ((ITerrainCulling)_terrainCulling).BoundingVolumeHierarchy;
        /// <summary>식생맵</summary>
        public VegetationMap VegetationMap { get => _vegetationMap; set => _vegetationMap = value; }
        /// <summary>엔티티 개수</summary>
        public int EntityCount { get => _entityCount; set => _entityCount = value; }
        /// <summary>리전의 현재 상태</summary>
        public RegionState RegionState { get => _regionState; set => _regionState = value; }
        /// <summary>리전의 좌표</summary>
        public RegionCoord RegionCoord { get => _regionCoord; set => _regionCoord = value; }
        /// <summary>지형 배치 프로세서</summary>
        public EntityBatchProcessor TerrainBatchProcessor { get => _terrainBatchProcessor; set => _terrainBatchProcessor = value; }
        /// <summary>지형 데이터</summary>
        public TerrainData TerrainData { get => _terrainData; }
        public float BlendFactor { get => _blendFactor; }
        public float RegionSize { get => _regionSize; }

        public TerrainRegion()
        {
            _terrainData = new TerrainData();
            _terrainPatchFactory = new TerrainPatchFactory(_terrainData);
        }

        /// <summary>
        /// TerrainSystem의 새 인스턴스를 초기화합니다.
        /// </summary>
        public TerrainRegion(RegionCoord regionCoord, int chunkSize, int n, TerrainChunkShader terrainChunkShader)
        {
            _regionState = RegionState.Unloaded;
            _regionCoord = regionCoord;
            _chunkSize = chunkSize;
            _n = n;
            _regionHalfSize = _chunkSize * _n;
            _regionSize = _regionHalfSize * 2.0f;
            _totalDivideCount = 2 * n;
            _terrainChunkShader = terrainChunkShader;

            _chunkCreator = new ChunkCreator();
            _terrainData = new TerrainData();
            _terrainCulling = new TerrainCulling();
            _terrainPatchFactory = new TerrainPatchFactory(_terrainData);
            _terrainBatchProcessor = new EntityBatchProcessor(1);
        }

        /// <summary>
        /// 고해상도맵을 비동기로 로딩한다.
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="heightmapBasePath"></param>
        /// <returns></returns>
        public async Task LoadTerrainHighResMap(RegionCoord coord, string heightmapBasePath, Action completed)
        {
            _terrainData.InitializeHighResLoading(coord, heightmapBasePath);

            // 큐가 빌 때까지 타일들을 순차적으로 로드
            while (!_terrainData.IsAllTilesLoaded())
            {
                await _terrainData.LoadNextTile(_n, _chunkSize);
            }

            // 고해상도맵 사용 설정
            _terrainData.IsHighResLoaded = true;

            // 큐가 비어 완료되면 완료 함수를 콜백
            completed?.Invoke();

            // 모든 고해상도 타일 로딩이 완료되면 블렌딩 시작
            StartHighResTextureBlending();
        }

        /// <summary>
        /// 지형 시스템을 초기화하고 저해상도 리소스를 로드합니다.
        /// </summary>
        public async void LoadTerrainLowResMap(RegionCoord coord, string heightmapLowResFileName, Action completed = null, bool chunkEnable = true)
        {
            _regionState = RegionState.Loading;
            _onCompleted = completed;

            // 리전 초기화
            InitializeRegionParameters(coord);

            // 높이맵 텍스처 로드
            await LoadHeightMapTexture(heightmapLowResFileName);

            // 단일 대표 지형 엔티티 생성
            CreateTerrainEntity(coord);

            // 청크 생성 시작
            if (chunkEnable)
            {
                StartChunkCreation(coord);
            }
        }

        /// <summary>
        /// 리전의 기본 파라미터를 초기화합니다.
        /// </summary>
        private void InitializeRegionParameters(RegionCoord coord)
        {
            // 패치 크기를 위한 설정
            _regionSize = _chunkSize * _totalDivideCount;
            _regionOffset = new Vertex3f(coord.X * _regionSize, coord.Y * _regionSize, 0);

            // 식생맵을 초기화하는 코드는 현재 주석 처리됨
            // _vegetationMap = new VegetationMap(_regionWidth, _regionWidth);
        }

        /// <summary>
        /// 높이맵 텍스처를 로드합니다.
        /// </summary>
        private async Task LoadHeightMapTexture(string heightmapFileName)
        {
            // 높이맵 파일 존재 검증
            if (!File.Exists(heightmapFileName))
            {
                // 파일이 존재하지 않을 경우 예외 처리 가능
                // 현재는 주석 처리되어 있음
            }

            if (_terrainData == null)
            {
                _terrainData = new TerrainData();
            }

            // 저해상도 높이맵 텍스처로부터 지형 데이터 초기화
            using (Bitmap bitmap = await _terrainData.LoadFromFile(heightmapFileName, _n, _chunkSize))
            {
                _terrainData.HeightMapTextureLowRes = (bitmap == null) ?
                    TextureStorage.DebugTexture : new Texture(bitmap);
            }
        }

        /// <summary>
        /// 단일 대표 지형 엔티티를 생성합니다.
        /// </summary>
        private void CreateTerrainEntity(RegionCoord coord)
        {
            // 단일 대표 지형 엔티티 생성 - LOD 시스템의 최하위 레벨용
            _terrainEntity = _terrainPatchFactory.CreateUnifiedTerrainEntity(
                _n, _chunkSize, _terrainData.HeightMapTextureLowRes, coord.X, coord.Y);

            // 단일 대표 지형의 AABB 바운딩 계산 및 설정

            // 리전의 좌하단 좌표 계산
            float terrainRegionLeftBottomX = _regionSize * coord.X - _regionHalfSize;
            float terrainRegionLeftBottomY = _regionSize * coord.Y - _regionHalfSize;

            // AABB 범위 계산
            Vertex3f lower = new Vertex3f(
                terrainRegionLeftBottomX,
                terrainRegionLeftBottomY,
                TerrainConstants.DEFAULT_VERTICAL_SCALE * _terrainData.MinHeight);

            Vertex3f upper = new Vertex3f(
                terrainRegionLeftBottomX + _regionSize,
                terrainRegionLeftBottomY + _regionSize,
                TerrainConstants.DEFAULT_VERTICAL_SCALE * _terrainData.MaxHeight);

            // AABB 객체 재활용 또는 새로 생성
            if (_terrainEntity.AABB == null)
            {
                _terrainEntity.AABB = new AABB(lower, upper);
            }
            else
            {
                // 재할당 방지를 위하여 재활용
                _terrainEntity.AABB.LowerBound = lower;
                _terrainEntity.AABB.UpperBound = upper;
            }
        }

        /// <summary>
        /// 청크 생성을 시작하고 완료 콜백을 설정합니다.
        /// </summary>
        private void StartChunkCreation(RegionCoord coord)
        {
            // 청크 리스트 초기화
            _terrainCulling.DicChunkList.Clear();
            _chunkCreator.Reset();

            // 청크 생성 시작 (비동기)
            _chunkCreator.StartCreatingChunks(coord, _n, _chunkSize, _terrainData, () =>
            {
                // 청크 생성 결과 가져오기
                List<AABB> chunks = _chunkCreator.GetResult();

                _terrainCulling.Clear();

                // 청크별 AABB 설정 및 컬링 시스템 등록
                foreach (AABB aabb in chunks)
                {
                    int j = (int)aabb.Index.x + _n;
                    int i = (int)aabb.Index.y + _n;
                    string chunkName = $"chunk_{j}x{i}";

                    Chunk chunk = new Chunk(chunkName, aabb);
                    _terrainCulling.AddAABB(aabb);
                    _terrainCulling.DicChunkList.Add(chunkName, chunk);
                }

                // 리전 상태 업데이트
                _regionState = RegionState.Active;

                // 완료 콜백 호출
                _onCompleted?.Invoke();
            });
        }

        /// <summary>
        /// 저해상도맵을 고해상도맵으로 바꾼다.
        /// </summary>
        public void SwapMapTextureBuffer()
        {
            _terrainData.SwapMapTextureBuffer();
        }

        /// <summary>
        /// 물체가 위치하는 청크에 추가한다.
        /// </summary>
        /// <param name="entity"></param>
        public void AddEntity(Entity entity)
        {
            // 물체가 위치한 청크의 인덱스를 구한다.
            int size = _terrainData.Width;
            int half = size / 2;
            Vertex3f position = entity.Position - new Vertex3f(_regionCoord.X * _regionSize, _regionCoord.Y * _regionSize, 0);
            int a = (int)((position.x + half) / _chunkSize);
            int b = (int)((position.y + half) / _chunkSize);
            int index = a * _totalDivideCount + b;

            // 청크에 물체를 추가한다.
            Chunk chunk = _terrainCulling.GetChunk($"chunk_{a}x{b}");
            chunk.AddEntity(entity);

            // 추가한 물체의 위치와 크기를 식생맵에 수정한다.
            _vegetationMap.AddVegetation(entity.Position.x + half, entity.Position.y + half, entity.AABB.Size.z * 0.1f);

            _entityCount++;
        }

        /// <summary>
        /// 렌더링 시스템의 상태를 업데이트합니다.
        /// </summary>
        public void Update(Camera camera, Polyhedron viewFrustum, HierarchicalZBuffer zbuffer,
            float duration, TerrainChunkShader terrainChunkShader)
        {
            // ---------------------------------------------------------
            // 컬링 업데이트
            // ---------------------------------------------------------
            Matrix4x4f vp = camera.VPMatrix;
            Matrix4x4f view = camera.ViewMatrix;

            // 이전 프레임의 가시성 정보 초기화
            _terrainCulling.ClearBackLink();

            // 리전의 일정 이상의 큰 AABB로 빠르게 컬링
            //int travCount =_terrainCulling.CullingFastLargeAABBTestByHierarchicalZBuffer(zbuffer, vp, view);

            //RegionManager.DEBUG_STRING += $"{_regionCoord}({travCount}) ";

            // 뷰프러스텀 기반 1차 컬링
            _terrainCulling.CullingTestByViewFrustum(viewFrustum);

            // 계층적 Z버퍼 기반 2차 컬링
            _terrainCulling.CullingTestByHierarchicalZBuffer(zbuffer, vp, view);

            // 최종 가시 패치 목록 구성
            _terrainCulling.PrepareEntities();

            if (!_chunkCreator.IsComplete)
            {
                // 명시적으로 Update 호출하여 상태 업데이트
                _chunkCreator.Update();
            }

            // ---------------------------------------------------------
            // 저해상도에서 고해상도의 전환 업데이트
            // ---------------------------------------------------------

            // 저해상도에서 고해상도로의 블렌딩 상태 업데이트 (전환 감지하여)
            UpdateBlending(duration, terrainChunkShader);

            // 저해상도에서 고해상도로의 텍스처 업데이트 처리 (전환 감지하여)
            _terrainData.UpdateTexturesOnMainThread((int)_regionSize);
        }

        /// <summary>
        /// 텍스처 블렌딩 상태를 업데이트합니다.
        /// </summary>
        private void UpdateBlending(float duration, TerrainChunkShader terrainChunkShader)
        {
            if (_isBlending)
            {
                _blendTimer += duration;
                _blendFactor = Math.Min(_blendTimer / _blendDuration, 1.0f);

                // 셰이더에 블렌딩 계수 전달
                terrainChunkShader.LoadUniform(TerrainChunkShader.UNIFORM_NAME.blendFactor, _blendFactor);

                // 블렌딩 완료 체크
                if (_blendFactor >= 1.0f)
                {
                    _isBlending = false;

                    // 블렌딩 완료 후 저해상도 텍스처 해제도 가능
                    // _terrainData.ReleaseLowResTexture();
                    SwapMapTextureBuffer();
                }
            }
        }

        /// <summary>
        /// 고해상도 텍스처가 로드되면 블렌딩 효과를 시작합니다.
        /// </summary>
        public void StartHighResTextureBlending()
        {
            if (_terrainData.IsHighResLoaded == true && _isBlending == false)
            {
                _isBlending = true;
                _blendTimer = 0.0f;
                _blendFactor = 0.0f;

                // 셰이더에 두 텍스처 모두 바인딩 및 블렌드 인자 초기화
                UpdateShaderTextureBindings();
            }
        }

        /// <summary>
        /// 셰이더에 저해상도와 고해상도 텍스처를 모두 바인딩합니다.
        /// </summary>
        private void UpdateShaderTextureBindings()
        {
            
        }

        /// <summary>
        /// 주어진 월드 좌표에서의 지형 높이를 계산합니다.
        /// </summary>
        /// <param name="positionInRegionSpace">리전좌표공간의 위치</param>
        /// <returns>해당 위치의 보간된 지형 높이를 포함한 3D 좌표</returns>
        public Vertex3f GetTerrainHeightVertex3f(Vertex3f positionInRegionSpace)
        {
            return _terrainData.GetTerrainHeightVertex3f(positionInRegionSpace);
        }

        /// <summary>
        /// 청크내의 모든 물체를 리스트로 가져온다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<Entity> GetVisibleEntities(int n)
        {
            if (_terrainCulling.FrustumedChunkAABB == null) return null;

            List<Entity> entities = new List<Entity>();

            foreach (AABB aabb in _terrainCulling.FrustumedChunkAABB)
            {
                int j = (int)aabb.Index.x + n;
                int i = (int)aabb.Index.y + n;
                string chunkName = $"chunk_{j}x{i}";

                if (_terrainCulling.DicChunkList.ContainsKey(chunkName))
                {
                    Chunk chunk = _terrainCulling.DicChunkList[chunkName];
                    entities.AddRange(chunk.DicEntityCache.Values);
                }
            }

            return entities;
        }

        /// <summary>
        /// 리셋한다.
        /// </summary>
        public void Reset()
        {
            // 재사용하므로 꼭 리셋을 해준다.
            _terrainData?.Reset();
        }

        /// <summary>
        /// 청크를 가져온다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Chunk GetChunk(string name)
        {
            return _terrainCulling.GetChunk(name);
        }

        /// <summary>
        /// 청크의 모든 초목의 정보를 GPU로 전송한다.
        /// </summary>
        public void UploadVegatation()
        {
            foreach (KeyValuePair<string, Chunk> item in _terrainCulling.DicChunkList)
            {
                Chunk chunk = item.Value;
                chunk.UploadVegetation();
            }
        }

        /// <summary>
        /// 지형을 렌더링합니다.
        /// </summary>
        public void Render(TerrainTessellationShader terrainTessellationShader, TerrainImposterShader terrainImposterShader,
            Camera camera, Vertex3f lightDirection)
        {
            Gl.Disable(EnableCap.Blend);

            // 각 가시 패치 렌더링
            foreach (Entity entity in _terrainCulling.FrustumedEntities)
            {
                Renderer3d.RenderByTerrainTessellationShader(terrainTessellationShader,
                    entity,
                    camera,
                    RegionManager.Textures,
                    RegionManager.DetailMap,
                    RegionManager.IsDetailMap,
                    lightDirection,
                    0); //_vegetationMap.Texture
            }

            // 블렌딩 설정 복원
            Gl.Enable(EnableCap.Blend);
            Gl.BlendEquation(BlendEquationMode.FuncAdd);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public void Render(VegetationBillboardShader shader, Camera camera, uint texture)
        {
            Gl.Disable(EnableCap.Blend); // 블렌딩 비활성화 (필요한 경우 활성화 가능)

            shader.Bind(); // 쉐이더 활성화

            // 뷰 & 투영 행렬을 쉐이더에 전달
            shader.LoadUniform(VegetationBillboardShader.UNIFORM_NAME.vp, camera.VPMatrix);
            shader.LoadUniform(VegetationBillboardShader.UNIFORM_NAME.gCameraPos, camera.Position);
            shader.LoadTexture(VegetationBillboardShader.UNIFORM_NAME.gColorMap, TextureUnit.Texture0, texture);

            Gl.BindVertexArray((FrustumedChunk[0] as Chunk).VegetationVAO); // VAO 바인딩

            // **뷰 프러스텀을 통과한 청크만 렌더링**
            foreach (Chunk chunk in FrustumedChunk)
            {
                uint vbo = chunk.VegetationVBO;
                int vegetationCount = chunk.VegetationCount;

                Gl.BindBuffer(BufferTarget.ArrayBuffer, vbo); // VBO 바인딩
                Gl.EnableVertexAttribArray(0);
                Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 3 * sizeof(float), IntPtr.Zero);
                Gl.VertexAttribDivisor(0, 1); // Instanced Rendering 설정

                // **한 번의 Draw Call로 청크 내 모든 나무 렌더링**
                Gl.DrawArraysInstanced(PrimitiveType.Points, 0, 1, vegetationCount);

                Gl.DisableVertexAttribArray(0);
            }

            Gl.BindVertexArray(0); // VAO 바인딩 해제
            shader.Unbind(); // 쉐이더 비활성화

            Gl.Enable(EnableCap.Blend); // 블렌딩 다시 활성화
        }

        public override string ToString()
        {
            return $"region={_regionCoord.X}x{_regionCoord.Y}";
        }

        #region 디버깅용

        public Vertex3f ConvertToRegionSpace(Vertex3f position)
        {
            return new Vertex3f(position.x + _regionCoord.X * _regionSize,
                position.y + _regionCoord.Y * _regionSize,
                position.z);
        }

        public void BatchEntity(Entity entity)
        {
            _terrainBatchProcessor.EnqueueEntity(entity);
        }

        public async void StartBatchEntities()
        {
            await _terrainBatchProcessor.StartProcessing();
        }

        public void _DEBUG_NonVisibleAll()
        {
            _terrainCulling.ClearBackLink(false);
            _terrainCulling.PrepareEntities();
        }

        public void _DEBUG_Print()
        {
            _terrainCulling.BoundingVolumeHierarchy.Print();
        }

        public string _DEBUG_CheckChunk()
        {
            string txt = "";
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Chunk chunk = _terrainCulling.GetChunk($"chunk_{j}x{i}");
                    txt += $"({chunk.DicEntityCache.Count})";
                    Console.WriteLine(chunk.Name + "---------------------");
                    foreach (Entity entity in chunk.DicEntityCache.Values)
                    {
                        Console.WriteLine(entity.Name + " " + entity.Position);
                    }
                }
                txt += "\n";
            }
            return txt;
        }


        #endregion
    }
}