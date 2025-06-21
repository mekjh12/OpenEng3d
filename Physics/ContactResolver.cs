using OpenGL;
using Physics.Collision;
using System;
using System.CodeDom;
using System.Collections.Generic;
using ZetaExt;
using System.Drawing;

namespace Physics
{
    public class ContactResolver
    {
        float _velocityEpsilon = 0.1f;
        float _positionEpsilon = 0.1f;
        int _velocityIternations = 1;
        int _positionIternations = 1;
        
        public ContactResolver(float positionEpsilon = 0.1f, float velocityEpsilon = 0.1f, int velocityIternations = 10, int positionIternations = 10)
        {
            _positionEpsilon = positionEpsilon;
            _velocityEpsilon = velocityEpsilon;
            _positionIternations = positionIternations;
            _velocityIternations = velocityIternations;
        }

        public void ResolveContacts(List<Contact> data, float duration)
        {
            // 접촉데이터가 없으면 반환한다.
            if (data.Count == 0) return;

            // 접촉 처리를 위해 데이터를 준비한다.
            PrepareContacts(data, duration);

            // 접촉에 따른 관통된 깊이을 처리한다.
            //AdjustPositions(data, duration);
            for (int i = 0; i < data.Count; i++)
            {
                Contact contact = data[i];
                float max = 0.0f;
                Vertex3f[] linearChange = new Vertex3f[2];
                Vertex3f[] angularChange = new Vertex3f[2];
                contact.ApplyPositionChange(duration, ref linearChange, ref angularChange, ref max);
            }

            // 접촉에 따른 속도를 처리한다.
            //AdjustVelocities(data, duration);
            for (int i = 0; i < data.Count; i++)
            {
                Contact contact = data[i];
                float max = 0.0f;
                Vertex3f[] velocityChange = new Vertex3f[2];
                Vertex3f[] rotationChange = new Vertex3f[2];
                contact.ApplyVelocityChange(ref velocityChange, ref rotationChange);
            }
        }


        /// <summary>
        /// 1. 접촉점의 로컬좌표계<br/>
        /// 2. 강체의 접근속도의 합<br/>
        /// 3. 접촉점의 두 강체의 상대좌표<br/>
        /// 4. 반발계수에 따른 강체의 분리를 위한 속도의 합<br/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="duration"></param>
        private void PrepareContacts(List<Contact> data, float duration)
        {
            foreach (Contact contact in data)
            {
                if (contact == null) continue;

                if (contact.ContactNormal == Vertex3f.Zero)
                    throw new System.Exception("");

                // 접촉데이터의 내부자료를 처리한다.
                contact.CalculateInternals(duration);
            }
        }

        private void AdjustPositions(List<Contact> data, float duration)
        {
            float epsilon = 0.001f;
            const int POSITION_ITERNATIONS = 1;

            int positionIterationUsed = 0;
            while (positionIterationUsed < POSITION_ITERNATIONS)
            {
                float max = 0.0f;
                Vertex3f deltaPosition = Vertex3f.Zero;
                Vertex3f[] linearChange = new Vertex3f[2];
                Vertex3f[] angularChange = new Vertex3f[2];

                // 최대관통깊이를 갖는 접촉을 찾는다.
                int index = data.Count;
                for (int i = 0; i < data.Count; i++)
                {
                    Contact c = data[i];
                    if (c == null) continue;
                    if (c.Penetration > max)
                    {
                        max = c.Penetration;
                        index = i;
                    }
                }

                if (index >= data.Count) continue;
                Contact worstContact = data[index];

                // 최대관통깊이가 작으면 루프를 끝낸다.
                if (max < epsilon) break;

                // 최대관통 접촉에 대하여 두 강체의 위치를 조정한다.
                max = worstContact.Penetration;
                worstContact.ApplyPositionChange(duration, ref linearChange, ref angularChange, ref max);

                // 위치 조정에 따른 두 강체를 가지고 있는 접촉에 대하여 강체사이의 관통깊이를 조정해준다.
                for (int i = 0; i < data.Count; i++)
                {
                    Contact c = data[i];
                    for (int b = 0; b < 2; b++)
                    {
                        if (c.RigidBodies[b] == null) continue;

                        for (int d = 0; d < 2; d++)
                        {
                            if (c.RigidBodies[b] == worstContact.RigidBodies[d])
                            {
                                deltaPosition = linearChange[d] + angularChange[d].Cross(c.RelativeContactPosition[b]);
                                c.Penetration += deltaPosition * c.ContactNormal * (b == 0 ? 1 : -1);
                            }
                        }
                    }
                }
                positionIterationUsed++;
            }
        }

        private void AdjustVelocities(List<Contact> data, float duration)
        {
            float epsilon = 0.001f;
            const int VELOCITY_ITERNATIONS = 1;

            int veolocityIterationUsed = 0;
            while (veolocityIterationUsed < VELOCITY_ITERNATIONS)
            {
                float max = 0.0f;
                Vertex3f deltaVelocity = Vertex3f.Zero;
                Vertex3f[] velocityChange = new Vertex3f[2];
                Vertex3f[] rotationChange = new Vertex3f[2];

                // 최대 속력의 접촉점을 찾는다.
                int index = data.Count;
                for (int i = 0; i < data.Count; i++)
                {
                    Contact c = data[i];
                    if (c == null) continue;
                    if (c.DesiredDeltaVelocity.Abs() > max)
                    {
                        max = c.DesiredDeltaVelocity.Abs();
                        index = i;
                    }
                }

                if (index >= data.Count) break;
                Contact bestContact = data[index];

                // 최대 속력이 매우 작으면 루프를 끝낸다.
                if (max < epsilon) break;

                // 최대 속력을 갖는 접촉점에서의 두 강체의 속도를 조정한다.
                bestContact.ApplyVelocityChange(ref velocityChange, ref rotationChange);

                // 속도 조정에 따른 두 강체를 가지고 있는 접촉에 대하여 강체사이의 관통깊이를 조정해준다.
                for (int i = 0; i < data.Count; i++)
                {
                    Contact c = data[i];
                    for (int b = 0; b < 2; b++)
                    {
                        if (c.RigidBodies[b] == null) continue;

                        for (int d = 0; d < 2; d++)
                        {
                            if (c.RigidBodies[b] == bestContact.RigidBodies[d])
                            {
                                deltaVelocity = velocityChange[d] + rotationChange[d].Cross(c.RelativeContactPosition[b]);
                                c.ContactClosedVelocity +=  (c.ContactWorldTransform.Transposed * deltaVelocity) * (b == 0 ? 1 : -1);
                                c.CalculateDesiredDeltaVelocity(duration);
                            }
                        }
                    }
                }
                veolocityIterationUsed++;

                //Vertex3f contactClosingVelocity = contact.ContactWorldTransform * (contact.ContactClosedVelocity * 10.0f);
                //Debug.RenderLines.Add(new RenderLine(contact.ContactPoint, contact.ContactPoint + contactClosingVelocity, new Vertex4f(1, 0, 1, 1), 12.0f));
            }
        }
    }
}
