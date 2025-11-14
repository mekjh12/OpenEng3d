using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZetaExt;

namespace Occlusion
{
    /// <summary>
    /// 오클루전 컬링 시스템을 구현하는 클래스입니다.
    /// 계층적 Z-버퍼, 뷰 프러스텀 컬링, 그리고 안개 효과를 통한 다중 오클루전 처리를 지원합니다.
    /// </summary>
    /// <remarks>
    /// 이 시스템은 다음과 같은 주요 기능을 제공합니다:
    /// - BVH(Bounding Volume Hierarchy)를 사용한 공간 분할
    /// - 뷰 프러스텀 컬링
    /// - 계층적 Z-버퍼 기반 오클루전 컬링
    /// - 지형 기반 오클루전
    /// - 안개 효과를 이용한 컬링
    /// </remarks>
    public class OcclusionCullingSystem
    {
        /// <summary>
        /// 바운딩 박스의 유형을 정의하는 열거형입니다.
        /// </summary>
        public enum BoundingBoxType
        {
            /// <summary>바운딩 박스 없음</summary>
            None = 0,
            /// <summary>축 정렬 바운딩 박스(Axis-Aligned Bounding Box)</summary>
            AABB = 2,
            /// <summary>방향성 바운딩 박스(Oriented Bounding Box)</summary>
            OBB = 4,
        }

        // 시스템 상태 관련 필드
        protected int _total = 0;                                     // 총 엔티티 수
        protected string _rootPath = "";                             // 리소스 루트 경로
        //protected BSH<OcclusionEntity> _entitiesBSH;                // 모든 엔티티의 BSH 트리
        //protected BSH<OcclusionEntity> _frustumedBSH;               // 프러스텀 내 엔티티의 BSH 트리

        // 엔티티 관리 컨테이너
        protected List<Entity> _frustumedEntity;           // 프러스텀 내 엔티티 리스트
        protected List<Entity> _occludedEntity;            // 오클루전된 엔티티 리스트
        protected List<Entity> _terrainOccludedEntity;     // 지형에 의해 오클루전된 엔티티 리스트

        // 모델 및 리소스 관리
        protected Dictionary<string, List<TexturedModel>> _dicRawModel;  // 모델 데이터 저장소

        // 오클루전 처리 관련
        protected Polyhedron _occPolyhedron;                        // 오클루전 다면체
        protected Entity _recentEntity;                             // 최근 처리된 엔티티
        private BVH _bvh;                                         // 메인 BVH 트리
        protected BVH _frustumedBVH;                                // 프러스텀 컬링된 BVH 트리

        // 빌보드 처리 관련
        protected List<Vertex3f> _treeBillboard;                    // 트리 빌보드 위치 목록
        protected uint _treeBillboardVAO = 0;                       // 트리 빌보드 VAO
        protected int _treeBillboardCount = 0;                      // 트리 빌보드 개수

        bool _isViewFrustumCulled = true;                           // 뷰프러스텀 컬링 사용유무
        bool _isFogCulled = false;                                  // 안개 오클루전 사용유무
        bool _isHierachicalZbuffer = true;                          // 계층적 깊이 오클루전 사용유무

        /// <summary>
        /// 트리 빌보드의 VAO(Vertex Array Object) ID를 반환합니다.
        /// </summary>
        public uint TreeBillboardVAO => _treeBillboardVAO;

        /// <summary>
        /// 현재 등록된 트리 빌보드의 총 개수를 반환합니다.
        /// </summary>
        public int TreeBillboardCount => _treeBillboardCount;

        /// <summary>
        /// 오클루더로 사용할 객체의 최소 화면 투영 면적입니다.
        /// 0.1이 최적값으로 권장됩니다.
        /// </summary>
        public static float OCCLUDER_MINIMAL_AREA = 0.03f;

        /// <summary>
        /// 뷰 프러스텀 내에서 렌더링될 오클루더 후보의 수입니다.
        /// </summary>
        public static float OCCLUDER_RENDER_COUNT = 0;

        /// <summary>
        /// 가장 최근에 처리된 엔티티를 반환합니다.
        /// </summary>
        public Entity RecentEntity => _recentEntity;

