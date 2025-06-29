using System.Collections.Generic;

namespace Animate
{
    /// <summary>
    /// 3D 메시를 구성하는 삼각형 폴리곤 정보를 저장하는 클래스<br/>
    /// 
    /// * 블렌더로부터 내보낸 DAE 파일에서 삼각형 데이터를 관리<br/>
    /// * DAE 파일 파싱 시 정점, 법선, 텍스처 좌표의 인덱스 정보 관리<br/>
    /// * 각 삼각형별로 머티리얼과 정점 데이터 인덱스들을 묶어서 처리
    /// </summary>
    public class MeshTriangles
    {
        string _material;       // 삼각형에 적용될 머티리얼 이름
        List<uint> _vertices;   // 정점 위치 인덱스들
        List<uint> _normals;    // 법선 벡터 인덱스들  
        List<uint> _texcoords;  // 텍스처 좌표 인덱스들
        List<uint> _colors;     // 색상 인덱스들 (현재 미사용)

        public string Material
        {
            get => _material; 
            set => _material = value;
        }

        public List<uint> Vertices => _vertices;

        public List<uint> Texcoords => _texcoords;

        public List<uint> Normals => _normals;

        /// <summary>색상 인덱스 리스트 (확장용)</summary>
        public List<uint> Colors => _colors;

        public MeshTriangles()
        {
            _vertices = new List<uint>();
            _normals = new List<uint>();
            _texcoords = new List<uint>();
            _colors = new List<uint>();
        }

        public void AddVertices(params uint[] values)
        {
            _vertices.AddRange(values);
        }

        public void AddTexCoords(params uint[] values)
        {
            _texcoords.AddRange(values);
        }

        public void AddNormals(params uint[] values)
        {
            _normals.AddRange(values);
        }

        /// <summary>색상 인덱스 추가 (향후 확장용)</summary>
        public void AddColors(params uint[] values)
        {
            _colors.AddRange(values);
        }

        /// <summary>모든 인덱스 리스트 초기화</summary>
        public void Clear()
        {
            _vertices.Clear();
            _normals.Clear();
            _texcoords.Clear();
            _colors.Clear();
        }

        /// <summary>삼각형 개수 반환 (정점 수 / 3)</summary>
        public int TriangleCount => _vertices.Count / 3;

        /// <summary>유효한 삼각형 데이터인지 검증</summary>
        public bool IsValid => _vertices.Count > 0 &&
                               _vertices.Count == _normals.Count &&
                               _vertices.Count == _texcoords.Count;

        public override string ToString()
        {
            return $"MeshTriangles(Material: {_material ?? "None"}, Triangles: {TriangleCount}, Valid: {IsValid})";
        }
    }
}