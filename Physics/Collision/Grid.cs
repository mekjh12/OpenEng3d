using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
    public class Grid
    {
        protected uint _xExtent;
        protected uint _yExtent;

        protected List<RigidBody>[] _location;
        Vertex3f _orgin = Vertex3f.Zero;
        float _inverseCellSize;

        protected List<RigidBody> _activeSets;

        public Grid(uint xExtent, uint yExtent, float cellSize)
        {
            _xExtent = xExtent;
            _yExtent = yExtent;
            _location = new List<RigidBody>[_xExtent * _yExtent]; // cast 는 내림으로 정수형 변환

            _orgin = new Vertex3f(0.5f * _xExtent * cellSize, 0.5f * _yExtent * cellSize, 0.0f);
            _inverseCellSize = 1.0f / cellSize;

            _activeSets = new List<RigidBody>();
        }

        protected uint GetLocationIndex(Vertex3f objectCenterPosition)
        {
            Vertex3f square = (objectCenterPosition + _orgin) * _inverseCellSize;
            return ((uint)square.x) + _xExtent * (uint)square.y;
        }

        public void Add(RigidBody rigidBody)
        {
            uint loc = GetLocationIndex(rigidBody.Position);

            if (_location[loc] == null)
            {
                _location[loc] = new List<RigidBody>();
            }

            _location[loc].Add(rigidBody);

            if (_location[loc].Count > 1)
            {
                _activeSets.Add(rigidBody);
            }
        }

        public void Remove(RigidBody rigidBody)
        {
            uint loc = GetLocationIndex(rigidBody.Position);
            if (_location[loc].Contains(rigidBody))
            {
                if (_location[loc].Count > 1)
                {
                    if (_activeSets.Contains(rigidBody))
                    {
                        _activeSets.Remove(rigidBody);
                    }
                }

                _location[loc].Remove(rigidBody);
            }
        }

        public void ToMap()
        {

            for (uint i = 0; i < _yExtent; i++)
            {
                string line = "";
                for (uint j = 0; j < _xExtent; j++)
                {
                    uint idx = _xExtent * i + j;
                    if (_location[idx] == null)
                    {
                        line += "0\t";
                    }
                    else
                    {
                        line += _location[idx].Count + "\t";
                    }
                }
                Console.WriteLine(line);
            }
        }

    }
}