        /// <summary>
        /// 뷰 프러스텀 컬링을 통과한 엔티티 목록을 반환합니다.
        /// </summary>
        public List<Entity> FrustumPassEntity => _frustumedEntity;

        /// <summary>
        /// 오클루전 컬링이 완료된 엔티티 목록을 반환합니다.
        /// </summary>
        public List<Entity> OccludedEntity => _occludedEntity;

        /// <summary>
        /// 모델 이름으로 해당 텍스처 모델 목록에 접근하는 인덱서입니다.
        /// </summary>
        /// <param name="modelName">검색할 모델의 이름</param>
        /// <returns>해당 이름의 텍스처 모델 목록, 없는 경우 null</returns>
        public List<TexturedModel> this[string modelName]
        {
            get => _dicRawModel.ContainsKey(modelName) ? _dicRawModel[modelName] : null;
        }

        public TexturedModel[] GetModels(string modelName)
        {
            return _dicRawModel.ContainsKey(modelName) ? _dicRawModel[modelName].ToArray() : null;
        }

        /// <summary>
        /// 시스템에 등록된 총 엔티티의 수를 반환합니다.
        /// </summary>
        public int EntityTotalCount => _total;

        public bool IsHierachicalZbuffer
        {
            get => _isHierachicalZbuffer;
            set => _isHierachicalZbuffer = value;
        }

        public bool IsFogCulled
        {
            get => _isFogCulled;
            set => _isFogCulled = value;
        }

        public BVH BoundVolumeHierachy
        {
            get => _bvh;
        }
        public bool IsViewFrustumCulled
        {
            get => _isViewFrustumCulled;
            set => _isViewFrustumCulled = value;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public OcclusionCullingSystem(string rootPath)
        {
            _total = 0;
            //_entitiesBSH = new BSH<OcclusionEntity>();
            _frustumedEntity = new List<Entity>();
            _occludedEntity = new List<Entity>();
            //_lightBSH = new BSH<Light>();

            _treeBillboard = new List<Vertex3f>();

            _dicRawModel = new Dictionary<string, List<TexturedModel>>();
            _rootPath = rootPath;

            _bvh = new BVH();

            TextureStorage.NullTextureFileName = _rootPath + @"\Res\debug.jpg";

            if (!Directory.Exists(_rootPath))
            {
                new Exception($"{_rootPath} 지정된 경로의 폴더가 없습니다.");
            }
        }

        public List<TexturedModel> GetRawModels(string modelName)
        {
            if (_dicRawModel.ContainsKey(modelName))
            {
                return _dicRawModel[modelName];
            }
            else
            {
                return null;
            }
        }

        public void AddTreeBillboard(Vertex3f pos)
        {
            _treeBillboard.Add(pos);
        }

        public void UploadTreeBillboardAtGpu()
        {
            float[] data = new float[_treeBillboard.Count * 3];
            for (int i = 0; i < _treeBillboard.Count; i++)
            {
                Vertex3f p = _treeBillboard[i];
                data[3 * i + 0] = p.x;
                data[3 * i + 1] = p.y;
                data[3 * i + 2] = p.z;
            }

            _treeBillboardVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_treeBillboardVAO);
            GpuLoader.StoreDataInAttributeList(RawModel3d.UNIFORM_LOCATION_ATRRIBUTE_NUMBER.VERTEX, 3, data, BufferUsage.StaticDraw);
            Gl.BindVertexArray(0);

            _treeBillboardCount = _treeBillboard.Count;
            _treeBillboard.Clear();
        }

        public void UpdateRigid(Camera camera, AABB collider, out Node contactNode)
        {
            bool coll = Collision(_bvh, collider, out contactNode);
            collider.Collided = coll;
            Debug.Write(" 접촉" + coll);
        }

        public bool UpdateCollisionRigid(Camera camera, AABB collider, out Node contactNode)
        {
            bool coll = Collision(_bvh, collider, out contactNode);
            return coll;
        }

