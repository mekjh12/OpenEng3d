using Geometry;
using OpenGL;
using Physics.Collision;
using Physics.Rigid;
using System;
using ZetaExt;

namespace Physics
{
    /// <summary>
    /// 강체 클래스
    /// </summary>
    public abstract class RigidBody: BaseRigidBody, IRigidBody, ICollisionable
    {
        protected Matter _matter;                                     // 물질 정보
        protected float _volume = 0.0f;                               // 부피
        protected float _inverseMass = 1.0f;                          // 질량의 역수
        protected float _linearDamping = 0.99f;                       // 선형 감쇠 계수
        protected float _angularDamping = 0.79f;                      // 각 감쇠 계수
        protected Vertex3f _position = Vertex3f.Zero;                 // 위치
        protected Vertex3f _velocity = Vertex3f.Zero;                 // 속도 
        protected Vertex3f _rotation = Vertex3f.Zero;                 // 회전 각도

        protected Quaternion4 _orientation = Quaternion4.Identity;     // 방향 쿼터니언
        protected Matrix4x4f _transformMatrix = Matrix4x4f.Identity;   // 변환 행렬

        protected Matrix3x3f _inverseInertiaTensor = Matrix3x3f.Identity;        // 관성 텐서 역행렬
        protected Matrix3x3f _inverseInertiaTensorWorld = Matrix3x3f.Identity;   // 월드 공간 관성 텐서 역행렬

        protected AABB _aabb;                                         // 바운딩 박스
        protected CollisionPrimitive _primitive;                      // 충돌 프리미티브

        protected bool _isAwake = true;                              // 활성화 상태
        protected bool _canSleep = true;                             // 휴면 가능 상태
        protected float _motion = 1.0f;                              // 운동량

        protected Vertex3f _forceAccum = Vertex3f.Zero;              // 힘 누적
        protected Vertex3f _torgueAccum = Vertex3f.Zero;             // 토크 누적
        protected Vertex3f _acceleration = Vertex3f.Zero;            // 가속도
        protected Vertex3f _angularAcceleration = Vertex3f.Zero;     // 각가속도
        protected Vertex3f _lastFrameAcceleration = Vertex3f.Zero;   // 이전 프레임 가속도

        private bool _isRegisty = false;                             // 등록 상태

        /// <summary>
        /// 월드공간에서의 강체의 회전자세 쿼터니온
        /// </summary>
        public Quaternion4 Orientation
        {
            get => _orientation;
            set => _orientation = value;
        }

        /// <summary>
        /// 강체가 앞으로 나가는 월드좌표 방향벡터
        /// </summary>
        public Vertex3f Forward
        {
            get => _transformMatrix.Column0.xyz();
        }

        /// <summary>
        /// 강체의 위쪽 월드좌표 벡터
        /// </summary>
        public Vertex3f Up
        {
            get => _transformMatrix.Column2.xyz();
        }

        /// <summary>
        /// 강체의 오른쪽 월드좌표 벡터
        /// </summary>
        public Vertex3f Right
        {
            get => _transformMatrix.Column1.xyz();
        }

        /// <summary>
        /// 강체의 활성화 여부
        /// </summary>
        public bool IsAwake
        {
            get => _isAwake;
            set => _isAwake = value;
        }

        /// <summary>
        /// 강체의 가속도를 유지하는 용도로 사용한다.
        /// 이 값은 중력(주 용도)로 인한 가속도나 기타 일정한 가속도를 사용할 수 있다.
        /// </summary>
        public Vertex3f Acceleration
        {
            get => _acceleration; 
            set => _acceleration = value;
        }

        /// <summary>
        /// 가장 최근의 이전 프레임에서의 강체의 가속도
        /// </summary>
        public Vertex3f LastFrameAcceleration
        {
            get => _lastFrameAcceleration;
        }

        /// <summary>
        /// 강체의 월드 변환 행렬
        /// </summary>
        public Matrix4x4f TransformMatrix => _transformMatrix;

        /// <summary>
        /// 강체의 부피
        /// </summary>
        public float Volume => _volume;

        /// <summary>
        /// 월드공간에서의 강체의 각속도벡터
        /// </summary>
        public Vertex3f Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        /// <summary>
        /// 강체가 강체보관소에 등록이 되어 있는지 여부를 반환한다.
        /// </summary>
        public bool IsRegisty
        {
            get => _isRegisty;
            set => _isRegisty = value;
        }

