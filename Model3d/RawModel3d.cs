using Common.Abstractions;
using Geometry;
using OpenGL;
using System;

namespace Model3d
{
    public class RawModel3d : BaseModel3d
    {
        public enum UNIFORM_LOCATION_ATRRIBUTE_NUMBER
        {
            VERTEX = 0, 
            TEXCOORD = 1, 
            NORMAL = 2, 
            COLOR = 3, 
            BONEINDEX = 4, 
            BONEWEIGHT = 5
        }

        protected uint _vbo;
        protected uint _ibo;
        protected OBB _obb;
        protected AABB _aabb;

        Vertex2f[] _texCoords;
        Vertex3f[] _normals;
        Vertex3f[] _colors;
        Vertex4i[] _boneIndex;
        Vertex4f[] _boneWeight;

        public AABB AABB
        {
            get => _aabb;
            set => _aabb = value;
        }

        public OBB OBB
        {
            get => _obb;
            set => _obb = value;
        }


        public uint IBO
        {
            get => _ibo;
            set => _ibo = value;
        }

        public Vertex3f[] Vertices => _vertices;

        public Vertex2f[] TexCoords => _texCoords;

        public Vertex3f[] Normals => _normals;

        public Vertex3f[] Colors => _colors;

        public Vertex4i[] BoneIndices => _boneIndex;

