using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Physics.Collision
{
    /// <summary>
    /// 충돌 검사를 위한 강체들을 관리하는 공간 분할 그리드 시스템
    /// </summary>
    public class SpatialGrid
    {
        List<RigidBody> _bodies;          // 관리할 강체 목록
        GridCell[] _cells;                // 공간을 분할한 그리드 셀 배열
        List<RigidBodyPair> _pairs;       // 충돌 가능성이 있는 강체 쌍 목록

        Vertex2f _minRegion;              // 그리드 영역의 최소 좌표
        Vertex2f _maxRegion;              // 그리드 영역의 최대 좌표
        Vertex2f _cellSize;               // 각 셀의 크기
        Vertex2i _size;                   // 그리드의 가로/세로 셀 개수

        // 디버깅용 카운터들
        uint _collisionTests;             // 실행된 충돌 테스트 횟수
        uint _totalCells;                 // 전체 셀 개수
        uint _allocatedCells;             // 할당된 셀 개수
        uint _hashChecks;                 // 해시 체크 횟수
        Dictionary<string, bool> _checked; // 이미 검사한 강체 쌍 기록

        /// <summary>
        /// 셀의 크기
        /// </summary>
        public Vertex2f CellSize
        {
            get => _cellSize;
            set => _cellSize = value;
        }

        /// <summary>
        /// 광역 충돌 검출쌍
        /// </summary>
        public List<RigidBodyPair> CollisedRigidBodyPaired
        {
            get => _pairs;
        }

        /// <summary>
        /// 전체 셀의 크기
        /// </summary>
        public int TotalCell
        {
            get => (int)_totalCells;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        public SpatialGrid(float minX = -10.0f, float minY = -10.0f, float maxX = 10.0f, float maxY = 10.0f)
        {
            _minRegion = new Vertex2f(minX, minY);
            _maxRegion = new Vertex2f(maxX, maxY);
            _cellSize = Vertex2f.One;
            _pairs = new List<RigidBodyPair>();
            _bodies = new List<RigidBody>();
        }

        /// <summary>
        /// 강체를 추가한다.
        /// </summary>
        /// <param name="r"></param>
        public void Add(RigidBody r)
        {
            if (_bodies == null)
                _bodies = new List<RigidBody>();

            _bodies.Add(r);
        }

        /// <summary>
        /// 보관된 강체를 제거한다.
        /// </summary>
        /// <param name="r"></param>
        public void Remove(RigidBody r)
        {
            if (_bodies != null)
            {
                _bodies.Remove(r);
            }
        }

        /// <summary>
        /// 보관된 강체로 충돌을 광역 검출한다.
        /// </summary>
        /// <param name="duration"></param>
        public void Update(float duration)
        {
            // 보관된 강체를 모두 순회하여 그리드(셀들)에 담은 후,
            // 셀에 분배된 강체를 가지고 충돌쌍을 검출한다.
            // 검출된 충돌쌍은 _pairs 변수에 담아 보관한다.

            // 그리드를 설정한다.
            Vertex2f region = _maxRegion - _minRegion;
            uint gridWidth = (uint)(region.x / _cellSize.x);
            uint gridHeight = (uint)(region.y / _cellSize.y);
            _size = new Vertex2i((int)gridWidth, (int)gridHeight);
            _totalCells = gridWidth * gridHeight;
            _cells = new GridCell[gridWidth * gridHeight];
            _allocatedCells = 0;

            // 보관된 강체를 그리드에 나누어 담는다.
            for (int i = 0; i < _bodies.Count; i++)
            {
                RigidBody body = _bodies[i];
                if (body == null) continue;
                if (body.AABB == null) continue;

                float px = body.Position.x;
                float py = body.Position.y;

                // 강체가 그리드의 범위를 벗어나면 무시한다.
                if (px < _minRegion.x || px > _maxRegion.x || py < _minRegion.y || py > _maxRegion.y) continue;

                // 강체가 겹쳐지는 셀의 최소, 최대 범위를 계산한다.
                Vertex2i idxMin = new Vertex2i((int)((px - _minRegion.x) / _cellSize.x), 
                    (int)((py - _minRegion.y) / _cellSize.y));
                Vertex2i idxMax = new Vertex2i((int)((px + body.AABB.Size.x - _minRegion.x) / _cellSize.x),
                    (int)((py + body.AABB.Size.y - _minRegion.y) / _cellSize.y));

                // 강체가 겹쳐지는 셀이 여러 개일 수도 있으므로 겹쳐진 셀에 모두 강체를 추가한다.
                for (int y = idxMin.y; y <= idxMax.y; y++)
                {
                    for (int x = idxMin.x; x <= idxMax.x; x++)
                    {
                        int idx = (int)(x + y * gridWidth);
                        if (idx >= _cells.Length) continue;

                        if (_cells[idx] == null)
                        {
                            _cells[idx] = new GridCell();
                        }

                        _cells[idx].Add(body);
                    }
                }
            }

            // 그리드에 분배된 강체를 가지고 충돌쌍을 검출한다.
            QueryForCollisionPairs();
        }

        /// <summary>
        /// 충돌 가능성이 있는 셀에 충돌 가능한 강체의 쌍을 검출한다.
        /// </summary>
        private void QueryForCollisionPairs()
        {
            _checked = new Dictionary<string, bool>();
            _pairs.Clear();

            _collisionTests = 0;
            _hashChecks = 0;

            for (int i = 0; i < _size.y; i++)
            {
                for (int j = 0; j < _size.x; j++)
                {
                    int idx = i * _size.x + j;
                    GridCell cell = _cells[idx];
                    if (cell == null) continue;

                    // 셀 안에 강체가 2개 이상 있어야 충돌이 가능하다.
                    if (cell.Count < 2) continue;

                    // 셀 안의 강체들을 쌍을 지어 조사한다.
                    for (int k = 0; k < cell.Count; k++)
                    {
                        RigidBody rigidA = cell[k];
                        for (int l = k + 1; l < cell.Count; l++)
                        {
                            RigidBody rigidB = cell[l];

                            string hashA = rigidA.Guid + ":" + rigidB.Guid;
                            string hashB = rigidB.Guid + ":" + rigidA.Guid;
                            _hashChecks += 2;

                            if (!_checked.ContainsKey(hashA) && !_checked.ContainsKey(hashB))
                            {
                                _checked.Add(hashA, true);
                                _checked.Add(hashB, true);

                                _collisionTests++;

                                if (rigidA.AABB.CollisionTest(rigidB.AABB))
                                {
                                    _pairs.Add(new RigidBodyPair(rigidA, rigidB));
                                }
                            }
                        }
                    }
                }
            }

            Debug.Write($"HashChecks={_hashChecks} ");
            Debug.Write($"CollisionTests={_collisionTests} "); //
            Debug.Write($"광역충돌수={_pairs.Count} ");
        }

        /// <summary>
        /// 디버깅을 위한 맵그리기
        /// </summary>
        public void ToMap()
        {
            for (uint i = 0; i < _size.y; i++)
            {
                string line = "";
                for (uint j = 0; j < _size.x; j++)
                {
                    uint idx = (uint)(_size.x * i + j);
                    if (_cells[idx] == null)
                    {
                        line += "0 ";
                    }
                    else
                    {
                        line += _cells[idx].Count + " ";
                    }
                }
                Console.WriteLine(line);
            }
        }


    }
}
