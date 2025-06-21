using OpenGL;
using ZetaExt;

namespace Occlusion
{
    public class Sphere<T>
    {
        Vertex3f _center;
        float _radius;
        T _object;

        Sphere<T>[] _childs;
        Sphere<T> _parent;
        string _txt;
        uint _guid;
        int _depth = -1;

        public int Depth
        {
            get => _depth;
            set => _depth = value;
        }

        public Sphere<T> Clone()
        {
            Sphere<T> sphere = new Sphere<T>(_center, _radius);
            sphere.Object = _object;
            return sphere;
        }

        public bool IsRoot => (_parent == null);
        public uint GUID => _guid;

        public string Txt
        {
            get => _txt;
            set => _txt = value;
        }

        /// <summary>
        /// 붙이고자 하는 오브젝트
        /// </summary>
        public T Object
        {
            get => _object;
            set => _object = value;
        }

        public Sphere<T> Brother
        {
            get
            {
                if (ExistParent)
                {
                    int idx = ChildIndex;
                    int brotherIdx = (ChildIndex + 1) % 2;
                    return Parent.Childs[brotherIdx];
                }
                else
                {
                    return null;
                }
            }
        }

        public bool ExistParent => (this.Parent != null);

        public bool ExistGrandParent
        {
            get => Parent != null ? Parent.ExistParent : false;
        }

        public Sphere<T> GrandParent
        {
            get => ExistGrandParent ? _parent.Parent : null;
        }

        public Sphere<T> Parent
        {
            get => _parent;
            set => _parent = value;
        }

        /// <summary>
        /// 현재 나의 노드의 부모에서 나의 Index
        /// </summary>
        public int ChildIndex
        {
            get
            {
                if (_parent != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (_parent.Childs[i] == this) return i;
                    }
                    return -1;
                }
                else
                {
                    return -1;
                }
            }
        }

        public float Area => 4.0f * 3.141502f * _radius * _radius;

        public Sphere<T>[] Childs => _childs;

        public Vertex3f Center
        {
            get => _center;
            set => _center = value;
        }

        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        public bool IsLeaf
        {
            get
            {
                bool isLeaf = true;
                for (int i = 0; i < 2; i++)
                {
                    isLeaf = isLeaf && (_childs[i] == null);
                }
                return isLeaf;
            }
        }

        public Sphere()
        {
            _guid = Common.Core.GUID.GenID;
            _childs = new Sphere<T>[2];
        }

        public Sphere(Vertex3f center, float radius)
        {
            _center = center;
            _radius = radius;
            _childs = new Sphere<T>[2];
            _guid = Common.Core.GUID.GenID;
        }

        /// <summary>
        /// 자식을 부모노드에 대체하여 붙인다.
        /// </summary>
        /// <param name="removeChild"></param>
        /// <param name="attachNode"></param>
        /// <returns></returns>
        public bool ReplaceChild(Sphere<T> removeChild, Sphere<T> attachNode)
        {
            if (_childs[0] == removeChild)
            {
                _childs[0] = attachNode;
                attachNode.Parent = this;
                return true;
            }

            if (_childs[1] == removeChild)
            {
                _childs[1] = attachNode;
                attachNode.Parent = this;
                return true;
            }
            return false;
        }

        public bool IsInclude(Sphere<T> sphere)
        {
            float d = (_center - sphere.Center).Norm();
            return ((d + sphere.Radius) < _radius);
        }

        /// <summary>
        /// 서로 간의 부모 자식의 연결노드도 함께 해준다.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Sphere<T> Union(Sphere<T> A, Sphere<T> B)
        {
            float R = ((A.Center - B.Center).Norm() + A.Radius + B.Radius) * 0.5f;
            float t1 = (R - B.Radius) / (2 * R - A.Radius - B.Radius);
            float t2 = (R - A.Radius) / (2 * R - A.Radius - B.Radius);
            Vertex3f center = A.Center * t1 + B.Center * t2;
            Sphere<T> C = new Sphere<T>(center, R);
            C.Childs[0] = A;
            C.Childs[1] = B;
            A.Parent = C;
            B.Parent = C;
            return C;
        }

        public static float Cost(Sphere<T> targetNode, Sphere<T> insertBox)
        {
            float cost = 0.0f;

            Sphere<T> tourNode = targetNode;
            while (tourNode != null)
            {
                cost += Sphere<T>.Union(tourNode, insertBox).Area - tourNode.Area;
                tourNode = tourNode.Parent;
            }

            return cost;
        }
    }
}
