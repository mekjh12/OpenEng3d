using Common.Abstractions;
using Geometry;
using Lights;
using Model3d;
using Occlusion;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ZetaExt;

namespace Terrain
{
    /// <summary>지형 관련 상수</summary>
    public static class TerrainConstants
    {
        /// <summary>높이 스케일 상수</summary>
        public const float DEFAULT_VERTICAL_SCALE = 200.0f;
    }

    public enum RegionState
    {
        Unloaded,   // 완전히 언로드된 상태
        Loading,    // 리전 데이터 로딩 중
        Active,     // 활성화되어 렌더링 가능한 상태
        Unloading,  // 리전 데이터 언로드 중
        UnActive,   // 로딩되었지만 비활성화
    }

    /// <summary>
    ///  
    /// 
    /// </summary>
    /// <remarks>
    /// RegionManager에서 사용하지 않는 리전을 풀에 반환하지 않고,
    /// RecentRegionCache에 저장하도록 수정, LoadRegion에서 캐시 우선 확인 후 재사용
    /// </remarks>
    public class RegionManager
    {
        // 상수
        private static readonly int SEPERATE_CHUNK_COUNT = 20;                     // 청크 분할 수
        private static readonly int SEPERATE_CHUNK_SIZE = 100;                     // 각 청크의 크기
        private readonly int REGION_SIZE = 0;                                      // 리전의 전체 크기
        private readonly int REGION_HALF_SIZE = 0;                                 // 리전의 절반 크기

        // 정적 변수
        public static string DEBUG_STRING = "";                                    // 디버깅용 문자열
        private static Texture[] _textures;                                        // 지형 텍스처 배열 [0]: 기본, [1-4]: 블렌딩 레이어
        private static Texture _detailMap;                                         // 근거리 디테일용 노멀/디테일맵
        private static bool _isDetailMap;                                          // 디테일맵 사용 여부
        public static RawModel3d TerrainPlaneNxN                                   // 지형 평면 모델
            = Loader3d.LoadPlaneNxN(SEPERATE_CHUNK_COUNT / 2, SEPERATE_CHUNK_SIZE);
        public static RawModel3d TerrainPlane1x1                                   // 지형 평면 모델
            = Loader3d.LoadPlaneNxN(1, 1000);

        // 리전 관리 컬렉션
        private readonly Dictionary<RegionCoord, TerrainRegion> _regions;          // 활성화된 리전 목록
        private readonly Queue<TerrainRegion> _regionPool;                         // 재사용 대기 리전풀
        private readonly RecentRegionCache _recentRegions;                         // 최근 사용된 리전 캐시
        private RegionCoord _currentRegion;                                        // 현재 위치한 리전
        private readonly string _heightmapBasePath;                                // 높이맵 기본 경로
        private readonly SimpleTerrainRegionCache _simpleTerrainRegionCache;       // 단순지형 캐시
        private List<SimpleTerrainRegion> _outerVisibleSimpleRegion;               // 가시단순지형 리스트

        // 렌더링 관련
        private readonly Model3dManager _model3dManager;                            // 3D 모델 관리자
        private List<TerrainRegion> _visibleRegionsCache;                           // 보이는 리전 캐시
        private List<Entity> _visibleEntities;                                      // 보이는 엔티티 목록
        private SunLight _sunLight;                                                 // 태양광 설정
        private readonly TerrainChunkShader _terrainChunkShader;                    // 지형 청크 쉐이더

        /// <summary>지형에 사용되는 텍스처 배열을 반환합니다.</summary>
        public static Texture[] Textures => _textures;

        /// <summary>디테일맵 텍스처를 반환합니다.</summary>
        public static Texture DetailMap => _detailMap;

        /// <summary>디테일맵 사용 여부를 설정하거나 반환합니다.</summary>
        public static bool IsDetailMap { get => _isDetailMap; set => _isDetailMap = value; }

