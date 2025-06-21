using System;
using OpenGL;
using ZetaExt;

namespace Physics.Collision
{
    public class Contact
    {
        Vertex3f _contactPoint; // 접촉점
        Vertex3f _contactNormal; // 접촉법선
        float _penetration = 0.0f; // 관통깊이
        float _desiredDeltaVelocity = 0.0f;

        /// <summary>
        /// 마찰력
        /// </summary>
        float _friction = 0.0f;

        /// <summary>
        /// 반발계수
        /// </summary>
        float _restitution = 0.0f;
        RigidBody[] _body; // 두 강체

        /// <summary>
        /// 접촉점의 월드변환행렬
        /// </summary>
        Matrix3x3f _contactWorld = Matrix3x3f.Identity;

        // 내부 계산을 위한 변수
        Vertex3f[] _relativeContactPosition = new Vertex3f[2];
        Vertex3f _contactClosedVelocity = Vertex3f.Zero;

        /// <summary>
        /// 접촉에 의하여 분리되어지는 분리속도(접촉점의 법선벡터가 양이다.)
        /// </summary>
        public float DesiredDeltaVelocity
        {
            get => _desiredDeltaVelocity;
        }

        /// <summary>
        /// 접촉점의 로컬공간을 월드공간으로의 변환행렬
        /// </summary>
        public Matrix3x3f ContactWorldTransform
        {
            get => _contactWorld;
        }

        /// <summary>
        /// 접근 속도의 합벡터
        /// </summary>
        public Vertex3f ContactClosedVelocity
        {
            get => _contactClosedVelocity;
            set => _contactClosedVelocity = value;
        }

        /// <summary>
        /// 두 강체의 중심으로부터의 상대적 월드좌표
        /// </summary>
        public Vertex3f[] RelativeContactPosition
        {
            get => _relativeContactPosition;
        }

        /// <summary>
        /// 반발계수
        /// </summary>
        public float Restitution
        {
            get => _restitution;
            set => _restitution = value;
        }

        /// <summary>
        /// 접촉점
        /// </summary>
        public Vertex3f ContactPoint
        {
            get => _contactPoint;
            set => _contactPoint = value;
        }

        /// <summary>
        /// 마찰계수
        /// </summary>
        public float Friction
        {
            get => _friction;
            set => _friction = value;
        }

        /// <summary>
        /// 관통깊이
        /// </summary>
        public float Penetration
        {
            get => _penetration;
            set => _penetration = value;
        }

        /// <summary>
        /// 접촉법선
        /// </summary>
        public Vertex3f ContactNormal
        {
            get => _contactNormal;
            set => _contactNormal = value;
        }