        /// <summary>
        /// 바운딩 박스
        /// </summary>
        public AABB AABB
        {
            get => _aabb;
        }

        /// <summary>
        /// 충돌 원형
        /// </summary>
        public CollisionPrimitive Primitive
        {
            get => _primitive;
        }

        /// <summary>
        /// 강체의 선속도벡터
        /// </summary>
        public Vertex3f Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        /// <summary>
        /// 강체의 질량이 유한인지 반환한다.
        /// </summary>
        public bool HasFiniteMass => _inverseMass >= 0.0f;

        /// <summary>
        /// 강체의 월드 공간에서의 관성텐서의 역행렬
        /// </summary>
        public Matrix3x3f InverseInertiaTensorWorld
        {
            get => _inverseInertiaTensorWorld;
            set => _inverseInertiaTensorWorld = value;
        }

        /// <summary>
        /// 강체의 질량
        /// </summary>
        public float Mass
        {
            get => 1.0f / _inverseMass;
            set => _inverseMass = 1.0f / value;
        }

        /// <summary>
        /// 강체의 질량의 역수
        /// </summary>
        public float InverseMass
        {
            get => _inverseMass;
            set => _inverseMass = value;
        }

        /// <summary>
        /// 강체의 월드공간에서의 위치
        /// </summary>
        public Vertex3f Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public RigidBody(Matter matter) : base()
        {
            _matter = matter;
            _orientation = Quaternion4.Identity;
        }

        /// <summary>
        /// 강체의 월드변환행렬, 월드관성텐서, 바운딩볼륨, 충돌원형을 계산한다.
        /// </summary>
        protected void CalculateDerivedData()
        {
            // 강체의 자세 변환 행렬을 계산한다.
            _orientation.Normalize();
            _transformMatrix = _orientation.ToMatrixt4x4f(_position);

            // 강체의 월드공간에서의 관성텐서를 계산한다.
            _inverseInertiaTensorWorld = TransformInertiaTensor(_inverseInertiaTensor, _transformMatrix);

            // 강체의 모양에 따라 바운딩볼륨을 계산한다.
            CalculateBoundingVolume();

            // 강체의 모양에 따라 충돌 원형을 계산한다.
            CalculatePrimitive();
        }

        /// <summary>
        /// 강체의 월드공간에서의 관성텐서를 계산한다.
        /// </summary>
        /// <param name="inertiaLocalTensor"></param>
        /// <param name="worldTransMatrix"></param>
        /// <returns></returns>
        protected Matrix3x3f TransformInertiaTensor(Matrix3x3f inertiaLocalTensor, Matrix4x4f worldTransMatrix)
        {
            // W = RBR^{-1}
            // B 기본 관성텐서
            // R 강체의 월드변환행렬
            Matrix3x3f rot = worldTransMatrix.Rot3x3f();
            return rot * inertiaLocalTensor * rot.Inverse;
        }

        /// <summary>
        /// 힘을 누적하여 속도와 위치를 업데이트한다.
        /// </summary>
        /// <param name="duration"></param>
        public void Integrate(float duration)
        {
            // 입력된 힘에 의해 선형 가속도를 계산한다.
            // 다음 프레임에서 사용한 선형 가속도를 이용할 때 
            // _lastFrameAcceleration 변수를 사용할 수 있다.

            // 강체의 힘생성기외에 힘이 작용할 수도 있으므로
            _lastFrameAcceleration = _acceleration; 
            _lastFrameAcceleration += _forceAccum * _inverseMass;

            // 입력된 토크에 의해 관성텐서를 이용하여 각가속도를 계산한다.
            _angularAcceleration = _inverseInertiaTensorWorld * _torgueAccum;

            // 속도를 보정한다.
            // 가속도와 충격량으로부터 선속도를 업데이트한다.
            _velocity += _lastFrameAcceleration * duration;

            // 각가속도와 충격량으로부터 각속도를 업데이트한다.
            _rotation += _angularAcceleration * duration;

            // 드래그를 적용한다.
            _velocity *= Math.Pow(_linearDamping, duration);
            _rotation *= Math.Pow(_angularDamping, duration);

            // 위치를 보정한다.
            _position += _velocity * duration;

            // 방향쿼터니온을 업데이트한다.
            // q = q + deltaT/2 * w * q
            Quaternion4 q = new Quaternion4(_rotation) * duration;
            q *= _orientation;
            _orientation += q * 0.5f;

            // 방향을 정규화하고, 새로운 위치와 방향으로 행렬을 업데이트한다.
            // 강체의 월드변환행렬, 월드관성텐서, 바운딩볼륨, 충돌원형을 계산한다.
            CalculateDerivedData();

            // 누적기를 초기화한다.
            ClearAccumulator();
        }