        public bool Collision(BVH bvh, AABB collider, out Node contactNode)
        {
            // BVH를 순회한다.
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(bvh.Root);

            bool res = false;
            contactNode = null;

            while (queue.Count > 0)
            {
                // 큐로부터 꺼낼때는 
                Node cnode = queue.Dequeue();
                if (cnode == null) continue;

                contactNode = cnode;

                // AABB와 접촉하면 
                if (collider.Visible(cnode.AABB.Planes))
                {
                    if (cnode.IsLeaf)
                    {
                        if ((cnode.AABB.BaseEntity as PhysicalRenderEntity).RigidBody != null)
                        {
                            if (collider.Visible((cnode.AABB.BaseEntity as PhysicalRenderEntity).RigidBody.Planes))
                            {
                                res = true;
                                break;
                            }
                        }
                    }

                    // 하위 노드로 순회한다.
                    if (cnode.Child1 != null) queue.Enqueue(cnode.Child1);
                    if (cnode.Child2 != null) queue.Enqueue(cnode.Child2);
                }
            }

            return res;
        }
        /// <summary>
        /// 폐색 컬링 시스템을 업데이트합니다.
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <param name="viewFrustum">시야 절두체</param>
        /// <param name="fogPlane">안개 평면</param>
        /// <param name="fogDensity">안개 밀도</param>
        public void Update(Camera camera, Polyhedron viewFrustum, Vertex4f? fogPlane = null, float fogDensity = 0.01f)
        {
            // BVH 트리 복사
            _bvh.ClearBackCopy();

            // 시야 절두체 컬링 수행
            if (_isViewFrustumCulled)
            {
                VisibleCullTest(_bvh, viewFrustum, camera);
            }

            // 안개 컬링 수행
            if (_isFogCulled)
            {
                if (fogPlane != null)
                {
                    FogOcclusion.OccludeFogHalfSpace(camera, (Vertex4f)fogPlane, fogDensity, _bvh);
                }
            }
        }