        /// <summary>
        /// 충돌한 두 개의 강체
        /// </summary>
        public RigidBody[] RigidBodies
        {
            get => _body;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public Contact()
        {
            _body = new RigidBody[2];
        }

        /// <summary>
        /// 접촉데이터를 설정한다.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="friction">마찰</param>
        /// <param name="restitution">반환</param>
        public void SetBodyData(RigidBody one, RigidBody two, float friction, float restitution)
        {
            if (_body == null) _body = new RigidBody[2];

            _body[0] = one;
            _body[1] = two;

            _friction = friction;
            _restitution = restitution;
        }

        /// <summary>
        /// 두 강체의 인덱스 위치를 교환한다.
        /// </summary>
        private void SwapBodies()
        {
            _contactNormal *= -1.0f;
            RigidBody temp = _body[0];
            _body[0] = _body[1];
            _body[1] = temp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Vertex3f CalculateFrictionlessImpulse(float desiredDeltaVelocity)
        {
            // 단위 임펄스에 대한 접촉점에서의 접촉법선벡터방향의 속도의 변화량을 계산한다.
            float deltaVelocity = _body[0].GetVelocityPerUnitImpulse(_contactPoint, _contactNormal);
            if (_body[1] != null)
            {
                deltaVelocity += _body[1].GetVelocityPerUnitImpulse(_contactPoint, _contactNormal);
            }

            // 속도에 의한 충격량의 변화로 이는 접촉점의 좌표계의 x좌표와 평행하다.
            Vertex3f impulseContact = new Vertex3f(desiredDeltaVelocity / deltaVelocity, 0.0f, 0.0f);
            return impulseContact;
        }

        public void ApplyPositionChange(float duration, ref Vertex3f[] linearChange, ref Vertex3f[] angularChange, ref float penetration)
        {
            const float ANGULAR_LIMIT = 0.002f;
            float[] angularMove = new float[2];
            float[] linearMove = new float[2];

            float totalInertia = 0.0f;
            float[] linearInertia = new float[2];
            float[] angularInertia = new float[2];

            for (int i = 0; i < 2; i++)
            {
                if (_body[i] == null) continue;

                RigidBody body = _body[i];

                // 마찰이 없는 접촉평면에서의 각관성에 따른 속도의 변화를 계산한다.
                Matrix3x3f inverseInertiaTensorWorld = body.InverseInertiaTensorWorld;
                Vertex3f relativeContactPosition = _contactPoint - body.Position;
                Vertex3f angularInertiaWorld = relativeContactPosition.Cross(_contactNormal);
                angularInertiaWorld = inverseInertiaTensorWorld * angularInertiaWorld;
                Vertex3f angularVelocityLocal = angularInertiaWorld.Cross(relativeContactPosition);
                angularInertia[i] = angularInertiaWorld * _contactNormal;

                // 선성분에 대한 질량의 역수를 계산한다.
                linearInertia[i] = body.InverseMass;

                // 두 강체가 접촉점에서의 4개의 각과 선성분의 관성을 모두 더한 값이다.
                totalInertia += linearInertia[i] + angularInertia[i];
            }

            for (int i = 0; i < 2; i++)
            {
                if (_body[i] == null) continue;
                RigidBody body = _body[i];

                // 선운동, 각운동 
                float sign = (i == 0) ? 1.0f : -1.0f;
                angularMove[i] = sign * _penetration * (angularInertia[i] / totalInertia);
                linearMove[i] = sign * _penetration * (linearInertia[i] / totalInertia);
            }

            for (int i = 0; i < 2; i++)
            {
                if (_body[i] == null) continue;
                RigidBody body = _body[i];

                // 선운동, 각운동 
                float sign = (i == 0) ? 1.0f : -1.0f;
                angularMove[i] = sign * _penetration * (angularInertia[i] / totalInertia);
                linearMove[i] = sign * _penetration * (linearInertia[i] / totalInertia);

                Vertex3f projection = _relativeContactPosition[i];
                projection += _contactNormal * (_relativeContactPosition[i] * _contactNormal);

                // 과회전방지
                float maxMagnitude = ANGULAR_LIMIT * projection.Magnitude();
                if (angularMove[i] < -maxMagnitude)
                {
                    float totalMove= angularMove[i] + linearMove[i];
                    angularMove[i] = -maxMagnitude;
                    linearMove[i] = totalMove - angularMove[i];
                }
                else if (angularMove[i] > maxMagnitude)
                {
                    float totalMove = angularMove[i] + linearMove[i];
                    angularMove[i] = maxMagnitude;
                    linearMove[i] = totalMove - angularMove[i];
                }

                if (angularMove[i] == 0.0f)
                {

                }
                else
                {
                    Vertex3f targetAngularDirection = _relativeContactPosition[i].Cross(_contactNormal);
                    Matrix3x3f inverseInertiaTensorWorld = body.InverseInertiaTensorWorld;
                    angularChange[i] = inverseInertiaTensorWorld * targetAngularDirection * (angularMove[i] / angularInertia[i]);
                }

                linearChange[i] = _contactNormal * linearMove[i];

                // 선형 이동을 적용한다.
                body.Position += linearChange[i];

                // 각이동에 따른 방향을 적용한다.
                // 방향쿼터니온을 업데이트한다.
                // q = q + deltaT/2 * w * q
                Quaternion4 q = body.Orientation;
                Quaternion4 delQuaternion = new Quaternion4(angularChange[i]);
                body.Orientation = q + delQuaternion * q * 0.5f;
            }
        }

        /// <summary>
        /// 접촉점에서 반발계수에 따른 충격량을 계산하여 두 강체의 속도와 회전을 변화시킨다.
        /// </summary>
        public void ApplyVelocityChange(ref Vertex3f[] velocityChange, ref Vertex3f[] rotationChange)
        {
            // 접촉점의 로컬공간에서의 충격량
            Vertex3f impulseContact = Vertex3f.Zero;
            if (_friction == 0.0f)
                impulseContact = CalculateFrictionlessImpulse(_desiredDeltaVelocity);
            else
                impulseContact = CalculateFrictionlessImpulse(_desiredDeltaVelocity);

            // 접촉점의 월드공간에서의 충격량
            Vertex3f impulse = _contactWorld * impulseContact;

            // 충격량을 적용하여 첫번째 강체에 선속도와 각속도를 반영한다.
            Vertex3f impulsiveTorque = _relativeContactPosition[0].Cross(impulse);
            rotationChange[0] = _body[0].InverseInertiaTensorWorld * impulsiveTorque;
            velocityChange[0] = impulse * _body[0].InverseMass;

            _body[0].Velocity += velocityChange[0];
            _body[0].Rotation += rotationChange[0];

            // 충격량을 적용하여 두번째 강체에 선속도와 각속도를 반영한다. (충격량은 반대이다.)
            if (_body[1] != null)
            {
                impulsiveTorque = impulse.Cross(_relativeContactPosition[1]);
                rotationChange[1] = _body[1].InverseInertiaTensorWorld * impulsiveTorque;
                velocityChange[1] = impulse * -_body[1].InverseMass;

                _body[1].Velocity += velocityChange[1];
                _body[1].Rotation += rotationChange[1];
            }
        }

        /// <summary>
        /// 1. 접촉점의 로컬좌표계<br/>
        /// 2. 강체의 접근속도의 합<br/>
        /// 3. 접촉점의 두 강체의 상대좌표<br/>
        /// 4. 반발계수에 따른 강체의 분리를 위한 속도의 합<br/>
        /// </summary>
        /// <param name="duration"></param>
        public void CalculateInternals(float duration)
        {
            // 첫번째 강체가 Null이면 두 강체를 교환하여 첫번째 강체가 널이 되지 않도록 한다.
            if (_body[0] == null) SwapBodies();

            // 접촉점을 접촉법선을 x축으로 정규직교기저를 만든다.
            CalculateContactBasis();
            //Debug.RenderLines.Add(new RenderLine(_contactPoint, _contactPoint + _contactWorld.Column0, new Vertex4f(1, 0, 0, 1), 3.0f));
            //Debug.RenderLines.Add(new RenderLine(_contactPoint, _contactPoint + _contactWorld.Column1, new Vertex4f(0, 1, 0, 1), 3.0f));
            //Debug.RenderLines.Add(new RenderLine(_contactPoint, _contactPoint + _contactWorld.Column2, new Vertex4f(0, 0, 1, 1), 3.0f));

            // 두 강체의 원점으로부터 접촉점의 상대좌표를 계산한다.
            _relativeContactPosition[0] = _contactPoint - _body[0].Position;
            if (_body[1] != null)
            {
                _relativeContactPosition[1] = _contactPoint - _body[1].Position;
            }

            // 두 강체로부터 접근속도의 합을 구한다.
            _contactClosedVelocity = CalculateLocalVelocity(0, duration);
            if (_body[1] != null)
            {
                // 강체의 로컬좌표로 계산하므로 1번째 강체의 접촉법선에 반대방향이므로
                // 속도를 더하지 않고 빼주어야 한다.
                _contactClosedVelocity -= CalculateLocalVelocity(1, duration);
            }

            // 반발계수에 따른 변경되어져야 할 속도(분리속도)를 계산한다.
            _desiredDeltaVelocity = CalculateDesiredDeltaVelocity(duration);
        }

        /// <summary>
        /// 충돌한 두 강체의 접근속도의 합의 속도가 두 강체의 특성에 따른
        /// 반발계수에 따른 분리속도를 위해 필요한 속도의 양을 계산한다.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public float CalculateDesiredDeltaVelocity(float duration)
        {
            const float velocityLimit = 10.05f;
            float velocityFromAcc = 0.0f;

            if (_body[0].IsAwake)
                velocityFromAcc += (_body[0].LastFrameAcceleration * duration) * _contactNormal;

            if (_body[1] != null &&  _body[1].IsAwake)
                velocityFromAcc -= (_body[1].LastFrameAcceleration * duration) * _contactNormal;

            // 반발 게수를 설정한다.
            float thisRestitution = _restitution;

            // 접근속도가 일정 속도 이하이면 반발계수를 0으로 하여 반발하지 않도록 한다.
            if (_contactClosedVelocity.x.Abs() < velocityLimit)
            {
                thisRestitution = 0.0f;
            }


            // 반발계수에 따른 분리속도를 위한 변화를 필요로 하는 속도를 반환한다.
            return -_contactClosedVelocity.x - thisRestitution * (_contactClosedVelocity.x);
        }

        /// <summary>
        /// 접촉점에서의 접촉좌표공간(접촉벡터x축의 정규직교기저)의 접근속도를 가져온다.
        /// </summary>
        /// <param name="bodyIndex"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private Vertex3f CalculateLocalVelocity(uint bodyIndex, float duration)
        {
            RigidBody rigidBody = _body[bodyIndex % 2];

            // 접촉점에서의 회전속도와 선속도를 합하여 접촉점에서의 월드공간의 속도벡터를 계산한다.
            Vertex3f velocity = rigidBody.GetWorldVelocity(_contactPoint);

            // 월드공간의 속도벡터를 접촉점의 로컬공간의 속도벡터로 변환한다.
            Matrix3x3f inverseContactWorld = _contactWorld.Transposed;
            Vertex3f contactVelocity = inverseContactWorld * velocity;

            // 강체의 업데이트를 이미 실행하였으므로, 강체에 작용한 힘을 계산하기 위하여
            Vertex3f accVelocity = rigidBody.LastFrameAcceleration * duration;

            // 월드공간의 가속도에 기인한 속도벡터를 접촉점의 로컬공간의 속도벡터로 변환한다.
            accVelocity = inverseContactWorld * accVelocity;

            // 접촉평면 안에서의 속도만 고려해야 하기 떄문에, 접촉성분 방향(x축)의 속도벡터는 무시한다.
            accVelocity.x = 0.0f;

            // 접촉평면 안에서의 속도벡터를 더한다.
            contactVelocity += accVelocity;

            return contactVelocity;
        }         

        /// <summary>
        /// 접촉에 대하여 임의의 정규직교기저를 만든다.</br>
        /// - 각 벡터가 열의 값으로 지정되는 3x3행렬을 만든다.</br>
        /// - 로컬 공간의 좌표가 월드좌표로 변환하는 행렬을 만든다.</br>
        /// </summary>
        private void CalculateContactBasis()
        {
            //                 x-axis (contactNormal)
            //            _______|_______________
            //           /       |              /
            //          /        |____ z-axis  /
            //         /        /             /
            //        /       y-axis         /
            //       /______________________/

            Vertex3f[] contactTangent = new Vertex3f[2];

            float x = _contactNormal.x;
            float y = _contactNormal.y;
            float z = _contactNormal.z;

            if (x.Abs() > y.Abs())
            {
                float s = 1.0f / (z * z + x * x).Sqr();
                contactTangent[0].x = z * s;
                contactTangent[0].y = 0.0f;
                contactTangent[0].z = -x * s;

                contactTangent[1] = _contactNormal.Cross(contactTangent[0]);
            }
            else
            {
                float s = 1.0f / (y * y + z * z).Sqr();
                contactTangent[0].x = 0.0f;
                contactTangent[0].y = -z * s;
                contactTangent[0].z = y * s;

                contactTangent[1] = _contactNormal.Cross(contactTangent[0]);
            }

            // 최종 3개의 x,y,z축으로 월드변환행렬(정규직교기저)을 만든다.
            _contactWorld.Framed(_contactNormal, contactTangent[0], contactTangent[1]);
        }
    }
}
