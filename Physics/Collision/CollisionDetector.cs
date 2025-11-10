using FastMath;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Physics.Collision
{
    /// <summary>
    /// 충돌 검출기
    /// </summary>
    public class CollisionDetector
    {
        CollisionData _collisionData; // 충돌 데이터 집합
        ContactResolver _contactResolver; // 충돌 처리

        public static bool VisibleContactPoint = true;

        /// <summary>
        /// 생성자
        /// </summary>
        public CollisionDetector()
        {
            _collisionData = new CollisionData(contactMaxCount: 1000);
            _contactResolver = new ContactResolver();
        }

        /// <summary>
        /// 미세 충돌 검사를 업데이트한다.
        /// </summary>
        /// <param name="paired"></param>
        public void Update(List<RigidBodyPair> paired, float duration)
        {
            // 광역 충돌검사를 통과한 강체쌍을 미세 충돌 검사한다.
            FineCollisionTest(paired);

            // 
            _contactResolver.ResolveContacts(_collisionData.Contacts, duration);
        }

        /// <summary>
        /// 강체쌍을 모두 순회하면서 미세 충돌을 검사한다.
        /// </summary>
        /// <param name="paired"></param>
        protected void FineCollisionTest(List<RigidBodyPair> paired)
        {
            _collisionData.Clear();

            foreach (RigidBodyPair pair in paired)
            {
                pair.RigidBodyA.Primitive.RigidBody = pair.RigidBodyA;
                pair.RigidBodyB.Primitive.RigidBody = pair.RigidBodyB;

                // 구와 구의 충돌 검사
                if (pair.RigidBodyA.Primitive is CollisionSphere && pair.RigidBodyB.Primitive is CollisionSphere)
                {
                    SphereAndSphere(pair.RigidBodyA.Primitive as CollisionSphere, pair.RigidBodyB.Primitive as CollisionSphere, ref _collisionData);
                }

                // 박스와 박스의 충돌 검사
                if (pair.RigidBodyA.Primitive is CollisionBox && pair.RigidBodyB.Primitive is CollisionBox)
                {
                    BoxAndBox(pair.RigidBodyA.Primitive as CollisionBox, pair.RigidBodyB.Primitive as CollisionBox, ref _collisionData);
                }

                // 구와 평면의 충돌 검사
                if (pair.RigidBodyA.Primitive is CollisionSphere && pair.RigidBodyB.Primitive is CollisionPlane ||
                    pair.RigidBodyB.Primitive is CollisionSphere && pair.RigidBodyA.Primitive is CollisionPlane)
                {
                    CollisionSphere sphere;
                    CollisionPlane plane;
                    RigidPlane rigidPlane;
                    if (pair.RigidBodyA.Primitive is CollisionSphere)
                    {
                        sphere = (CollisionSphere)pair.RigidBodyA.Primitive;
                        plane = (CollisionPlane)pair.RigidBodyB.Primitive;
                        rigidPlane = (RigidPlane)pair.RigidBodyB;
                    }
                    else
                    {
                        sphere = (CollisionSphere)pair.RigidBodyB.Primitive;
                        plane = (CollisionPlane)pair.RigidBodyA.Primitive;
                        rigidPlane = (RigidPlane)pair.RigidBodyA;
                    }

                    if (rigidPlane.IsHalfPlane)
                    {
                        SphereAndHalfPlane(sphere, plane, ref _collisionData);
                    }
                    else
                    {
                        SphereAndTruePlane(sphere, plane, ref _collisionData);
                    }
                }

                // 박스와 평면의 충돌 검사
                if (pair.RigidBodyA.Primitive is CollisionBox && pair.RigidBodyB.Primitive is CollisionPlane ||
                    pair.RigidBodyB.Primitive is CollisionBox && pair.RigidBodyA.Primitive is CollisionPlane)
                {
                    CollisionBox box;
                    CollisionPlane plane;
                    if (pair.RigidBodyA.Primitive is CollisionBox)
                    {
                        box = (CollisionBox)pair.RigidBodyA.Primitive;
                        plane = (CollisionPlane)pair.RigidBodyB.Primitive;
                    }
                    else
                    {
                        box = (CollisionBox)pair.RigidBodyB.Primitive;
                        plane = (CollisionPlane)pair.RigidBodyA.Primitive;
                    }

                    BoxAndHalfPlane(box, plane, ref _collisionData);
                }

                // 박스와 구의 충돌 검사
                if (pair.RigidBodyA.Primitive is CollisionBox && pair.RigidBodyB.Primitive is CollisionSphere ||
                    pair.RigidBodyB.Primitive is CollisionBox && pair.RigidBodyA.Primitive is CollisionSphere)
                {
                    CollisionBox box;
                    CollisionSphere sphere;
                    if (pair.RigidBodyA.Primitive is CollisionBox)
                    {
                        box = (CollisionBox)pair.RigidBodyA.Primitive;
                        sphere = (CollisionSphere)pair.RigidBodyB.Primitive;
                    }
                    else
                    {
                        box = (CollisionBox)pair.RigidBodyB.Primitive;
                        sphere = (CollisionSphere)pair.RigidBodyA.Primitive;
                    }

                    BoxAndSphere(box, sphere, ref _collisionData);
                }
            }

            Debug.Write("충돌점=" + _collisionData.Contacts.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static uint BoxAndBox(CollisionBox one, CollisionBox two, ref CollisionData data)
        {
            // 두 중심 사이의 벡터 (one ---> two)
            Vertex3f toCenter = two.Position - one.Position;

            float penetration = float.MaxValue;
            int best = -1;

            // 0-2: 첫번째 상자의 면축, 3-5: 두번째 상자의 면축, 6-14 선선에 기반한 축
            // 각각의 축에 대하여 가장 작은 관통을 가진 축을 찾는다.
            bool intersected = true;
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisX, 0, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisY, 1, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisZ, 2, ref best, ref penetration);

            intersected &= IntersectionTest.TryAxis(one, two, two.AxisX, 3, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, two.AxisY, 4, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, two.AxisZ, 5, ref best, ref penetration);

            int bestSingleAxis = best;

            intersected &= IntersectionTest.TryAxis(one, two, one.AxisX.Cross(two.AxisX), 6, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisX.Cross(two.AxisY), 7, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisX.Cross(two.AxisZ), 8, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisY.Cross(two.AxisX), 9, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisY.Cross(two.AxisY), 10, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisY.Cross(two.AxisZ), 11, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisZ.Cross(two.AxisX), 12, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisZ.Cross(two.AxisY), 13, ref best, ref penetration);
            intersected &= IntersectionTest.TryAxis(one, two, one.AxisZ.Cross(two.AxisZ), 14, ref best, ref penetration);

            // 충돌이 일어나지 않으면 반환한다.
            if (!intersected) return 0;

            if (best < 3)
            {
                // 첫번째 강체의 면 법선이 최적인 경우
                IntersectionTest.FillPointFaceBoxBox(one, two, toCenter, ref data, best, penetration);
                //Console.WriteLine("1번 강체 점-면 충돌");
                return 1;
            }
            else if (best < 6)
            {
                // 두번째 강체의 면 법선이 최적인 경우
                IntersectionTest.FillPointFaceBoxBox(two, one, -toCenter, ref data, best - 3, penetration);
                //Console.WriteLine("2번 강체 점-면 충돌");
                return 1;
            }
            else if(best < 15)
            {
                // 선-선 접촉에 따른 최소 관통 두 축을 찾는다.
                // 3의 배수로 이루어진 IntersectionTest 테스트의 순서에 따라 지정된 것이다.
                best -= 6;
                int oneAxisIndex = best / 3;
                int twoAxisIndex = best % 3;
                Vertex3f oneAxis = one.Axis(oneAxisIndex);
                Vertex3f twoAxis = two.Axis(twoAxisIndex);
                Vertex3f axis = oneAxis.Cross(twoAxis);

                // 두 박스의 중심을 이은 축(axis)은 2번 박스의 중심에서 1번 박스의 중심을 향하는 벡터이다.
                if (axis * toCenter > 0) axis *= -1.0f;

                Vertex3f ptOnOneEdge = one.HalfSize; // 1박스의 모서리의 중점
                Vertex3f ptOnTwoEdge = two.HalfSize; // 2박스의 모서리의 중점
                for (int i = 0; i < 3; i++)
                {
                    if (i == oneAxisIndex) 
                        ptOnOneEdge = ptOnOneEdge.SetValue(i, 0);
                    else if (one.Axis(i) * axis > 0) 
                        ptOnOneEdge = ptOnOneEdge.SetValue(i, -ptOnOneEdge.GetValue(i));

                    if (i == twoAxisIndex) 
                        ptOnTwoEdge = ptOnTwoEdge.SetValue(i, 0);
                    else if (two.Axis(i) * axis < 0) 
                        ptOnTwoEdge = ptOnTwoEdge.SetValue(i, -ptOnTwoEdge.GetValue(i));
                }

                ptOnOneEdge = one.RigidBody.TransformMatrix.Transform(ptOnOneEdge);
                ptOnTwoEdge = two.RigidBody.TransformMatrix.Transform(ptOnTwoEdge);

                Vertex3f vertex = IntersectionTest.ContactPoint(
                    ptOnOneEdge, oneAxis, one.HalfSize.GetValue((int)oneAxisIndex),
                    ptOnTwoEdge, twoAxis, two.HalfSize.GetValue((int)twoAxisIndex), bestSingleAxis > 2);

                Debug.AddPoint(ptOnOneEdge, Color4f.Blue, 0.03f);
                Debug.AddPoint(ptOnTwoEdge, Color4f.Blue, 0.03f);
                Debug.AddVector(ptOnOneEdge - oneAxis, oneAxis * 2.0f, Color4f.Yellow, 0.5f);
                Debug.AddVector(ptOnTwoEdge - twoAxis, twoAxis * 2.0f, Color4f.Yellow, 0.5f);
                Debug.AddVector(vertex, axis, Color4f.Green, 0.5f);

                //접촉 데이터를 추가한다. 평면 방향으로의 법선을 가진다.
                Contact contact = new Contact()
                {
                    ContactNormal = axis.Normalized,
                    ContactPoint = vertex,
                    Penetration = penetration,
                };
                contact.SetBodyData(one.RigidBody, two.RigidBody, data.Friction, data.Restitution);
                data.AddContact(contact);

                // 디버깅을 위한 접촉점 보이기
                if (CollisionDetector.VisibleContactPoint) CollisionDetector.AddDebugContact(contact);
                //Console.WriteLine("선-선 충돌");

                return 1;
            }
            else
            {


                return 1;
            }
        }

        /// <summary>
        /// 박스와 구의 접촉점을 가져온다. 접촉점은 단 한개이다.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="sphere"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static uint BoxAndSphere(CollisionBox box, CollisionSphere sphere, ref CollisionData data)
        {
            // 접촉이 발생했는지 확인한다.
            if (data.ContactLeft <= 0) return 0;

            // 구의 중심을 박스 좌표로 역변환한다.
            Vertex3f center = sphere.Position;
            Vertex3f relCenter = (box.RigidBody.TransformMatrix.Inverse * center).xyz();

            // 접촉을 제외할 수 있는지 알아보기 위해 얼리아웃 체크를 한다.
            if (relCenter.x.Abs() - sphere.Radius > box.HalfSize.x ||
                relCenter.y.Abs() - sphere.Radius > box.HalfSize.y ||
                relCenter.z.Abs() - sphere.Radius > box.HalfSize.z )
            {
                return 0;
            }

            // 각 좌표를 박스에 고정시킨다.
            Vertex3f closestPt = Vertex3f.Zero;
            float dist = 0.0f;

            dist = relCenter.x;
            if (dist > box.HalfSize.x) dist = box.HalfSize.x;
            if (dist < -box.HalfSize.x) dist = -box.HalfSize.x;
            closestPt.x = dist;

            dist = relCenter.y;
            if (dist > box.HalfSize.y) dist = box.HalfSize.y;
            if (dist < -box.HalfSize.y) dist = -box.HalfSize.y;
            closestPt.y = dist;

            dist = relCenter.z;
            if (dist > box.HalfSize.z) dist = box.HalfSize.z;
            if (dist < -box.HalfSize.z) dist = -box.HalfSize.z;
            closestPt.z = dist;

            // 접촉이 발생하는지 확인한다.
            dist = (closestPt - relCenter).SquareMagnitude();
            if (dist > sphere.Radius * sphere.Radius) return 0;

            // 컴파일을 수행한다.
            Vertex3f closestPtWorld = box.RigidBody.TransformMatrix.Transform(closestPt);

            //접촉 데이터를 추가한다. 평면 방향으로의 법선을 가진다.
            Contact contact = new Contact()
            {
                ContactNormal = (closestPtWorld - center).Normalized,
                ContactPoint = closestPtWorld,
                Penetration = sphere.Radius - (float)MathFast.Sqrt(dist),
            };

            // 접촉점과 구의 중심이 일치하는 경우, 흔한 경우가 아니므로 충돌로 간주하지 않는다.
            if (contact.ContactNormal == Vertex3f.Zero) return 0;

            contact.SetBodyData(box.RigidBody, sphere.RigidBody, data.Friction, data.Restitution);
            data.AddContact(contact);

            // 디버깅을 위한 접촉점 보이기
            if (VisibleContactPoint) AddDebugContact(contact);

            return 1;
        }

        /// <summary>
        /// 박스와 반평면의 접촉점을 가져온다. 접촉점은 여러 개일 수 있다. 
        /// </summary>
        /// <param name="box"></param>
        /// <param name="plane"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static uint BoxAndHalfPlane(CollisionBox box, CollisionPlane plane, ref CollisionData data)
        {
            // 접촉이 발생했는지 확인한다.
            if (data.ContactLeft <= 0) return 0;

            // 교차를 체크한다.
            if (!IntersectionTest.BoxAndHalfSpace(box, plane)) return 0;

            Vertex3f[] mults = new Vertex3f[8] 
            {
                new Vertex3f(1, 1, 1), new Vertex3f(-1, 1, 1), new Vertex3f(1,-1, 1), new Vertex3f(-1,-1, 1),
                new Vertex3f(1, 1,-1), new Vertex3f(-1, 1,-1), new Vertex3f(1,-1,-1), new Vertex3f(-1,-1,-1),
            };

            uint contactUsed = 0;
            for (int i = 0; i < 8; i++)
            {
                Vertex3f vertexPos = mults[i];
                vertexPos = vertexPos.ComponentProduct(box.HalfSize);
                vertexPos = box.RigidBody.TransformMatrix.Multiply(vertexPos);

                // 점이 평면의 뒷면에 위치해 있으면
                float distance = plane.SignedDistance(vertexPos);
                if (distance <= 0.0f)
                {
                    //접촉 데이터를 추가한다. 평면 방향으로의 법선을 가진다.
                    Contact contact = new Contact()
                    {
                        ContactNormal = plane.Normal,
                        Penetration = - distance,
                        ContactPoint = vertexPos - plane.Normal * distance
                    };
                    contact.SetBodyData(box.RigidBody, plane.RigidBody, data.Friction, data.Restitution);

                    data.AddContact(contact);
                    contactUsed++;
                    if (contactUsed == data.ContactLeft) return contactUsed;
                }
            }

            return contactUsed;
        }

        /// <summary>
        /// 구와 평면의 접촉점을 가져온다. 접촉점은 한 개이다.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="plane"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static uint SphereAndTruePlane(CollisionSphere sphere, CollisionPlane plane, ref CollisionData data)
        {
            // 접촉이 발생했는지 확인한다.
            if (data.ContactLeft <= 0) return 0;

            // 구의 위치를 저장한다.
            Vertex3f position = sphere.Position;

            // 평면으로부터의 거리를 측정한다.
            float centerDistance = plane.SignedDistance(position);

            // 접촉이 발생하지 않으면 반환한다.
            if (centerDistance * centerDistance > sphere.Radius * sphere.Radius) return 0;

            // 평면이 어느 쪽에 위치하고 있는지 확인한다.
            Vertex3f normal = plane.Normal;
            float penetration = -centerDistance;
            if (centerDistance < 0)
            {
                normal *= -1.0f;
                penetration = -penetration;
            }
            penetration += sphere.Radius;

            //접촉데이터를 추가한다. 평면 방향으로의 법선을 가진다.
            Contact contact = new Contact()
            {
                ContactNormal = plane.Normal,
                ContactPoint = position - plane.Normal * centerDistance,
                Penetration = penetration
            };
            contact.SetBodyData(sphere.RigidBody, null, data.Friction, data.Restitution);

            return (uint)(data.AddContact(contact) ? 1 : 0);
        }

        /// <summary>
        /// 구와 반평면이 충돌하는지 검사한 후 충돌데이터를 삽입한다.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="plane"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static uint SphereAndHalfPlane(CollisionSphere sphere, CollisionPlane plane, ref CollisionData data)
        {
            // 접촉이 발생했는지 확인한다.
            if (data.ContactLeft <= 0) return 0;

            // 구의 위치를 저장한다.
            Vertex3f position = sphere.Position;

            // 평면으로부터의 거리를 측정한다.
            float ballDistance = plane.Normal.Dot(position) - sphere.Radius - plane.Distance;

            // 접촉이 발생하지 않으면 반환한다.
            if (MathFast.Abs(plane.Normal.Dot(position) - plane.Distance) >= sphere.Radius) return 0;

            //접촉데이터를 추가한다. 평면 방향으로의 법선을 가진다.
            Contact contact = new Contact()
            {
                ContactNormal = plane.Normal,
                ContactPoint = position - plane.Normal * (ballDistance + sphere.Radius),
                Penetration = -ballDistance
            };
            contact.SetBodyData(sphere.RigidBody, plane.RigidBody, data.Friction, data.Restitution);

            // 디버깅을 위한 접촉점 보이기
            if (VisibleContactPoint) AddDebugContact(contact);

            return (uint)(data.AddContact(contact) ? 1 : 0);
        }

        /// <summary>
        /// 두 개의 구가 충돌하는지 검사한 후 충돌데이터를 삽입한다.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static uint SphereAndSphere(CollisionSphere one, CollisionSphere two, ref CollisionData data)
        {
            // 접촉이 발생했는지 확인한다.
            if (data.ContactLeft <= 0) return 0;

            // 구의 위치를 가져온다.
            Vertex3f midline = one.Position - two.Position;
            float size = midline.Magnitude();

            // 충분히 큰지 확인한다.
            if (size<=0.0f || size >= one.Radius + two.Radius) return 0;

            // 충돌법선을 계산한다.
            Vertex3f normal = midline.Normalized;
            float penetration = (one.Radius + two.Radius) - size;

            //접촉데이터를 추가한다.
            Contact contact = new Contact()
            {
                ContactNormal = normal,
                ContactPoint = one.Position - midline * 0.5f,
                Penetration = penetration
            };
            contact.SetBodyData(one.RigidBody, two.RigidBody, data.Friction, data.Restitution);

            // 디버깅을 위한 접촉점 보이기
            if (VisibleContactPoint) AddDebugContact(contact);

            return (uint)(data.AddContact(contact) ? 1 : 0);
        }

        public static void AddDebugContact(Contact contact)
        {
            Debug.RenderPoints.Add(new RenderPont(contact.ContactPoint, Color4f.Red, 0.02f));
            Debug.RenderLines.Add(new RenderLine(contact.ContactPoint, contact.ContactPoint + contact.ContactNormal * 0.5f, Color4f.Red, 0.1f));
        }

    }
}