        /// <summary>
        /// 시야 절두체 내에서 지정된 화면 면적 비율 이상을 차지하는 폐색체들을 찾아 반환합니다.
        /// </summary>
        /// <param name="vpMatrix">뷰-투영 행렬</param>
        /// <param name="areaThreshold">화면 면적 비율 임계값 (0~4 범위, 예: 0.4는 10%)</param>
        /// <returns>지정된 면적 비율 이상을 차지하는 폐색체 목록</returns>
        public List<OccluderEntity> GetLargeOccludersInViewFrustum(Matrix4x4f vpMatrix, float areaThreshold = 0.1f)
        {
            List<OccluderEntity> result = new List<OccluderEntity>();

            // BVH 트리를 너비 우선 탐색
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(_bvh.Root);

            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();
                if (currentNode == null) continue;

                // 현재 노드의 정규화된 화면 면적 계산 (0~4 범위)
                float area = currentNode.AABB.CalculateScreenSpaceArea(vpMatrix);

                // 지정된 면적 비율 이상을 차지하는 경우만 처리
                // 예: 10%는 0.4 (4 * 0.1)
                if (area > areaThreshold)
                {
                    if (currentNode.IsLeaf)
                    {
                        result.Add((OccluderEntity)currentNode.AABB.BaseEntity);
                    }
                    else
                    {
                        // 자식 노드들을 큐에 추가
                        if (currentNode.Left) queue.Enqueue(currentNode.Child1);
                        if (currentNode.Right) queue.Enqueue(currentNode.Child2);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 계층적 Z-버퍼를 이용한 폐색 컬링을 수행합니다.
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <param name="zbuffer">계층적 Z-버퍼</param>
        public void UpdateOcclusionCuliingByHierachicalZbuffer(Camera camera, HierarchicalZBuffer zbuffer)
        {
            int travCount = 0;

            // 계층적 Z-버퍼 테스트 수행
            if (_isHierachicalZbuffer)
            {
                travCount = CullTestByHierarchicalZBuffer(_bvh, zbuffer, camera.VPMatrix, camera.ViewMatrix);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bvh"></param>
        /// <param name="zbuffer"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public int CullTestByHierarchicalZBuffer(BVH bvh, HierarchicalZBuffer zbuffer, Matrix4x4f vp, Matrix4x4f view)
        {
            int visibleCount = 0;
            int travNodeCount = 0;

            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(bvh.Root);

            while (queue.Count > 0)
            {
                Node cnode = queue.Dequeue();
                if (cnode == null) continue;

                travNodeCount++;

                //if (zbuffer.TestVisibility(vp, view, cnode.AABB))
                {
                    if (cnode.IsLeaf) visibleCount++;
                    if (cnode.Left) queue.Enqueue(cnode.Child1);
                    if (cnode.Right) queue.Enqueue(cnode.Child2);
                }
                //else
                {
                    cnode.UnLinkBackCopy();
                }
            }
            return travNodeCount;
        }

        /// <summary>
        /// 컬링된 트리에서 보이는 물체들을 추출하여 갱신합니다.
        /// </summary>
        /// <param name="camera">카메라</param>
        /// <returns>처리된 물체 수</returns>
        public int UpdateVisibleEntitiesFromCulledTree(Camera camera)
        {
            _frustumedEntity?.Clear();
            _frustumedEntity = _bvh.ExtractEntity();

            foreach (Entity entity in _frustumedEntity)
            {
                entity.Update(camera);
            }

            return _frustumedEntity.Count;
        }

        public void OccludedByTerrainOccluder(BVH bvh, Camera camera, TerrainOccluder3 terrainOccluder)
        {
            if (bvh == null) return;

            int travNodeCount = 0;

            // 알고리즘 설명
            Vertex3f cameraPosition = camera.Position;

            // OcclusionRegion의 평면을 구함.
            Plane[] planes = terrainOccluder.GetOccluderPlane(cameraPosition);

            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(bvh.Root);
            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();
                if (currentNode == null) continue;
                travNodeCount++;

                if (currentNode.AABB.Included(planes))
                {
                    currentNode.UnLinkBackCopy();
                }
                else
                {
                    if (currentNode.Left) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Right) queue.Enqueue(currentNode.Child2);
                }
            }

            //Debug.Write($"t={travNodeCount},");
        }

        /// <summary>
        /// 가시성 테스트를 한다. 원본트리를 변경하지 않고 노드의 left, right(BackCopy)만을 이용한다.
        /// </summary>
        /// <param name="bvh"></param>
        /// <param name="viewPolyhedron"></param>
        /// <param name="camera"></param>
        private void VisibleCullTest(BVH bvh, Polyhedron viewPolyhedron, Camera camera)
        {
            int visibleCount = 0;
            int travNodeCount = 0;

            // BVH를 순회한다.
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(bvh.Root);

            while (queue.Count > 0)
            {
                // 큐로부터 꺼낼때는 
                Node cnode = queue.Dequeue();
                if (cnode == null) continue;

                travNodeCount++;

                // AABB가 보이면 
                if (cnode.AABB.Visible(viewPolyhedron.Planes))
                {
                    visibleCount++;

                    // AABB가 모두 포함되면 --> 원본노드와 연결한다.
                    if (cnode.AABB.Included(viewPolyhedron.Planes))
                    {
                        // 아무런 행동을 하지 않아도 초기 bvh.ClearBackCopy()로 인하여 true로 연결되어있다.
                    }
                    // AABB의 일부만 접촉한 경우는 --> 하위 리스트를 탐색
                    else
                    {
                        // 하위 노드로 순회한다.
                        if (cnode.Child1 != null) queue.Enqueue(cnode.Child1);
                        if (cnode.Child2 != null) queue.Enqueue(cnode.Child2);
                    }
                }
                // AABB가 보이지 않으면 --> 새로운 노드를 연결 해제한다.
                else
                {
                    cnode.UnLinkBackCopy();
                }
            }
            //Debug.Write($"순회노드={travNodeCount}, 가시노드={visibleCount}, ");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="rawModel"></param>
        /// <param name="textureFileName"></param>
        public TexturedModel AddRawModel(string modelName, RawModel3d rawModel, string textureFileName)
        {
            Texture texture = TextureStorage.Add(_rootPath + textureFileName, Texture.TextureMapType.Diffuse);
            TexturedModel texturedModel = new TexturedModel(rawModel, texture);
            texturedModel.GenerateBoundingBox();
            List<TexturedModel> list = new List<TexturedModel>();
            list.Add(texturedModel);
            _dicRawModel[modelName] = list;
            return texturedModel;
        }

        /// <summary>
        /// 원형 모델을 추가한다.
        /// </summary>
        /// <param name="modelFileName"></param>
        public TexturedModel[] AddRawModel(string modelFileName)
        {
            string materialFileName = modelFileName.Replace(".obj", ".mtl");

            // 텍스쳐모델을 읽어온다.
            List<TexturedModel> texturedModels = ObjLoader.LoadObj(_rootPath + modelFileName);

            // 모델에 맞는 원래 모양의 바운딩 박스를 만든다.
            foreach (TexturedModel texturedModel in texturedModels)
            {
                texturedModel.GenerateBoundingBox();
            }

            // 모델을 캐시에 저장한다.
            _dicRawModel[Path.GetFileNameWithoutExtension(modelFileName)] = texturedModels;

            return texturedModels.ToArray();
        }

        public Vertex3f GetSize(string rawModelName)
        {
            List<TexturedModel> texturedModels = _dicRawModel[rawModelName];
            OBB res = null;
            foreach (RawModel3d rawModel3D in texturedModels)
            {
                res = (OBB)((res == null) ? rawModel3D.OBB : res.Union(rawModel3D.OBB));
            }
            return res.Size;
        }

        public Entity AddEntity(string entityName, string modelName, string lowModelName, Vertex3f pos, Vertex3f? size = null, float pitch = 0, float yaw = 0, float roll = 0)
        {
            LodEntity entity = new LodEntity(entityName, modelName, _dicRawModel[modelName].ToArray(), _dicRawModel[lowModelName].ToArray(), new float[] {50.0f, 100.0f} );
            entity.SetRollPitchAngle(pitch, yaw, roll);
            entity.Position = pos;
            entity.IsDrawOneSide = true;
            entity.Size = (Vertex3f)((size == null) ? Vertex3f.One : size);
            Insert(entity);
            return entity;
        }

        public Entity BakeEntity(string entityName, string modelName, string lowModelName, Vertex3f pos, Vertex3f? size = null, float pitch = 0, float yaw = 0, float roll = 0)
        {
            LodEntity entity = new LodEntity(entityName, modelName, _dicRawModel[modelName].ToArray(), _dicRawModel[lowModelName].ToArray(), new float[] { 50.0f, 100.0f });
            entity.SetRollPitchAngle(pitch, yaw, roll);
            entity.Position = pos;
            entity.IsDrawOneSide = true;
            entity.Size = (Vertex3f)((size == null) ? Vertex3f.One : size);
            return entity;
        }

        public Entity AddOccluderEntity(string entityName, string modelName, Vertex3f pos, Vertex3f? size = null, float pitch = 0, float yaw = 0, float roll = 0, bool isOneside = true)
        {
            Entity entity = new OccluderEntity(entityName, modelName, _dicRawModel[modelName].ToArray());
            entity.SetRollPitchAngle(pitch, yaw, roll);
            entity.Position = pos;
            entity.IsDrawOneSide = isOneside;
            entity.Size = (Vertex3f)((size == null) ? Vertex3f.One : size);
            Insert(entity);
            return entity;
        }

        public Entity AddEntity(string entityName, string modelName, Vertex3f pos, Vertex3f? size = null, float pitch = 0, float yaw = 0, float roll = 0, bool isOneside = true)
        {
            OccluderEntity entity = new OccluderEntity(entityName, modelName, _dicRawModel[modelName].ToArray());
            entity.SetRollPitchAngle(pitch, yaw, roll);
            entity.Position = pos;
            entity.IsDrawOneSide = isOneside;
            entity.Size = (Vertex3f)((size == null) ? Vertex3f.One : size);
            Insert(entity);
            return entity;
        }

        /// <summary>
        /// 물체를 트리에 삽입한다.
        /// </summary>
        /// <param name="entity"></param>
        public void Insert(Entity entity)
        {
            if (entity is Entity)
            {
                entity.UpdateBoundingBox();
                entity.AABB.BaseEntity = entity;
                _bvh.InsertLeaf(entity.AABB);
            }

            if (entity is OccluderEntity)
            {
                OccluderEntity occlusionEntity = (OccluderEntity)entity;
                occlusionEntity.GenBoxOccluder(occlusionEntity.OBB);
            }

            _recentEntity = entity;
            _total++;
        }

        /// <summary>
        /// 오클루전 컬링(Occlusion Culling)을 수행하여 카메라의 시야에서 가려진 엔티티를 제거하는 메서드
        /// 렌더링 성능 최적화를 위해 보이지 않는 객체들을 사전에 제거한다.
        /// </summary>
        /// <param name="cameraPosition">카메라의 현재 위치 좌표</param>
        /// <param name="cameraModelMatrix">카메라의 모델 변환 행렬</param>
        /// <param name="frustumPlane">카메라 시야를 구성하는 4개의 평면 방정식 배열</param>
        public void OccludeEntityByOccluder(Vertex3f cameraPosition, Matrix4x4f cameraModelMatrix, Plane[] frustumPlane)
        {
            // FrustumedEntity 리스트에서 오클루더(가리는 객체)만 모은다.
            Dictionary<uint, OccluderEntity> occluderDict = new Dictionary<uint, OccluderEntity>();
            foreach (Entity entity in _frustumedEntity)
            {
                // 엔티티가 OccluderEntity이고 실제 오클루더인 경우에만 처리
                if (entity is OccluderEntity)
                {
                    OccluderEntity occlusionEntity = (OccluderEntity)entity;
                    if (occlusionEntity.IsOccluder)
                    {
                        // 카메라로부터 엔티티까지의 거리 계산
                        float distance = (cameraPosition - occlusionEntity.Position).Length();

                        // 원근 공간에서의 영역 크기 계산
                        // 거리에 따라 크기가 달리 보이므로 적당히 큰 오클루더만 선택한다.
                        float perspectiveArea = occlusionEntity.OBB.Area / distance;

                        // 최소 영역 임계값보다 큰 오클루더만 사전에 추가
                        if (perspectiveArea > OCCLUDER_MINIMAL_AREA)
                        {
                            occluderDict.Add(occlusionEntity.OBJECT_GUID, occlusionEntity);
                        }
                    }
                }
            }

            // frustumedEntity에서 occludedEntity로 모두 복사한다.
            // 초기에는 모든 frustum 내 엔티티를 잠재적 오클루디 리스트에 추가
            _occludedEntity = new List<Entity>();
            foreach (Entity entity in _frustumedEntity)
            {
                _occludedEntity.Add(entity);
            }

            // occluder를 모두 검사할 때까지 반복
            while (occluderDict.Count > 0)
            {
                // 딕셔너리에서 마지막 오클루더를 하나씩 꺼내서 오클루딩을 한다.
                KeyValuePair<uint, OccluderEntity> first = occluderDict.Last();
                OccluderEntity occluderEntity = first.Value;

                // 현재 오클루더를 사전에서 제거
                occluderDict.Remove(occluderEntity.OBJECT_GUID);

                // 더 이상 오클루더가 아닌 경우 건너뛴다
                if (!occluderEntity.IsOccluder) continue;

                // 오클루더의 바운딩 박스 정보 추출
                BoxOccluder occluderBox = occluderEntity.BoxOccluder;

                // OcclusionRegion의 평면을 구함.
                // frustum 평면과 오클루더의 크기, 행렬 정보를 이용해 오클루전 영역 평면 생성
                int planeCount = Occluder.MakeOcclusionRegion(frustumPlane, occluderBox.Size, occluderBox.ModelMatrix,
                    cameraModelMatrix, out Plane[] occluderPlane);

                // 실루엣 엣지가 모두 제거된 경우 또는 박스의 안쪽에 카메라가 있는 경우에 오클루전을 건너뛴다
                if (planeCount == 0) continue;

                // 잠재적으로 가려질 수 있는 엔티티 리스트
                List<Entity> culledCandidateList = new List<Entity>();

                // 현재 오클루디 리스트를 순회
                foreach (OccluderEntity occludeeEntity in _occludedEntity)
                {
                    // 오클루더 자신은 제외
                    if (occluderEntity == occludeeEntity) continue;

                    // occlusion region에 있는지 확인
                    // 오클루더의 영역에 포함되는지 바운딩 박스로 검사
                    if (BoundingVolume.OrientedBoxIncluded(occluderPlane, occludeeEntity.OBB))
                    {
                        if (occluderEntity != occludeeEntity)
                        {
                            culledCandidateList.Add(occludeeEntity);
                        }
                    }
                }

                // 제거할 엔티티들을 오클루디 리스트에서 제거
                foreach (OccluderEntity entity in culledCandidateList)
                {
                    // 오클루더 사전에서 제거
                    occluderDict.Remove(entity.OBJECT_GUID);

                    // 오클루디 리스트에서 제거
                    _occludedEntity.Remove(entity);
                }
            };
        }


        /*

        public void Insert(Light light)
        {
            if (light is PointLight)
            {
                PointLight pointLight = (PointLight)light;
                _lightBSH.InsertLeaf(new BSH<Light>.Sphere<Light>(pointLight.Position, pointLight.MaxDistance)
                {
                    Object = (Light)light
                });
            }
            else if (light is SpotLight)
            {
                SpotLight sLight = (SpotLight)light;
                _lightBSH.InsertLeaf(new BSH<Light>.Sphere<Light>(sLight.Position, sLight.MaxDistance)
                {
                    Object = (Light)light
                });
            }
        }
        */


        /*
        
        /// <summary>
        /// * 뷰프러스텀 안의 광원을 판별하여 광원 리스트를 만든다.<br/>
        /// * 반환하는 광원의 개수는 많지 않므로 Binary Tree보다는 단순 리스트로 반환한다.<br/>
        /// </summary>
        /// <param name="viewPolyhedron"></param>
        public LightGroup OccludeEmitedLightInViewFrustum(Polyhedron viewPolyhedron)
        {
            // 라이팅그룹을 만든다.
            LightGroup lightGroup = new LightGroup();
            lightGroup.Init();

            // 최대조명거리를 기반으로 판별하여 뷰프러스텀 안에서 광원(점, 스팟)만 가져온다.
            _lightBSH.IntersectViewFrustum(viewPolyhedron.Planes, out List<Light> lights);

            // 스팟광원이 컬링되고 남은 광원만 가져온다.
            List<Light> spotLightCulled = IntersectViewFrustumSpotLight(viewPolyhedron.Planes, lights);

            // 종류별로 나누어서 lightGroup에서 분류하여 보관한다.
            foreach (Light light in spotLightCulled)
                lightGroup.Add(light);

            return lightGroup;
        }

        public List<Light> IntersectViewFrustumSpotLight(Plane[] planes, List<Light> lights)
        {
            List<Light> res = new List<Light>();
            foreach (Light light in lights)
            {
                if (light is SpotLight)
                {
                    if (BoundingVolume.SpotLightVisible(planes, (SpotLight)light))
                    {
                        res.Add(light);
                    }
                }
                else
                {
                    res.Add(light);
                }
            }
            return res;
        }


        /// <summary>
        /// 그림자 영역을 만들고 영역 안의 entity를 판별하여 그림자 entity 리스트를 만든다.
        /// </summary>
        /// <param name="viewPolyhedron"></param>
        /// <param name="directLightDirection"></param>
        /// <param name="renderShadowRegionEntities"></param>
        public void OccludeShadowVisibleEntities(Polyhedron viewPolyhedron, Vertex3f directLightDirection, out List<Entity> renderShadowRegionEntities)
        {
            renderShadowRegionEntities = new List<Entity>();
            renderShadowRegionEntities.Clear();

            //Polyhedron.ClipPolyhedron(in _viewPolyhedron, new Polyhedron.Plane(Vertex3f.UnitY, 0.0f), out Polyhedron clippedPolyhedron);
            Plane[] planes = Shadow.CalculateShadowRegionByDirectionLight(viewPolyhedron, directLightDirection);
            //_clipedViewPolyhedron = clippedPolyhedron;

            Queue<BSH<Entity>.Sphere<Entity>> queue2 = new Queue<BSH<Entity>.Sphere<Entity>>();
            queue2.Enqueue(_entitiesBSH.Root);
            while (queue2.Count > 0)
            {
                BSH<Entity>.Sphere<Entity> currentNode = queue2.Dequeue();
                if (currentNode == null) break;
                if (BoundingVolume.SphereVisible(planes, currentNode.Center, currentNode.Radius))
                {
                    if (currentNode.IsLeaf)
                    {
                        if (BoundingVolume.OrientedBoxVisible(planes, currentNode.Object.OBB))
                        {
                            renderShadowRegionEntities.Add(currentNode.Object);
                        }
                    }
                    if (currentNode.Childs[0] != null) queue2.Enqueue(currentNode.Childs[0]);
                    if (currentNode.Childs[1] != null) queue2.Enqueue(currentNode.Childs[1]);
                }
            }
        }       
        */

    }
}