        /// <summary>모든 TerrainRegion 목록을 반환합니다.</summary>
        public List<TerrainRegion> TerrainRegions => _regions.Values.ToList();

        /// <summary>현재 설정된 태양광을 반환합니다.</summary>
        public SunLight SunLight => _sunLight;

        /// <summary>뷰 영역에 보이는 리전 목록을 반환합니다.</summary>
        public List<TerrainRegion> VisibleRegions => _visibleRegionsCache;

        /// <summary>현재 위치한 TerrainRegion을 반환합니다.</summary>
        public TerrainRegion CurrentTerrainRegion => _currentRegion != null && _regions.TryGetValue(_currentRegion, out var region) ? region : null;

        /// <summary>현재 위치한 리전의 좌표를 반환합니다.</summary>
        public RegionCoord CurrentRegionCoord => CurrentTerrainRegion.RegionCoord;

        /// <summary>리전 풀을 반환합니다.</summary>
        public Queue<TerrainRegion> RegionPool => _regionPool;

        public SimpleTerrainRegionCache SimpleTerrainRegionCache => _simpleTerrainRegionCache;

        public List<SimpleTerrainRegion> OuterVisibleSimpleRegion => _outerVisibleSimpleRegion;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="model3DManager"></param>
        /// <param name="shaderBasePath"></param>
        /// <param name="heightmapBasePath"></param>
        public RegionManager(Model3dManager model3DManager,
            string shaderBasePath,
            string heightmapBasePath)
        {
            REGION_HALF_SIZE = SEPERATE_CHUNK_SIZE * SEPERATE_CHUNK_COUNT / 2;
            REGION_SIZE = REGION_HALF_SIZE * 2;

            _regions = new Dictionary<RegionCoord, TerrainRegion>();
            _regionPool = new Queue<TerrainRegion>();
            _heightmapBasePath = heightmapBasePath;
            _model3dManager = model3DManager;
            _recentRegions = new RecentRegionCache();

            // 중심부 밖 리전 관리
            _simpleTerrainRegionCache = new SimpleTerrainRegionCache(_heightmapBasePath, REGION_SIZE);
            _outerVisibleSimpleRegion = new List<SimpleTerrainRegion>();

            _textures = new Texture[5];
            _isDetailMap = false;

            //_terrainImposterShader = new TerrainImposterShader(shaderBasePath);
            //_terrainTessellationShader = new TerrainTessellationShader(shaderBasePath);
            _terrainChunkShader = new TerrainChunkShader(shaderBasePath);

            _visibleRegionsCache = new List<TerrainRegion>(0);
        }

        /// <summary>태양광을 설정합니다.</summary>
        /// <param name="sunLight">설정할 태양광 객체</param>
        public void SetSunLight(SunLight sunLight)
        {
            _sunLight = sunLight;
        }

        /// <summary>
        /// 지형 텍스처 배열과 디테일맵을 로드합니다.
        /// </summary>
        /// <param name="texturePaths">텍스처 파일 경로 배열</param>
        /// <param name="detailMapPath">디테일맵 텍스처 파일 경로</param>
        public void LoadTerrainTextures(string[] texturePaths, string detailMapPath)
        {
            if (texturePaths == null) return;
            for (int i = 0; i < Math.Min(texturePaths.Length, _textures.Length); i++)
            {
                _textures[i] = new Texture(texturePaths[i]);
                Gl.BindTexture(TextureTarget.Texture2d, _textures[i].TextureID);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.MIRRORED_REPEAT);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.MIRRORED_REPEAT);
                Gl.GenerateMipmap(TextureTarget.Texture2d);
                Gl.BindTexture(TextureTarget.Texture2d, 0);
            }

            if (detailMapPath == null) return;
            _detailMap = new Texture(detailMapPath);