        public Vertex4f[] BoneWeights => _boneWeight;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="isDrawElement"></param>
        /// <param name="isCpuStored"></param>
        public RawModel3d(bool isCpuStored = true, bool isDrawElement = false): base(isCpuStored, isDrawElement)
        {
            _vao = Gl.GenVertexArray();
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="vao"></param>
        /// <param name="vertices"></param>
        public RawModel3d(uint vao, Vertex3f[] vertices) : base(vertices)
        {
            _vao = vao;
        }

        /// <summary>
        /// 정점 배열을 기반으로 AABB(Axis-Aligned Bounding Box)와 OBB(Oriented Bounding Box)를 생성합니다.
        /// </summary>
        /// <param name="vertices">바운딩 박스를 계산할 정점 배열</param>
        public void GenerateBoundingBox(Vertex3f[] vertices)
        {
            // AABB 생성 - 모든 정점을 포함하는 축 정렬 경계 상자
            Vertex3f lowerBound = Vertex3f.Min(vertices);          // 최소 경계점 계산
            Vertex3f upperBound = Vertex3f.Max(vertices);          // 최대 경계점 계산
            _aabb = new AABB(lowerBound, upperBound, false);

            // OBB 생성 - 객체의 방향을 고려한 최적 경계 상자
            bool chked = OBBUtility.CalculateOBB(vertices,          // OBB 파라미터 계산
                out Vertex3f center,                                // 중심점
                out Vertex3f size,                                  // 크기
                out Vertex3f[] axis);                               // 주축 방향
            _obb = new OBB(center, size, axis);                     // OBB 생성
        }

        /// <summary>
        /// AABB, OBB 바운딩박스를 만든다.
        /// </summary>
        /// <returns></returns>
        public void GenerateBoundingBox()
        {
            Vertex3f lowerBound = Vertex3f.Min(_vertices);
            Vertex3f upperBound = Vertex3f.Max(_vertices);
            _aabb = new AABB(lowerBound, upperBound, false);

            bool chked = OBBUtility.CalculateOBB(_vertices, out Vertex3f center, out Vertex3f size, out Vertex3f[] axis);
            _obb = new OBB(center, size, axis);
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="vao"></param>
        /// <param name="positions"></param>
        public RawModel3d(uint vao, float[] positions) : base(false, false)
        {
            _vao = vao;
            _vertexCount = positions.Length / 3;
            _vertices = new Vertex3f[_vertexCount];

            for (int i = 0; i < _vertexCount; i++)
            {
                _vertices[i] = new Vertex3f(positions[3 * i + 0], positions[3 * i + 1], positions[3 * i + 2]);
            }
        }

        public RawModel3d(uint vao, uint ibo, float[] positions) : this(vao, positions)
        {
            _ibo = ibo;
        }

        /// <summary>
        /// 데이터를 클래스에 보관한다. 아직 GPU에는 올리지 않는다.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="texCoords"></param>
        /// <param name="normals"></param>
        /// <param name="colors"></param>
        /// <param name="boneIndex"></param>
        /// <param name="boneWeight"></param>
        public void Init(Vertex3f[] vertices = null, Vertex2f[] texCoords = null, Vertex3f[] normals = null, Vertex3f[] colors = null,
            Vertex4i[] boneIndex = null, Vertex4f[] boneWeight = null)
        {
            if (vertices != null)
            {
                _vertexCount = vertices.Length;
                _vertices = new Vertex3f[_vertexCount];
                for (int i = 0; i < vertices.Length; i++)
                    _vertices[i] = vertices[i];
            }

            if (texCoords != null)
            {
                _texCoords = new Vertex2f[texCoords.Length];
                for (int i = 0; i < texCoords.Length; i++)
                    _texCoords[i] = texCoords[i];
            }

            if (normals != null)
            {
                _normals = new Vertex3f[normals.Length];
                for (int i = 0; i < normals.Length; i++)
                    _normals[i] = normals[i];
            }

            if (colors != null)
            {
                _colors = new Vertex3f[colors.Length];
                for (int i = 0; i < colors.Length; i++)
                    _colors[i] = colors[i];
            }

            if (boneIndex != null)
            {
                _boneIndex = new Vertex4i[boneIndex.Length];
                for (int i = 0; i < boneIndex.Length; i++)
                    _boneIndex[i] = boneIndex[i];
            }

            if (boneWeight != null)
            {
                _boneWeight = new Vertex4f[boneWeight.Length];
                for (int i = 0; i < boneWeight.Length; i++)
                    _boneWeight[i] = boneWeight[i];
            }
        }

        /// <summary>
        /// VAO, VBO, IBO의 메모리를 해제한다.
        /// </summary>
        public void Clean()
        {
            if (_vao >= 0)
            {
                if (_vbo >= 0) Gl.DeleteBuffers(_vbo);
                if (_ibo >= 0) Gl.DeleteBuffers(_ibo);
                Gl.DeleteVertexArrays(_vao);
            }
        }

        /// <summary>
        /// GPU에 데이터를 전송하여 메모리에 올린다.
        /// </summary>
        public void GpuBind()
        {
            // 이전에 바인딩한 이력이 있으면 gpu에서 지운다.
            if (_vao >= 0) Clean();

            float[] postions;
            float[] texcoords;
            float[] normals;
            float[] colors;
            uint[] boneIndices;
            float[] boneWeights;

            _vao = Gl.GenVertexArray();
            Gl.BindVertexArray(_vao);

            if (_vertices != null)
            {
                postions = new float[_vertices.Length * 3];
                for (int i = 0; i < _vertices.Length; i++)
                {
                    postions[3 * i + 0] = _vertices[i].x;
                    postions[3 * i + 1] = _vertices[i].y;
                    postions[3 * i + 2] = _vertices[i].z;
                }
                GpuLoader.StoreDataInAttributeList(UNIFORM_LOCATION_ATRRIBUTE_NUMBER.VERTEX, 3, postions, BufferUsage.StaticDraw);
                if (!_isCpuStored) _vertices = null;
            }

            if (_texCoords != null)
            {
                texcoords = new float[_texCoords.Length * 2];
                for (int i = 0; i < _texCoords.Length; i++)
                {
                    texcoords[2 * i + 0] = _texCoords[i].x;
                    texcoords[2 * i + 1] = _texCoords[i].y;
                }
                GpuLoader.StoreDataInAttributeList(UNIFORM_LOCATION_ATRRIBUTE_NUMBER.TEXCOORD, 2, texcoords, BufferUsage.StaticDraw);
                if (!_isCpuStored) _texCoords = null;
            }

            if (_normals != null)
            {
                normals = new float[_normals.Length * 3];
                for (int i = 0; i < _normals.Length; i++)
                {
                    normals[3 * i + 0] = _normals[i].x;
                    normals[3 * i + 1] = _normals[i].y;
                    normals[3 * i + 2] = _normals[i].z;
                }
                GpuLoader.StoreDataInAttributeList(UNIFORM_LOCATION_ATRRIBUTE_NUMBER.NORMAL, 3, normals, BufferUsage.StaticDraw);
                if (!_isCpuStored) _normals = null;
            }

            if (_colors != null)
            {
                colors = new float[_colors.Length * 3];
                for (int i = 0; i < _colors.Length; i++)
                {
                    colors[3 * i + 0] = _colors[i].x;
                    colors[3 * i + 1] = _colors[i].y;
                    colors[3 * i + 2] = _colors[i].z;
                }
                GpuLoader.StoreDataInAttributeList(UNIFORM_LOCATION_ATRRIBUTE_NUMBER.COLOR, 3, colors, BufferUsage.StaticDraw);
                if (!_isCpuStored) _colors = null;
            }

            if (_boneIndex != null)
            {
                boneIndices = new uint[_boneIndex.Length * 4];
                for (int i = 0; i < _boneIndex.Length; i++)
                {
                    boneIndices[4 * i + 0] = (uint)_boneIndex[i].x;
                    boneIndices[4 * i + 1] = (uint)_boneIndex[i].y;
                    boneIndices[4 * i + 2] = (uint)_boneIndex[i].z;
                    boneIndices[4 * i + 3] = (uint)_boneIndex[i].w;
                }
                GpuLoader.StoreDataInAttributeList(UNIFORM_LOCATION_ATRRIBUTE_NUMBER.BONEINDEX, 4, boneIndices, BufferUsage.StaticDraw);
                if (!_isCpuStored) _boneIndex = null;
            }

            if (_boneWeight != null)
            {
                boneWeights = new float[_boneWeight.Length * 4];
                for (int i = 0; i < _boneWeight.Length; i++)
                {
                    boneWeights[4 * i + 0] = _boneWeight[i].x;
                    boneWeights[4 * i + 1] = _boneWeight[i].y;
                    boneWeights[4 * i + 2] = _boneWeight[i].z;
                    boneWeights[4 * i + 3] = _boneWeight[i].w;
                }
                GpuLoader.StoreDataInAttributeList(UNIFORM_LOCATION_ATRRIBUTE_NUMBER.BONEWEIGHT, 4, boneWeights, BufferUsage.StaticDraw);
                if (!_isCpuStored) _boneWeight = null;
            }

            Gl.BindVertexArray(0);
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