        /// <summary>
        /// 누적기를 초기화한다.
        /// </summary>
        private void ClearAccumulator()
        {
            _forceAccum = Vertex3f.Zero;
            _torgueAccum = Vertex3f.Zero;
        }

        /// <summary>
        /// 강체에 힘을 준다.
        /// </summary>
        /// <param name="force">월드공간에서의 힘벡터</param>
        public void AddForce(Vertex3f force)
        {
            _forceAccum += force;
        }

        /// <summary>
        /// 물체의 특정 지점에 힘을 가한다. <br/>
        /// 힘의 방향과 작용점의 위치는 월드좌표계로 표시된다.<br/>
        /// 힘이 물체 중심에 작용하는 것이므로 힘과 토크로 분리될 수도 있다.<br/>
        /// </summary>
        /// <param name="force"></param>
        /// <param name="point"></param>
        public void AddForceAtPoint(Vertex3f force, Vertex3f point)
        {
            Vertex3f pt = point;
            pt -= _position;

            _forceAccum += force;
            _torgueAccum += pt.Cross(force);

            _isAwake = true;
        }

        /// <summary>
        /// 월드공간의 접촉점에서의 속도벡터
        /// </summary>
        /// <param name="contactPoint">접촉점은 월드좌표</param>
        /// <returns>월드좌표의 속도벡터</returns>
        public Vertex3f GetWorldVelocity(Vertex3f contactPoint)
        {
            // 접촉점에서의 회전속도와 선속도를 합하여 접촉점에서의 월드공간의 속도벡터를 계산한다.
            Vertex3f relativeContactPosition = contactPoint - _position;
            Vertex3f velocity = _rotation.Cross(relativeContactPosition);
            velocity += _velocity;
            return velocity;
        }

        /// <summary>
        /// 강체의 접촉점에서의 접촉법선방향으로의 단위 임펄스(unit-impulse)마다의 속도의 변화량을 가져온다.
        /// </summary>
        /// <param name="contactPoint"></param>
        /// <returns></returns>
        public float GetVelocityPerUnitImpulse(Vertex3f contactPoint, Vertex3f contactNormal)
        {
            // 단위 임펄스의 회전에 의해 결정되는 속도
            Vertex3f relativeContactPosition = contactPoint - _position;
            Vertex3f torquePerUnitImpulse = relativeContactPosition.Cross(contactNormal);
            Vertex3f rotationPerUnitImpulse = _inverseInertiaTensorWorld * torquePerUnitImpulse;
            Vertex3f velocityPerUnitImpulse = rotationPerUnitImpulse.Cross(relativeContactPosition);

            // 접촉 좌표에서 발생하는 회전에 의한 속도의 변화를 구한다.
            float angularComponent = velocityPerUnitImpulse * contactNormal;
            float deltaVelocity = angularComponent;

            // 속도 변화에 선성분을 추가한다.
            deltaVelocity += _inverseMass;

            return deltaVelocity;
        }

        /// <summary>
        /// 강체의 모양에 따라 관성텐서를 지정한다.
        /// </summary>
        public abstract void InitInertiaTensor();

        /// <summary>
        /// 강체의 모양에 따라 물질의 상태에 따라 질량을 계산한다.
        /// </summary>
        public virtual void CalculateMass()
        {
            float mass = _matter.SpecificGravity * _volume;
            _inverseMass = 1.0f / mass;
            Console.WriteLine($"물질의 질량 {mass}");
        }

        /// <summary>
        /// 강체의 모양에 따라 AABB 바운딩볼륨을 계산한다.
        /// </summary>
        public abstract void CalculateBoundingVolume();

        /// <summary>
        /// 매 프레임마다 강체의 모양에 따라 충돌 원형을 계산한다.
        /// </summary>
        public abstract void CalculatePrimitive();
    }
}
