using Common.Abstractions;
using Geometry;
using Model3d;
using Occlusion;
using OpenGL;
using System;
using System.Collections.Generic;

namespace Terrain
{
    /// <summary>
    /// 청크는 Region의 부분으로 AABB, BVH를 관리한다.
    /// </summary>
    public class Chunk
    {
        private string _name;                                   // 청크의 이름
        private BVH _bvh;                                       // 물체의 이진계층트리구조
        private List<Entity> _frustumedEntity;                  // 뷰프러스텀 컬링을 통과한 물체리스트
        private Dictionary<string, Entity> _dicEntityCache;     // 물체의 딕셔너리 캐싱데이터
        private List<Entity> _entityCache;                      // 물체의 모든 캐쉬
        private AABB _chunkAABB;                                // 청크의 바운딩박스

        private bool _uploadedVegetation = false;
        private uint _vegetationVBO;
        private uint _vegetationVAO;

        public string Name => _name;
        public AABB ChunkAABB => _chunkAABB;  
        public List<Entity> EntityList => _entityCache;
        public List<Entity> FrustumedEntity { get => _frustumedEntity; set => _frustumedEntity = value; }
        public uint VegetationVAO { get => _vegetationVAO; set => _vegetationVAO = value; }
        public uint VegetationVBO { get => _vegetationVBO; set => _vegetationVBO = value; }
        public int VegetationCount => _entityCache.Count;
        public Dictionary<string, Entity> DicEntityCache { get => _dicEntityCache; set => _dicEntityCache = value; }

        /// <summary>
        /// 청크 생성자
        /// </summary>
        /// <param name="chunkName"></param>
        /// <param name="aabb"></param>
        public Chunk(string chunkName, AABB aabb)
        {
            _name = chunkName;
            _chunkAABB = aabb;
            _bvh = new BVH();
            _frustumedEntity = new List<Entity>();
            _dicEntityCache = new Dictionary<string, Entity>();
            _entityCache = new List<Entity>();
        }

        /// <summary>
        /// 물체를 지형에 추가한다.
        /// </summary>
        /// <param name="entity">물체</param>
        public void AddEntity(Entity entity)
        {
            entity.UpdateBoundingBox();
            entity.AABB.BaseEntity = entity;
            _bvh.InsertLeaf(entity.AABB);
            _dicEntityCache.Add(entity.Name, entity);
            _entityCache.Add(entity);
        }

        /// <summary>
        /// 컬링 시스템의 상태를 업데이트하고 가시성 테스트를 수행합니다.
        /// </summary>
        /// <param name="camera">현재 카메라</param>
        /// <param name="viewFrustum">뷰 프러스텀</param>
        /// <param name="zbuffer">계층적 Z-버퍼</param>
        public void Update(Camera camera, Polyhedron viewFrustum, HierarchicalZBuffer zbuffer, Matrix4x4f vp, Matrix4x4f view)
        {
            // 백트리에서 모든 링크를 모두 연결한다.
            //_bvh.ClearBackCopy();

            // 뷰프러스텀을 이용한 가시성 컬링 수행
            //CullingTestByViewFrustum(viewFrustum);

            // zbuffer테스트
            //CullingTestByHierarchicalZBuffer(zbuffer, vp, view);

            // 노드를 모은다.
            //_frustumedEntity?.Clear();
            //_frustumedEntity = _bvh.ExtractEntity();
        }

        /// <summary>
        /// 초목의 정보를 GPU로 전송한다.
        /// </summary>
        public void UploadVegetation()
        {
            // 나무의 위치 데이터를 수집
            List<float> vertices = new List<float>();

            foreach (Entity entity in _entityCache)
            {
                vertices.Add(entity.Position.x);
                vertices.Add(entity.Position.y);
                vertices.Add(entity.Position.z);
            }

            // VBO 생성
            uint vbo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            // VBO에 데이터를 업로드 (정적 VBO 사용)
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(vertices.Count * sizeof(float)), vertices.ToArray(), BufferUsage.StaticDraw);

            // VAO 생성
            _vegetationVAO = Gl.GenVertexArray();

            // AO 설정 (인스턴스 속성 지정)
            Gl.BindVertexArray(_vegetationVAO);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 3 * sizeof(float), IntPtr.Zero);
            Gl.VertexAttribDivisor(0, 1); // Instanced Rendering 사용
            Gl.BindVertexArray(0);

            // VBO ID 저장 (필요하면 클래스 멤버 변수에 저장 가능)
            _vegetationVBO = vbo;

            _uploadedVegetation = true;
        }


        /// <summary>
        /// 시야절두체(View Frustum)를 사용하여 객체를 컬링합니다.
        /// </summary>
        public void CullingTestByViewFrustum(Polyhedron viewPolyhedron)
        {
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(_bvh.Root);

            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();
                if (currentNode == null) continue;

                if (currentNode.AABB.Visible(viewPolyhedron.Planes) || currentNode.AABB.SphereRadius > 100.0f)
                {
                    if (currentNode.Left) queue.Enqueue(currentNode.Child1);
                    if (currentNode.Right) queue.Enqueue(currentNode.Child2);
                }
                else
                {
                    currentNode.UnLinkBackCopy();
                }
            }
        }


        /// <summary>
        /// 계층적 Z-버퍼를 사용하여 객체를 컬링합니다.
        /// </summary>
        public void CullingTestByHierarchicalZBuffer(HierarchicalZBuffer zbuffer, Matrix4x4f vp, Matrix4x4f view)
        {
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(_bvh.Root);

            while (queue.Count > 0)
            {
                Node cnode = queue.Dequeue();
                if (cnode == null) continue;

                //if (zbuffer.TestVisibility(vp, view, cnode.AABB) || cnode.AABB.SphereRadius > 100.0f)
                {
                    if (cnode.Left) queue.Enqueue(cnode.Child1);
                    if (cnode.Right) queue.Enqueue(cnode.Child2);
                }
                //else
                {
                    cnode.UnLinkBackCopy();
                }
            }
        }


        /// <summary>
        /// 컬링된 엔티티 목록을 갱신합니다.
        /// </summary>
        public void PrepareEntities()
        {
            _frustumedEntity.Clear();
            _frustumedEntity = _bvh.ExtractEntity();
        }

    }
}
