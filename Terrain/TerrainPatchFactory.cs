using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Terrain
{
    /// <summary>
    /// 지형 시스템의 패치들을 생성하고 관리하는 팩토리 클래스입니다.
    /// 지형을 여러 개의 작은 패치로 분할하여 생성하고, LOD와 컬링 최적화를 지원합니다.
    /// </summary>
    public class TerrainPatchFactory
    {
        private readonly TerrainData _terrainData;          // 높이맵 기반의 지형 데이터 참조. 패치 생성시 높이값 계산에 사용
        private RawModel3d _quadPatch;

        public TerrainPatchFactory(TerrainData terrainData)
        {
            _terrainData = terrainData;

            // 단순 사각 패치를 만든다.
            List<float> pos = new List<float>();    // 정점 위치 데이터
            List<uint> indices = new List<uint>();  // 인덱스 데이터

            // 정점 위치 데이터 생성 (사각형 패치)
            pos.Add(0.0f); pos.Add(0.0f); pos.Add(0.0f);
            pos.Add(1.0f); pos.Add(0.0f); pos.Add(0.0f);
            pos.Add(0.0f); pos.Add(1.0f); pos.Add(0.0f);
            pos.Add(1.0f); pos.Add(1.0f); pos.Add(0.0f);
            indices.Add(0); indices.Add(1); indices.Add(2); indices.Add(3);

            // GPU 버퍼 생성 및 데이터 업로드
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            Loader3d.StoreDataInAttributeList(0, 3, pos.ToArray());    // 위치
            uint ibo = Loader3d.StoreDataInAttributeList(indices.ToArray());
            Gl.BindVertexArray(0);

            // RawModel 생성 및 설정
            _quadPatch = new RawModel3d(vao, pos.ToArray())
            {
                IBO = ibo,                      // 인덱스 버퍼
                VertexCount = indices.Count,    // 정점 수
                IsDrawElement = true            // 인덱스 사용 렌더링
            };
        }

        public List<AABB> CreateChunks(RegionCoord coord, int n, int chunkSize, TerrainData terrainData)
        {
            int totalChunks = 4 * n * n;
            var entities = new List<AABB>(totalChunks);

            // 동시에 접근해도 안전하게 빈 공간 미리 확보
            for (int i = 0; i < totalChunks; i++)
            {
                entities.Add(null);
            }

            float halfSize = n * chunkSize;
            float totalWidth = halfSize * 2;
            float baseX = coord.X * totalWidth;
            float baseY = coord.Y * totalWidth;
            float verticalScale = TerrainConstants.DEFAULT_VERTICAL_SCALE;

            Parallel.For(0, 2 * n, x_offset => {
                int x = x_offset - n;

                // 각 스레드별 재사용 객체 (스레드 안전)
                Vertex3f lower = new Vertex3f();
                Vertex3f upper = new Vertex3f();
                Vertex3f index = new Vertex3f();

                for (int y_offset = 0; y_offset < 2 * n; y_offset++)
                {
                    int y = y_offset - n;
                    int i = y_offset;
                    int j = x_offset;

                    // 결과 리스트에서의 인덱스 계산
                    int entityIndex = y_offset * (2 * n) + x_offset;

                    Vertex2f h = terrainData.GetHeightBound(j, i, n);

                    // 객체 재사용
                    lower.x = chunkSize * x + baseX;
                    lower.y = chunkSize * y + baseY;
                    lower.z = verticalScale * h.x;

                    upper.x = lower.x + chunkSize;
                    upper.y = lower.y + chunkSize;
                    upper.z = verticalScale * h.y;

                    index.x = x;
                    index.y = y;
                    index.z = 0;

                    AABB newAABB = new AABB(
                        new Vertex3f(lower.x, lower.y, lower.z),
                        new Vertex3f(upper.x, upper.y, upper.z)
                    );
                    newAABB.Index = new Vertex3f(index.x, index.y, index.z);

                    // 미리 할당된 위치에 결과 저장
                    entities[entityIndex] = newAABB;
                }
            });

            return entities;
        }

        /// <summary>
        /// 전체 지형을 대표하는 단일 엔티티를 생성합니다.
        /// </summary>
        public Entity CreateUnifiedTerrainEntity(int n, int chunkSize, Texture heightMapTexture, int regionCoordX, int regionCoordY)
        {
            int regionSize = 2 * n * chunkSize;

            // 기본 평면 메시 생성
            RawModel3d planedRawModel3d = RegionManager.TerrainPlaneNxN;

            // 텍스처 모델 설정 및 바운딩 박스 초기화
            TexturedModel texturedPlaneModel = new TexturedModel(planedRawModel3d, heightMapTexture);
            texturedPlaneModel.AABB = new AABB(
                -Vertex3f.One * (n * chunkSize),    // 최소 경계
                Vertex3f.One * (n * chunkSize));    // 최대 경계
            texturedPlaneModel.OBB = OBB.ZeroSizeOBB;

            // 엔티티 생성 및 초기화
            Entity terrainEntity = new Entity($"terrainNxN", "terrain", texturedPlaneModel);
            terrainEntity.Position = new Vertex3f(regionCoordX * regionSize, regionCoordY * regionSize, 0);
            terrainEntity.UpdateBoundingBox();

            return terrainEntity;
        }

        /// <summary>
        /// 지정된 패치 위치에 대한 3D 메시를 생성합니다.
        /// </summary>
        /// <param name="x">패치의 X index 좌표</param>
        /// <param name="y">패치의 Y index 좌표</param>
        /// <param name="chunkSize">단위 크기</param>
        /// <remarks>
        /// 생성되는 메시 데이터:
        /// - 위치 (pos): 패치의 기하학적 형태
        /// - 텍스처 좌표 (tex): 높이맵과 지형 텍스처 매핑
        /// - 법선 벡터 (nor): 기본 상향 벡터
        /// - 인덱스: 사각형 패치를 위한 인덱스
        /// </remarks>
        private RawModel3d CreatePatchPlaneMesh(int coordX, int coordY, float totalWidth, int x, int y, float chunkSize = 1.0f)
        {
            List<float> pos = new List<float>();            // 정점 위치 데이터
            List<float> tex = new List<float>();            // 텍스처 좌표
            List<float> nor = new List<float>();            // 법선 벡터
            List<uint> indices = new List<uint>();          // 인덱스 데이터

            // 정점 위치 데이터 생성 (사각형 패치)
            float offsetX = coordX * totalWidth;
            float offsetY = coordY * totalWidth;
            pos.Add(x * chunkSize + offsetX);          pos.Add(y * chunkSize + offsetY);          pos.Add(0.0f);
            pos.Add((x + 1) * chunkSize + offsetX);    pos.Add(y * chunkSize + offsetY);          pos.Add(0.0f);
            pos.Add(x * chunkSize + offsetX);          pos.Add((y + 1) * chunkSize + offsetY);    pos.Add(0.0f);
            pos.Add((x + 1) * chunkSize + offsetX);    pos.Add((y + 1) * chunkSize + offsetY);    pos.Add(0.0f);

            // 텍스처 좌표 계산
            float size = _terrainData.Size;
            float mapWidth = _terrainData.Width;
            float mapHeight = _terrainData.Height;

            // 텍스처 좌표 매핑 (높이맵 UV 좌표)
            tex.Add((chunkSize * (x + 0) + size) / mapWidth);
            tex.Add((chunkSize * (y + 0) + size) / mapHeight);
            tex.Add((chunkSize * (x + 1) + size) / mapWidth);
            tex.Add((chunkSize * (y + 0) + size) / mapHeight);
            tex.Add((chunkSize * (x + 0) + size) / mapWidth);
            tex.Add((chunkSize * (y + 1) + size) / mapHeight);
            tex.Add((chunkSize * (x + 1) + size) / mapWidth);
            tex.Add( (chunkSize * (y + 1) + size) / mapHeight);

            // 법선 벡터 설정 (모두 상향)
            for (int i = 0; i < 4; i++)
            {
                nor.Add(0.0f); nor.Add(0.0f); nor.Add(1.0f);
            }

            // 인덱스 데이터 설정
            indices.Add(0); indices.Add(1); indices.Add(2); indices.Add(3);

            // GPU 버퍼 생성 및 데이터 업로드
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            Loader3d.StoreDataInAttributeList(0, 3, pos.ToArray());    // 위치
            Loader3d.StoreDataInAttributeList(1, 2, tex.ToArray());    // 텍스처 좌표
            Loader3d.StoreDataInAttributeList(2, 3, nor.ToArray());    // 법선
            uint ibo = Loader3d.StoreDataInAttributeList(indices.ToArray());
            Gl.BindVertexArray(0);

            // RawModel 생성 및 설정
            RawModel3d rawModel = new RawModel3d(vao, pos.ToArray())
            {
                IBO = ibo,                                           // 인덱스 버퍼
                VertexCount = indices.Count,                         // 정점 수
                IsDrawElement = true                                 // 인덱스 사용 렌더링
            };

            return rawModel;
        }
    }
}