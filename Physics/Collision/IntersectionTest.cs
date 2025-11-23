using FastMath;
using OpenGL;
using System;
using ZetaExt;

namespace Physics.Collision
{
    /// <summary>
    /// 충돌 원형의 교차 여부를 테스트하는 정적 클래스
    /// </summary>
    public static class IntersectionTest
    {
        /// <summary>
        /// 박스와 박스 교차 테스트에서의 점-면 충돌의 충돌점을 가져온다.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="toCentre"></param>
        /// <param name="data"></param>
        /// <param name="best"></param>
        /// <param name="penetration"></param>
        public static void FillPointFaceBoxBox(CollisionBox one, CollisionBox two, Vertex3f toCentre, ref CollisionData data, int best, float penetration)
        {
            // 첫번째 박스의 최적축이 두번째 박스의 중심에서 첫번째 박스의 중심으로 향하게 하기 위하여
            // toCentre와 둔각을 이루도록 설정한다.
            Vertex3f normal = one.Axis(best % 3);
            if (normal * toCentre > 0) normal *= -1.0f;

            // 두번째 상자의 입장에서 normal 방향의 꼭짓점을 얻기 위하여 좌표가 반대로 있으면 반대로 설정해 준다.
            // normal 벡터는 두번째 상자에서 첫번째 상자를 향하는 벡터이므로,
            // 월드 공간의 normal 벡터가 두 번째 상자의 x,Y,Z축과 같은 방향으로 향하도록 한다.
            // 예를 들어, x축과 normal벡터가 내적이 음수이면 반대방향에 있으므로 vertex의 방향을 반대로 한다.
            // 이후에 vertex를 월드 변환한다.
            Vertex3f vertex = two.HalfSize;
            if (two.Axisx * normal < 0) vertex.x *= -1.0f;
            if (two.AxisY * normal < 0) vertex.y *= -1.0f;
            if (two.AxisZ * normal < 0) vertex.z *= -1.0f;
            Vertex3f contactPoint = two.RigidBody.TransformMatrix.Transform(vertex);

            //접촉 데이터를 추가한다. 평면 방향으로의 법선을 가진다.
            Contact contact = new Contact()
            {
                ContactNormal = normal,
                ContactPoint = contactPoint,
                Penetration = penetration,
            };
            contact.SetBodyData(one.RigidBody, two.RigidBody, data.Friction, data.Restitution);
            data.AddContact(contact);

            // 디버깅을 위한 접촉점 보이기
            if (CollisionDetector.VisibleContactPoint) CollisionDetector.AddDebugContact(contact);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pOne"></param>
        /// <param name="dOne"></param>
        /// <param name="oneSize"></param>
        /// <param name="pTwo"></param>
        /// <param name="dTwo"></param>
        /// <param name="twoSize"></param>
        /// <param name="useOne"></param>
        /// <returns></returns>
        public static Vertex3f ContactPoint(Vertex3f pOne, Vertex3f dOne, float oneSize, Vertex3f pTwo, Vertex3f dTwo, float twoSize, bool useOne)
        {
            float smOne = dOne.SquareMagnitude();
            float smTwo = dTwo.SquareMagnitude();
            float dpOneTwo = dTwo * dOne;

            Vertex3f toSt = pOne - pTwo;
            float dpStaOne = dOne * toSt;
            float dpStaTwo = dTwo * toSt;

            float denom = smOne * smTwo - dpOneTwo * dpOneTwo;

            if (denom.Abs() < 0.0001f)
            {
                return useOne ? pOne : pTwo;
            }

            float mua = (dpOneTwo * dpStaTwo - smTwo * dpStaOne) / denom;
            float mub = (smOne * dpStaTwo - dpOneTwo * dpStaOne) / denom;

            if (mua > oneSize || mua < -oneSize || mub > twoSize || mub < -twoSize)
            {
                return useOne ? pOne : pTwo;
            }
            else
            {
                Vertex3f cOne = pOne + dOne * mua;
                Vertex3f cTwo = pTwo + dTwo * mub;
                return cOne * 0.5f + cTwo * 0.5f;
            }
        }

        /// <summary>
        /// 한 축으로 두 강체가 겹치는지 검사한다.
        /// </summary>
        /// <param name="one">강체1</param>
        /// <param name="two">강체2</param>
        /// <param name="axis">축 벡터</param>
        /// <param name="index">사용할 고유인덱스</param>
        /// <param name="smallestCase"></param>
        /// <param name="smallestPenetration"></param>
        /// <returns></returns>
        public static bool TryAxis(CollisionBox one, CollisionBox two, Vertex3f axis, int index, ref int smallestCase, ref float smallestPenetration)
        {
            if (axis.SquareMagnitude() < 0.0001f) return true;
            Vertex3f unitAxis = axis.Normalized;

            float penetration = PenetrationOnAxis(one, two, unitAxis);
            if (penetration < 0 ) return false;

            if (penetration < smallestPenetration)
            {
                smallestPenetration = penetration;
                smallestCase = index;
            }
            return true;
        }

        /// <summary>
        /// 두 상자가 축에 사영 되어진 길이의 합을 반환한다. <br/>
        /// 두 상자가 겹치면 겹치는 부분의 길이를 고려한다.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private static float PenetrationOnAxis(CollisionBox one, CollisionBox two, Vertex3f axis)
        {
            float oneProjLength = one.TransformToAxis(axis);
            float twoProjLength = two.TransformToAxis(axis);

            Vertex3f unitAxis = axis.Normalized;
            float distance = ((one.Position - two.Position) * unitAxis).Abs();

            return oneProjLength + twoProjLength - distance;
        }

        /// <summary>
        /// 축에 다른 상자와 겹치는지 여부를 반환한다.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="axis">축은 단위벡터</param>
        /// <returns></returns>
        private static bool OverlapOnAxis(CollisionBox one, CollisionBox two, Vertex3f axis)
        {
            float oneProjLength = one.BoxRadius(axis);
            float twoProjLength = two.BoxRadius(axis);

            Vertex3f unitAxis = axis.Normalized;
            float distance = ((one.Position - two.Position) * unitAxis).Abs();

            return distance < oneProjLength + twoProjLength;
        }

        /// <summary>
        /// 박스와 반평면과의 교차여부를 반환한다.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static bool BoxAndHalfSpace(CollisionBox box, CollisionPlane plane)
        {
            float projectedRadius = TransformToAxis(box, plane.Normal);
            float boxDistance = plane.SignedDistance(box.Position) - projectedRadius;
            return boxDistance <= 0.0f;
        }

        /// <summary>
        /// 박스의 축 방향으로의 정사형의 양의 반의 길이
        /// </summary>
        /// <param name="box"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private static float TransformToAxis(CollisionBox box, Vertex3f axis)
        {
            return box.HalfSize.x * MathFast.Abs(axis * box.Axisx) +
                box.HalfSize.y * MathFast.Abs(axis * box.AxisY) +
                box.HalfSize.z * MathFast.Abs(axis * box.AxisZ);
        }

        /// <summary>
        /// 박스와 박스가 교차하는지 여부를 검사한다. 단, 충돌점은 검출 할 수 없다.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool BoxAndBox(CollisionBox one, CollisionBox two)
        {
            return
                OverlapOnAxis(one, two, one.Axisx) &&
                OverlapOnAxis(one, two, one.AxisY) &&
                OverlapOnAxis(one, two, one.AxisZ) &&

                OverlapOnAxis(one, two, two.Axisx) &&
                OverlapOnAxis(one, two, two.AxisY) &&
                OverlapOnAxis(one, two, two.AxisZ) &&

                OverlapOnAxis(one, two, one.Axisx.Cross(two.Axisx)) &&
                OverlapOnAxis(one, two, one.Axisx.Cross(two.AxisY)) &&
                OverlapOnAxis(one, two, one.Axisx.Cross(two.AxisZ)) &&
                OverlapOnAxis(one, two, one.AxisY.Cross(two.Axisx)) &&
                OverlapOnAxis(one, two, one.AxisY.Cross(two.AxisY)) &&
                OverlapOnAxis(one, two, one.AxisY.Cross(two.AxisZ)) &&
                OverlapOnAxis(one, two, one.AxisZ.Cross(two.Axisx)) &&
                OverlapOnAxis(one, two, one.AxisZ.Cross(two.AxisY)) &&
                OverlapOnAxis(one, two, one.AxisZ.Cross(two.AxisZ));
        }

    }
}