            Gl.BindTexture(TextureTarget.Texture2d, _detailMap.TextureID);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR_MIPMAP_LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.MIRRORED_REPEAT);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.MIRRORED_REPEAT);
            Gl.GenerateMipmap(TextureTarget.Texture2d);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            _isDetailMap = true;
        }

        public uint[] GetAdjacentTerrainTextures(RegionCoord coord)
        {
            // 주변의 8개의 높이맵을 셰이더로 베어링한다.
            uint[] adjacentRegions = new uint[8];

            for (int i = 0; i < 8; i++)
            {
                RegionCoord value = RegionCoord.ADJACENT_REGION_COORDS_EXCEPT_CENTER[i];
                RegionCoord target = coord + value;
                if (_regions.ContainsKey(target))
                {
                    TerrainData terrainData = _regions[target].TerrainData;
                    if (terrainData.IsHighResLoaded)
                    {
                        adjacentRegions[i] = (terrainData.HeightMapTextureHighRes != null) ?
                            terrainData.HeightMapTextureHighRes.TextureID : 0;
                    }
                }
                else
                {
                    adjacentRegions[i] = _simpleTerrainRegionCache.ContainsKey(target) ?
                            _simpleTerrainRegionCache[target].TextureId : 0;
                }
            }

            return adjacentRegions;
        }

        /// <summary>
        /// 가시 영역의 리전들을 렌더링합니다.
        /// 각 리전의 청크들은 지형 쉐이더를 통해 렌더링됩니다.
        /// </summary>
        /// <param name="camera">카메라 객체</param>
        public void Render(Camera camera, SimpleTerrainShader simpleTerrainShader)
        {
            // (1) 주변 9개를 렌더링한다.
            RawModel3d quadPatch = Renderer3d.QaudPatch;

            //Console.WriteLine("--detail-------------------------------------");
            foreach (TerrainRegion terrainRegion in _visibleRegionsCache)
            {
                if (terrainRegion == null) continue;
                if (terrainRegion.FrustumedChunkAABB == null) continue;

                // 주변의 8개의 높이맵을 셰이더로 베어링한다.
                uint[] adjacentRegions = GetAdjacentTerrainTextures(terrainRegion.RegionCoord);
                //Console.Write($"{terrainRegion.RegionCoord}\t");
                //for (int i = 0; i < adjacentRegions.Length; i++)
                {
                    //Console.Write(adjacentRegions[i] + "\t");
                }
                //Console.Write("\r\n");

                Renderer3d.Render(_terrainChunkShader, quadPatch.VAO, quadPatch.IBO,
                    terrainRegion.FrustumedChunkAABB, camera, 
                    _textures, _detailMap, 
                    terrainRegion.TerrainData.HeightMapTextureLowRes,
                    terrainRegion.TerrainData.HeightMapTextureHighRes,
                    adjacentRegions,
                    terrainRegion.BlendFactor,
                    true,
                    _sunLight == null ? -Vertex3f.UnitZ : _sunLight.GetDirection(),
                    0, 
                    SEPERATE_CHUNK_SIZE,
                    SEPERATE_CHUNK_COUNT,
                    TerrainConstants.DEFAULT_VERTICAL_SCALE);
            }

            //Console.WriteLine("--outer-------------------------------------");
            // 외곽 16개를 렌더링한다.
            foreach (SimpleTerrainRegion simpleTerrainRegion in _outerVisibleSimpleRegion)
            {
                // 주변의 8개의 높이맵을 셰이더로 베어링한다.
                uint[] adjacentRegions = GetAdjacentTerrainTextures(simpleTerrainRegion.RegionCoord);

                //Console.Write($"{terrainRegion.RegionCoord}\t");
                for (int i = 0; i < adjacentRegions.Length; i++)
                {
                    //Console.Write(adjacentRegions[i] + "\t");
                }
                //Console.Write("\r\n");

                simpleTerrainRegion.Render(
                    simpleTerrainShader, 
                    camera, 
                    RegionManager.Textures,
                    _sunLight.GetDirection(),
                    adjacentRegions,
                    TerrainConstants.DEFAULT_VERTICAL_SCALE);
            }
        }

        /// <summary>
        /// Hi-Z생성 전에 업데이트한다.
        /// <para>현재 위치에서 9개의 인접 리전을 로딩한다.</para>
        /// <para>가시성 리전만 캐시 처리한다.</para>
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="viewFrustum"></param>
        public void PreUpdate(Camera camera, Polyhedron viewFrustum)
        {
            // 카메라 위치로부터 현재 리전 좌표 계산
            Vertex3f cameraPosition = camera.Position;
            RegionCoord regionCoord = GetRegionCoord(cameraPosition.xy());

            // 리전 전환 감지(위치변경 또는 새롭게 배치) 주변 9개만 해당됨.
            if (_currentRegion == null || !_currentRegion.Equals(regionCoord))
            {
                UpdateRegionLoadState(regionCoord, camera);
            }

            // 16개의 외곽리전을 캐시에 로딩하고 가시성 리전만 단순가시지형리스트에 담는다.
            ProcessOuterSimpleRegion(regionCoord, viewFrustum);

            // 9개의 리전 중에서 가시성 리전만 캐시에 임시보관한다.
            ProcessVisibleRegions(regionCoord, camera, viewFrustum);
        }

        /// <summary>
        /// 16개의 외곽리전을 캐시에 로딩하고 가시성 리전만 단순가시지형리스트에 담는다.
        /// </summary>
        /// <param name="currentRegion"></param>
        /// <param name="viewFrustum"></param>
        private void ProcessOuterSimpleRegion(RegionCoord currentRegion, Polyhedron viewFrustum)
        {
            // 주변 16개의 감지
            List<RegionCoord> outerRegions = RegionCoord.OUTER_REGION_COORDS
                .Select(coord => _currentRegion + coord)
                .ToList();

            // 단순가시지형리스트를 비운다.
            _outerVisibleSimpleRegion?.Clear();

            foreach (RegionCoord coord in outerRegions)
            {
                // 단순가시지형이 캐시되지 않았으면 로딩한다.
                if (!_simpleTerrainRegionCache.ContainsKey(coord))
                {
                    _simpleTerrainRegionCache.AddRegion(coord, TerrainPlane1x1);
                }

                // 리전좌표의 단순지형을 가져온다.
                SimpleTerrainRegion simpleTerrainRegion = _simpleTerrainRegionCache[coord];

                if (simpleTerrainRegion == null) continue;
                if (simpleTerrainRegion.AABB == null) continue;

                // 단순지형좌표가 보이면 단순가시지형리스트에 추가한다.
                if (simpleTerrainRegion.AABB.Visible(viewFrustum.Planes))
                {
                    _outerVisibleSimpleRegion.Add(simpleTerrainRegion);
                }
            }

        }

        /// <summary>
        /// 가시영역에 있는 리전들을 처리하고 캐시에 저장합니다.
        /// 카메라가 속한 리전은 무조건 포함되며, 나머지는 뷰 프러스텀과의 교차 여부로 결정됩니다.
        /// </summary>
        /// <param name="currentRegion">현재 카메라가 위치한 리전 좌표</param>
        /// <param name="camera">카메라 객체</param>
        /// <param name="viewFrustum">시야 프러스텀</param>
        private void ProcessVisibleRegions(RegionCoord currentRegion, Camera camera, Polyhedron viewFrustum)
        {
            _visibleRegionsCache.Clear();

            foreach (KeyValuePair<RegionCoord, TerrainRegion> item in _regions)
            {
                RegionCoord regionCoord = item.Key;
                TerrainRegion terrainRegion = item.Value;

                // 비동기 이슈로 TerrainEntity가 없는 경우 
                if (terrainRegion.TerrainEntity == null) continue;

                // 카메라가 속한 리전은 무조건 추가한다.
                if (currentRegion.Equals(regionCoord))
                {
                    terrainRegion.RegionState = RegionState.Active;
                    _visibleRegionsCache.Add(terrainRegion);
                    continue;
                }
                else
                {
                    terrainRegion.RegionState = RegionState.UnActive;
                }

                // 뷰영역에 접촉이 있는 리전만 추가한다.
                if (terrainRegion.TerrainEntity.AABB.Visible(viewFrustum.Planes))
                {
                    _visibleRegionsCache.Add(terrainRegion);
                }
            }
        }

        /// <summary>
        /// Hi-Z생성 후에 업데이트한다.
        /// <para>
        /// - 리전 전환 감지(위치변경 또는 새롭게 배치) <br/>
        /// - 현재 활성화된 리전들 업데이트 <br/>
        /// </para>
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="viewFrustum"></param>
        /// <param name="zbuffer"></param>
        public void Update(Camera camera, Polyhedron viewFrustum, HierarchicalZBuffer zbuffer, float duration)
        {
            // 현재 활성화된 리전들 업데이트
            foreach (TerrainRegion terrainRegion in _visibleRegionsCache)
            {
                RegionCoord diffRegionCoord = terrainRegion.RegionCoord - _currentRegion;
                
                if (Math.Abs(diffRegionCoord.X) <= 1 && Math.Abs(diffRegionCoord.Y) <= 1)
                {
                    // 현재 위치에서 인접한 리전은 세부적으로 업데이트한다.
                    terrainRegion.Update(camera, viewFrustum, zbuffer, duration, _terrainChunkShader);
                }
                else
                {
                    // 현재 위치의 인접하지 않은 리전은 저해상도의 맵으로 업데이트한다. (그외 리전을 높이데이터가 로딩되지 않음)
                    
                }
            }
            
        }

        /// <summary>
        /// 주어진 월드 좌표에 해당하는 리전 좌표를 계산합니다.
        /// </summary>
        /// <param name="pos">월드 공간의 2D 좌표</param>
        /// <returns>해당 위치의 리전 좌표</returns>
        public RegionCoord GetRegionCoord(Vertex2f pos)
        {
            int newRegionX = (int)Math.Floor((pos.x + REGION_HALF_SIZE) / REGION_SIZE);
            int newRegionZ = (int)Math.Floor((pos.y + REGION_HALF_SIZE) / REGION_SIZE);
            RegionCoord newRegionCoord = new RegionCoord(newRegionX, newRegionZ);
            return newRegionCoord;
        }

        /// <summary>
        /// 멀어진 리전들을 언로드하고 필요한 새 리전들을 로드합니다.
        /// </summary>
        /// <param name="newRegion">새로운 중심 리전 좌표</param>
        /// <param name="camera">카메라 객체</param>
        private void UpdateRegionLoadState(RegionCoord newRegion, Camera camera)
        {
            // 이전 리전에서 멀어진 리전들 언로드
            UnloadDistantRegions(newRegion);

            // 새로운 리전과 그 주변 리전들 로드
            LoadRequiredRegions(newRegion, camera);
            
            _currentRegion = newRegion;
        }

        /// <summary>
        /// 주어진 중심점 주변의 9개 리전을 로드합니다.
        /// 이미 로드된 리전은 건너뛰고 필요한 리전만 새로 로드합니다.
        /// </summary>
        /// <param name="center">중심 리전 좌표</param>
        /// <param name="camera">카메라 객체</param>
        private void LoadRequiredRegions(RegionCoord center, Camera camera)
        {
            List<RegionCoord> requiredRegions = RegionCoord.ADJACENT_REGION_COORDS
                .Select(coord => center + coord)
                .ToList();
            
            // 캐시에 로드되지 않았으면 리전을 로드한다.
            foreach (RegionCoord regionCoord in requiredRegions)
            {
                if (!_regions.ContainsKey(regionCoord))
                {
                    LoadRegion(regionCoord);
                    //LogProfile.WriteLine($"로딩={regionCoord.ToString()}");
                }
            }

            //Debug.PrintLine($"새롭게 로딩한 리전={num}");
        }

        /// <summary>
        /// 지정된 좌표의 리전을 로드합니다.
        /// 최근 캐시, 리전 풀 순서로 확인하여 재사용 가능한 리전이 있으면 사용하고,
        /// 없으면 새로 생성합니다.
        /// </summary>
        /// <param name="coord">로드할 리전의 좌표</param>
        private void LoadRegion(RegionCoord coord)
        {
            int n = SEPERATE_CHUNK_COUNT / 2;

            // (1) 최근 캐시에서 먼저 확인하고 있으면 그대로 가져와서 사용한다.
            TerrainRegion region = _recentRegions.RemoveRegion(coord);
            if (region != null)
            {
                // 캐시된 리전이 있으면 그대로 사용
                _regions[coord] = region;
                return;
            }

            // (2) 최근 캐시에 없는 경우에만 풀에서 가져와서 재설정하여 사용한다.
            //     또는 풀이 비워있는 경우에는 생성하여 사용한다. 
            region = _regionPool.Count > 0 ? _regionPool.Dequeue()
                : new TerrainRegion(coord, SEPERATE_CHUNK_SIZE, n, _terrainChunkShader);

            // (a) 저해상도 맵을 로딩한다.
            string heightmapLowResFileName = _heightmapBasePath + $"low\\region{coord.X}x{coord.Y}.png";

            if (!File.Exists(heightmapLowResFileName)) 
            {
                Console.WriteLine($"{heightmapLowResFileName}={File.Exists(heightmapLowResFileName)}");
                return;
            }

            region.LoadTerrainLowResMap(coord, heightmapLowResFileName, async () =>
            {
                // (b) 고해상도 맵 로딩 시작(저해상도 맵 로딩이 완료되면)
                await region.LoadTerrainHighResMap(coord, _heightmapBasePath, () =>
                {

                });
            });

            _regions[coord] = region;
        }

        /// <summary>
        /// 중심점에서 멀어진 리전들을 언로드합니다.
        /// 언로드된 리전은 최근 사용 캐시에 저장됩니다.
        /// </summary>
        /// <param name="center">중심 리전 좌표</param>
        private void UnloadDistantRegions(RegionCoord center)
        {
            List<RegionCoord> regionsToUnload = _regions.Keys
                .Where(destCoord => !IsRegionRequired(destCoord, center))
                .ToList();

            // 멀어진 리전들은 리전딕셔너리에서 삭제하고 리전풀로 반환하여 재사용한다.
            foreach (RegionCoord coord in regionsToUnload)
            {
                var region = _regions[coord];
                _regions.Remove(coord);
                //_regionPool.Enqueue(region); // 리전 풀에 반환
                _recentRegions.CacheRegion(coord, region); // 최근 사용 캐시에 저장
                //LogProfile.WriteLine("삭제=" + coord.ToString());
            }

            //Debug.PrintLine($"삭제한 리전={regionsToUnload.Count}");           
        }

        /// <summary>
        /// 지정된 좌표가 현재 필요한 9개 리전 영역 안에 있는지 확인합니다.
        /// </summary>
        /// <param name="destCoord">확인할 리전 좌표</param>
        /// <param name="centerCoord">중심 리전 좌표</param>
        /// <returns>필요한 리전이면 true, 아니면 false</returns>
        private bool IsRegionRequired(RegionCoord destCoord, RegionCoord centerCoord)
        {
            int dx = Math.Abs(destCoord.X - centerCoord.X);
            int dz = Math.Abs(destCoord.Y - centerCoord.Y);
            return dx <= 1 && dz <= 1; // 기본적으로 주변 8개 리전 포함
        }

        /// <summary>
        /// 가시성 테스트를 통과한 모든 물체를 가져온다.
        /// </summary>
        /// <returns></returns>
        public List<Entity> GetVisibleEntities()
        {
            // 리스트가 널이면 초기화
            if (_visibleEntities == null) _visibleEntities = new List<Entity>();

            // 리스트를 비운다.
            _visibleEntities.Clear();

            // 리전을 순회하면서 가시성 테스트를 통과한 모든물체를 가져온다.
            foreach (TerrainRegion terrainRegion in _visibleRegionsCache)
            {
                List<Entity> visibleEntities = terrainRegion.GetVisibleEntities(SEPERATE_CHUNK_COUNT / 2);
                if (visibleEntities != null)
                {
                    _visibleEntities.AddRange(visibleEntities);
                }
            }

            return _visibleEntities;
        }

        /// <summary>
        /// 월드공간의 절대 xy평면에서의 높이벡터를 가져온다.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vertex3f GetTerrainHeightVertex3f(Vertex3f position)
        {
            float orginalX = position.x;
            float orginalY = position.y;

            float offset = SEPERATE_CHUNK_COUNT * SEPERATE_CHUNK_SIZE;
            Vertex3f positionInRegionSpace = Vertex3f.Zero;
            RegionCoord regionCoord = GetRegionCoord(position.xy());
            positionInRegionSpace.x = position.x - offset * regionCoord.X;
            positionInRegionSpace.y = position.y - offset * regionCoord.Y;

            if (_regions.ContainsKey(regionCoord))
            {
                Vertex3f res = _regions[regionCoord].GetTerrainHeightVertex3f(positionInRegionSpace);
                res.x = orginalX;
                res.y = orginalY;
                return res;
            }
            else
            {
                return position;
            }
        }

        public Vertex2f ConvertRelativeRegionCoord(RegionCoord coord, float regionSize, Vertex2f worldPosition)
        {
            Vertex2f relativeCameraPosition = new Vertex2f(
                 worldPosition.x - (coord.X - 0.5f) * regionSize,
                 worldPosition.y - (coord.Y - 0.5f) * regionSize);
            return relativeCameraPosition;
        }

        #region 디버깅용
        public string _DEBUG_GetVisibleRegionText()
        {
            string txt = "";
            foreach (TerrainRegion terrainRegion in _visibleRegionsCache)
            {
                txt += terrainRegion.RegionCoord + " ";
            }
            return txt;
        }

        public string _DEBUG_GetVisibleChunkCountText()
        {
            string txt = "";
            foreach (TerrainRegion terrainRegion in _visibleRegionsCache)
            {
                if (terrainRegion.FrustumedChunkAABB == null) continue;
                txt += $"{terrainRegion.RegionCoord}={terrainRegion.FrustumedChunkAABB.Count} ";
            }

            return txt;
        }

        public int _DEBUG_GetVisibleChunkCount()
        {
            int total = 0;
            foreach (TerrainRegion terrainRegion in _visibleRegionsCache)
            {
                if (terrainRegion.FrustumedChunkAABB == null) continue;
                total += terrainRegion.FrustumedChunkAABB.Count;
            }
            return total;
        }

        public string _DEBUG_GetTerrainHeightResolutions()
        {
            string txt = "";
            foreach (TerrainRegion terrainRegion in _visibleRegionsCache)
            {
                if (terrainRegion.TerrainData == null) continue;
                txt += terrainRegion.RegionCoord + (terrainRegion.TerrainData.IsHighResLoaded ? "(1) " : "(0) ");
            }

            return txt;
        }

        public string _DEBUG_STRING = "";

        public void _DEBUG_aa()
        {
            // 모든 타일이 로드된 후
            TerrainRegion region = _regions[_currentRegion];
            if (region.TerrainData.IsAllTilesLoaded())
            {
                Bitmap debugTexture = region.TerrainData._DEBUG_GetHighResHeightmapBitmap();
                if (debugTexture != null)
                {
                    debugTexture.Save("C:\\Users\\mekjh\\OneDrive\\바탕 화면\\debug_heightmap.png", ImageFormat.Png);
                    debugTexture.Dispose();
                }
            }
        }

        #endregion
    }
}
