using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// Bone과 동일하게 뼈로서 부모와 자식을 연결하여 Armature를 구성하는 요소이다.
    /// </summary>
    public class Bone
    {
        int _index;
        string _name;
        List<Bone> _children;
        Bone _parent;

        // 애니메이션 변환 행렬를 구성하는 요소들
        // _localTransform이 변경되면 UpdatePropTransform()를 통해 _animatedTransform이 업데이트됨
        Matrix4x4f _localTransform = Matrix4x4f.Identity; // 부모뼈 공간에서의 변환 행렬
        Matrix4x4f _animatedTransform = Matrix4x4f.Identity; // 캐릭터 공간에서의 애니메이션 변환 행렬

        // 바인딩 포즈 행렬을 구성하는 요소들
        Matrix4x4f _localBindTransform = Matrix4x4f.Identity; // 부모뼈 공간에서의 바인딩 포즈 행렬
        Matrix4x4f _animatedBindPoseTransform = Matrix4x4f.Identity; // 캐릭터 공간에서의 바인딩 포즈 애니메이션 변환 행렬
        Matrix4x4f _inverseBindPoseTransform = Matrix4x4f.Identity; // 캐릭터 공간에서의 바인딩 포즈의 역행렬

        BoneAngle _restrictAngle;

        /// <summary>
        /// 캐릭터 공간에서의 바인딩 포즈 애니메이션 변환 행렬
        /// </summary>
        public Matrix4x4f AnimatedBindPoseTransform
        {
            get => _animatedBindPoseTransform;
            set => _animatedBindPoseTransform = value;
        }

        public BoneAngle RestrictAngle
        {
            get => _restrictAngle;
            set => _restrictAngle = value;
        }

        public bool IsLeaf => _children.Count == 0;

        /// <summary>
        ///  캐릭터 공간의 뼈의 끝부분 위치를 가져온다. 만약, 자식이 없으면 15의 값으로 지정된다.
        /// </summary>
        public Vertex3f EndPosition
        {
            get
            {
                if (_children.Count == 0)
                {
                    Matrix4x4f a = _animatedTransform * Matrix4x4f.Translated(0, 15, 0);
                    return a.Position;
                }
                else
                {
                    Vertex3f g = Vertex3f.Zero;
                    foreach (Bone bone in _children)
                    {
                        g += bone.AnimatedTransform.Position;
                    }
                    return g * (1.0f / _children.Count);
                }
            }
        }

        public Bone Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public bool IsArmature => (_parent == null);

        public bool IsRootArmature 
        {
            get
            {
                return _name == "mixamorig_Hips";
                return (_parent.Parent == null);
            }
        }
        
        public int Index
        {
            get => _index;
            set => _index = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// 캐릭터 공간에서의 뼈의 시작 위치, 즉 피봇의 위치를 가져오거나 설정한다.
        /// </summary>
        public Vertex3f PivotPosition
        {
            get => _animatedTransform.Position;
            set
            {
                _animatedTransform[3, 0] = value.x;
                _animatedTransform[3, 1] = value.y;
                _animatedTransform[3, 2] = value.z;
            }
        }

        public List<Bone> Childrens => _children;

        /// <summary>
        /// 캐릭터 공간에서의 애니메이션 변환 행렬
        /// </summary>
        public Matrix4x4f AnimatedTransform
        {
            get => _animatedTransform;
            set => _animatedTransform = value;
        }

        /// <summary>
        /// 부모 뼈공간에서의 변환행렬
        /// </summary>
        public Matrix4x4f LocalTransform
        {
            get => _localTransform;
            set => _localTransform = value;
        }

        /// <summary>
        /// 캐릭터 공간에서의 바인딩 포즈의 역행렬
        /// </summary>
        public Matrix4x4f InverseBindPoseTransform
        {
            get => _inverseBindPoseTransform;
            set => _inverseBindPoseTransform = value;
        }

        /// <summary>
        /// 바인딩 포즈의 변환행렬
        /// </summary>
        public Matrix4x4f LocalBindTransform
        {
            get => _localBindTransform;
            set => _localBindTransform = value;
        }

        /// <summary>
        /// 뼈대이름과 인덱스를 지정하여 뼈대를 생성한다.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        public Bone(string name, int index)
        {
            _children = new List<Bone>();
            _name = name;
            _index = index;
            _restrictAngle = new BoneAngle(-180, 180, -180, 180, -90, 90);
        }

        /// <summary>
        /// 자식 뼈대를 추가한다.
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(Bone child)
        {
            _children.Add(child);
        }


        /// <summary>
        /// 애니메이션 변환 행렬로부터 로컬 변환 행렬을 역계산하여 업데이트한다.
        /// (캐릭터 공간 → 부모 뼈공간 변환)
        /// </summary>
        public void UpdateLocalTransform()
        {
            if (_parent != null)
            {
                // 로컬 변환 = 부모의 역변환 * 현재 애니메이션 변환
                _localTransform = _parent._animatedTransform.Inversed() * _animatedTransform;
            }
            else
            {
                // 루트 뼈대의 경우 애니메이션 변환이 곧 로컬 변환
                _localTransform = _animatedTransform;
            }
        }

        /// <summary>
        /// 로컬 변환 행렬로부터 캐릭터 공간의 애니메이션 변환 행렬을 계산하고 자식 뼈대들에게 전파한다.
        /// </summary>
        /// <param name="isSelfIncluded">현재 뼈대부터 업데이트할지 여부 (true: 자신 포함, false: 자식들만)</param>
        /// <param name="exceptBone">업데이트에서 제외할 뼈대 (null이면 모든 뼈대 업데이트)</param>
        public void UpdatePropTransform(bool isSelfIncluded = false, Bone exceptBone = null)
        {
            // 깊이 우선 탐색(DFS)을 위한 스택 생성
            Stack<Bone> stack = new Stack<Bone>();

            // 시작점 설정: 자신 포함 여부에 따라 초기 스택 구성
            if (isSelfIncluded)
            {
                stack.Push(this); // 현재 뼈대부터 시작
            }
            else
            {
                // 현재 뼈대는 제외하고 직계 자식들부터 시작
                foreach (Bone childBone in _children)
                    stack.Push(childBone);
            }

            // 스택 기반 반복으로 모든 하위 뼈대 순회
            while (stack.Count > 0)
            {
                Bone currentBone = stack.Pop();

                // 제외 대상 뼈대는 건너뛰기
                if (currentBone == exceptBone) continue;

                // 애니메이션 변환 행렬 계산: LocalTransform을 부모의 월드 변환과 결합
                // 공식: AnimatedTransform = Parent.AnimatedTransform * LocalTransform
                // 루트 뼈대의 경우 부모가 없으므로 LocalTransform을 그대로 사용
                currentBone.AnimatedTransform = currentBone.Parent == null
                    ? currentBone.LocalTransform
                    : currentBone.Parent.AnimatedTransform * currentBone.LocalTransform;

                // 현재 뼈대의 모든 자식들을 스택에 추가하여 계속 순회
                foreach (Bone childBone in currentBone.Childrens)
                    stack.Push(childBone);
            }
        }

        public override string ToString()
        {
            string txt = "";
            Matrix4x4f m = _localBindTransform;
            for (uint i = 0; i < 4; i++)
            {
                txt += $"{Cut(m[0, i])} {Cut(m[1, i])} {Cut(m[2, i])} {Cut(m[3, i])}"
                    + ((i < 3) ? " / " : "");
            }

            string invBind = "";
            Matrix4x4f n = _inverseBindPoseTransform;
            for (uint i = 0; i < 4; i++)
            {
                invBind += $"{Cut(n[0, i])} {Cut(n[1, i])} {Cut(n[2, i])} {Cut(n[3, i])}"
                    + ((i < 3) ? " / " : "");
            }

            return $"[{_index},{_name}] Parent={_parent?.Name}, BindMatrix {txt} InvBindMatrix {invBind}";

            float Cut(float a) => ((float)Math.Abs(a) < 0.000001f) ? 0.0f : a;
        }

        public void ModifyPitch(float phi)
        {
            if (phi < _restrictAngle.ConstraintAngle.x )
            {
                Matrix4x4f localMat = _localTransform;
                Vertex3f lowerYAxis = localMat.Column1.Vertex3f();
                lowerYAxis.y = 0;
                lowerYAxis = lowerYAxis.Normalized * -Math.Sin(_restrictAngle.ConstraintAngle.x.ToRadian());
                lowerYAxis.y = (float)Math.Cos(_restrictAngle.ConstraintAngle.x.ToRadian());
                Matrix4x4f localRotMat = localMat.Column1.Vertex3f().RotateBetween(lowerYAxis);
                _localTransform = localMat * localRotMat;
                UpdatePropTransform(isSelfIncluded: true);
            }            
            else if (phi > _restrictAngle.ConstraintAngle.y)
            {
                Matrix4x4f localMat = _localTransform;
                Vertex3f lowerYAxis = localMat.Column1.Vertex3f();
                lowerYAxis.y = 0;
                lowerYAxis = lowerYAxis.Normalized * Math.Sin(_restrictAngle.ConstraintAngle.y.ToRadian());
                lowerYAxis.y = (float)Math.Cos(_restrictAngle.ConstraintAngle.y.ToRadian());
                Matrix4x4f localRotMat = localMat.Column1.Vertex3f().RotateBetween(lowerYAxis);
                _localTransform = localMat * localRotMat;
                UpdatePropTransform(isSelfIncluded: true);
            }
        }

        public void ModifyYaw(float phi)
        {
            if (phi < _restrictAngle.ConstraintAngle.z)
            {
                Matrix4x4f localMat = _localTransform;
                Vertex3f lowerZAxis = localMat.Column2.Vertex3f();
                lowerZAxis.z = 0;
                lowerZAxis = lowerZAxis.Normalized * -Math.Sin(_restrictAngle.ConstraintAngle.z.ToRadian());
                lowerZAxis.y = (float)Math.Cos(_restrictAngle.ConstraintAngle.z.ToRadian());
                Matrix4x4f localRotMat = localMat.Column2.Vertex3f().RotateBetween(lowerZAxis);
                _localTransform = localMat * localRotMat;
                UpdatePropTransform(isSelfIncluded: true);
            }
            else if (phi > _restrictAngle.ConstraintAngle.w)
            {
                Matrix4x4f localMat = _localTransform;
                Vertex3f lowerZAxis = localMat.Column2.Vertex3f();
                lowerZAxis.z = 0;
                lowerZAxis = lowerZAxis.Normalized * Math.Sin(_restrictAngle.ConstraintAngle.w.ToRadian());
                lowerZAxis.z = (float)Math.Cos(_restrictAngle.ConstraintAngle.w.ToRadian());
                Matrix4x4f localRotMat = localMat.Column2.Vertex3f().RotateBetween(lowerZAxis);
                _localTransform = localMat * localRotMat;
                UpdatePropTransform(isSelfIncluded: true);
            }
        }

        public void ModifyRoll(float phi)
        {
            if (phi < _restrictAngle.TwistAngle.x)
            {
                Matrix4x4f localMat = _localTransform;
                Vertex3f lowerZAxis = localMat.Column2.Vertex3f();
                lowerZAxis.z = 0;
                lowerZAxis = lowerZAxis.Normalized * -Math.Sin(_restrictAngle.ConstraintAngle.z.ToRadian());
                lowerZAxis.y = (float)Math.Cos(_restrictAngle.ConstraintAngle.z.ToRadian());
                Matrix4x4f localRotMat = localMat.Column2.Vertex3f().RotateBetween(lowerZAxis);
                _localTransform = localMat * localRotMat;
                UpdatePropTransform(isSelfIncluded: true);
            }
            else if (phi > _restrictAngle.TwistAngle.y)
            {
                Matrix4x4f localMat = _localTransform;
                Vertex3f lowerZAxis = localMat.Column2.Vertex3f();
                lowerZAxis.z = 0;
                lowerZAxis = lowerZAxis.Normalized * Math.Sin(_restrictAngle.ConstraintAngle.w.ToRadian());
                lowerZAxis.z = (float)Math.Cos(_restrictAngle.ConstraintAngle.w.ToRadian());
                Matrix4x4f localRotMat = localMat.Column2.Vertex3f().RotateBetween(lowerZAxis);
                _localTransform = localMat * localRotMat;
                UpdatePropTransform(isSelfIncluded: true);
            }
        }


        /// <summary>
        /// 피봇의 위치로부터 보는 곳의 위치로 좌표프레임을 변환한다.
        /// </summary>
        /// <param name="pivotPosition">피봇의 월드 위치</param>
        /// <param name="lookAt">보는 곳의 월드 위치</param>
        /// <param name="upVector">Vertex3f.UnitZ를 기본 사용 권장</param>
        public void ApplyCoordinateFrame(Vertex3f pivotPosition, Vertex3f lookAt, Vertex3f upVector, bool isRestrictAngle = true)
        {
            Vertex3f eyePos = _animatedTransform.Position + pivotPosition; // 캐릭터의 월드 공간 위치에 뼈의 위치를 더한다.

            Vertex3f z = (lookAt - eyePos).Normalized;
            Vertex3f x = upVector.Cross(z).Normalized;
            Vertex3f y = z.Cross(x).Normalized;

            Matrix4x4f loc = _localBindTransform;

            Matrix3x3f frame = Matrix3x3f.Identity.Frame(x, y, z) * _localBindTransform.Rot3x3f();
            Vertex3f pos = _animatedTransform.Position;
            _animatedTransform = frame.ToMat4x4f(pos);
            UpdateLocalTransform();

            // 제한된 각도를 벗어나면 제한된 각도로 회귀한다.
            if (isRestrictAngle)
            {
                Vertex3f angleVector = Kinetics.EulerAngleFromRotationMatrixZYX(_localTransform.Rot3x3f())[0]; // 오일러 각을 가져온다.
                angleVector.x = angleVector.x.Clamp(this.RestrictAngle.ConstraintAngle.x, this.RestrictAngle.ConstraintAngle.y);
                angleVector.y = angleVector.y.Clamp(this.RestrictAngle.TwistAngle.x, this.RestrictAngle.TwistAngle.y);
                angleVector.z = angleVector.z.Clamp(this.RestrictAngle.ConstraintAngle.z, this.RestrictAngle.ConstraintAngle.w);
                Matrix4x4f RotX = Matrix4x4f.RotatedX(angleVector.x);
                Matrix4x4f RotY = Matrix4x4f.RotatedY(angleVector.y);
                Matrix4x4f RotZ = Matrix4x4f.RotatedZ(angleVector.z); // 회전을 계산한다.
                pos = _localTransform.Position;
                Matrix4x4f Rot = Matrix4x4f.Translated(pos.x, pos.y, pos.z) * RotZ * RotY * RotX; // 회전과 이동을 계산한다.
                float sx = _localTransform.Column0.Vertex3f().Norm();
                float sy = _localTransform.Column1.Vertex3f().Norm();
                float sz = _localTransform.Column2.Vertex3f().Norm();
                _localTransform = Rot * Matrix4x4f.Scaled(sx, sy, sz) * 100f; // 원본의 행렬의 열벡터의 크기를 가져온다.
                UpdatePropTransform(isSelfIncluded: true);
            }
        }

        /// <summary>
        /// 현재 뼈에 캐릭터 공간 회전행렬을 적용하여 부모뻐로부터의 뒤틀림(twist) 회전각을 가져온다.
        /// </summary>
        /// <param name="rotAMat"></param>
        /// <returns></returns>
        public float TwistAngle(Matrix4x4f rotAMat)
        {
            Matrix4x4f BrAMat = _animatedTransform;
            BrAMat = _parent.AnimatedTransform.Inverse * rotAMat * BrAMat;
            Vertex3f nX = BrAMat.Column0.Vertex3f().Normalized;
            float theta = (float)Math.Acos(Vertex3f.UnitX.Dot(nX)) * 180.0f / 3.141502f;
            theta = nX.z < 0 ? theta : -theta;
            return theta;
        }


    }
}
