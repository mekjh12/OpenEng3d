using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// -------------------------------------------------------------------
    ///                          행렬의 표현방법
    /// -------------------------------------------------------------------
    /// * 캐릭터공간에서의 행렬 접미사: AMat
    /// * 뼈공간(로컬공간)에서의 행렬 접미사: LMat
    /// * 월드공간에서의 행렬 접미사: WMat
    /// -------------------------------------------------------------------
    /// </summary>
    public static class Kinetics
    {
        //public static float Norm(this Vertex3f vec) => (float)Math.Sqrt(vec.Dot(vec));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="target"></param>
        /// <param name="endTarget"></param>
        public static void IKBoneRotate(Bone bone, Vertex3f target, Vertex3f endTarget)
        {
            //Vertex3f e = endTarget - bone.PivotPosition;
            //Vertex3f t = target - bone.PivotPosition;

            //bone.RotateBy(e.RotateBetween(t), endTarget);
            //bone.UpdateRootTransforms(isSelfIncluded: false);

            Vertex3f angleVector = EulerAngleFromRotationMatrix(bone.BoneTransforms.LocalTransform.Rot3x3f())[0];
            Matrix4x4f RotX = Matrix4x4f.RotatedX(angleVector.x);
            Matrix4x4f RotY = Matrix4x4f.RotatedY(angleVector.y);
            Matrix4x4f RotZ = Matrix4x4f.RotatedZ(angleVector.z);
            Vertex3f pos = bone.BoneTransforms.LocalTransform.Position;
            Matrix4x4f Rot = Matrix4x4f.Translated(pos.x, pos.y, pos.z) * RotZ * RotY * RotX;
            bone.BoneTransforms.LocalTransform = Rot;
            //bone.UpdateRootTransforms(isSelfIncluded: true);
        }

        public static void BoneRotate(Bone bone, float theta)
        {
            Matrix4x4f Mq = Matrix4x4f.RotatedY(theta);

            Vertex3f angleVector = EulerAngleFromRotationMatrix(Mq.Rot3x3f())[0];
            //Matrix4x4f RotX = Matrix4x4f.RotatedX(angleVector.x.Clamp(bone.RestrictAngle.XMin, bone.RestrictAngle.XMax));
            //Matrix4x4f RotY = Matrix4x4f.RotatedY(angleVector.y.Clamp(bone.RestrictAngle.YMin, bone.RestrictAngle.YMax));
            //Matrix4x4f RotZ = Matrix4x4f.RotatedZ(angleVector.z.Clamp(bone.RestrictAngle.ZMin, bone.RestrictAngle.ZMax));

            Vertex3f pos = bone.BoneTransforms.LocalTransform.Position;
            //Matrix4x4f Rot = Matrix4x4f.Translated(pos.x, pos.y, pos.z) * RotZ * RotY * RotX;
            //bone.LocalTransform = Rot;
            //bone.UpdateChildBone(isSelfIncluded: true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target">캐릭터공간에서의 타겟점을 설정해야 한다.</param>
        /// <param name="bone"></param>
        /// <param name="chainLength"></param>
        /// <param name="iternations"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static ColorPoint[] IKSolvedByFABRIK(Vertex3f target, Bone bone, int chainLength = 2, int iternations = 50, float epsilon = 0.001f)
        {
            // 말단뼈로부터 최상위 뼈까지 리스트를 만들고 가능한 Chain Length를 구함.
            List<Bone> bones = new List<Bone>();
            Bone parent = bone;
            bones.Add(parent);
            while (parent.Parent != null)
            {
                bones.Add(parent.Parent);
                parent = parent.Parent;
            }
            int rootChainLength = bones.Count;
            int N = Math.Min(chainLength, rootChainLength);

            // 뼈의 리스트 (말단의 뼈로부터 최상위 뼈로의 순서로)
            // 0번째가 말단뼈 --> ... --> N-1이 최상위 뼈
            Bone[] Bn = new Bone[N];

            // [초기값 설정] 캐릭터 공간 행렬과 뼈 공간 행렬을 만듦 
            for (int i = 0; i < N; i++) Bn[i] = bones[i];

            // 반복횟수와 오차범위안에서 반복하여 최적의 해를 찾는다.
            int iter = 0;
            float err = float.MaxValue;

            List<ColorPoint> points = new List<ColorPoint>();
            //Vertex3f RootPos = Bn[N - 1].PivotPosition;
            //points.Add(new ColorPoint(RootPos, 1, 1, 1, 0.02f));
            points.Add(new ColorPoint(target, 1, 1, 0, 0.01f));

            while (iter < iternations && err > epsilon)
            {
                // (1) Forward Reaching IK
                Vertex3f T = target;
                //Vertex3f E = Bn[0].TipPosition;

                for (int i = 0; i < N; i++)
                {
                    Bone cBone = Bn[i];
                    //Vertex3f P = cBone.PivotPosition;
                    //Matrix4x4f RotAMat = (E - P).RotateBetween(T - P);
                    //Vertex3f newE = cBone.RotateBy(RotAMat, E);                    
                    //Vertex3f pos = T - newE;
                    //Matrix4x4f TranAMat = Matrix4x4f.Translated(pos.x, pos.y, pos.z);
                    //cBone.AnimatedTransform = TranAMat * cBone.AnimatedTransform;
                    //E = P;
                    //T = cBone.PivotPosition;
                }

                // Update Local and Character Matrix.
                for (int i = 0; i < N; i++)
                {
                    //Bn[i].UpdateLocalTransformFromRootSpace();
                }
                //Bn[N - 1].UpdateRootTransforms(isSelfIncluded: true);

                // (2) Backward Reaching IK
                //T = RootPos;
                for (int i = N - 1; i >= 0; i--)
                {
                    Bone cBone = Bn[i];
                    //E = cBone.TipPosition;
                    //Vertex3f P = cBone.PivotPosition;
                    //Matrix4x4f RotAMat = (E - P).RotateBetween(E - T);
                    //Vertex3f newE = cBone.RotateBy(RotAMat, E);
                    //Vertex3f pos = T - P;
                    //Matrix4x4f TranAMat = Matrix4x4f.Translated(pos.x, pos.y, pos.z);
                    //cBone.AnimatedTransform = TranAMat * cBone.AnimatedTransform;
                    //T = newE + pos;
                }

                // Update Local and Character Matrix.
                for (int i = 0; i < N; i++)
                {
                    //Bn[i].UpdateLocalTransformFromRootSpace();
                }
                //Bn[N - 1].UpdateRootTransforms(isSelfIncluded: true);

                // (3) Constraint Bone Modify.
                for (int i = N - 1; i >= 0; i--)
                {
                    Bone cBone = Bn[i];
                    Vertex3f angleVector = EulerAngleFromRotationMatrixZYX(cBone.BoneTransforms.LocalTransform.Rot3x3f())[0];
                    angleVector.x = angleVector.x.Clamp(cBone.BoneKinematics.RestrictAngle.ConstraintAngle.x, cBone.BoneKinematics.RestrictAngle.ConstraintAngle.y);
                    angleVector.y = angleVector.y.Clamp(cBone.BoneKinematics.RestrictAngle.TwistAngle.x, cBone.BoneKinematics.RestrictAngle.TwistAngle.y);
                    angleVector.z = angleVector.z.Clamp(cBone.BoneKinematics.RestrictAngle.ConstraintAngle.z, cBone.BoneKinematics.RestrictAngle.ConstraintAngle.w);
                    Matrix4x4f RotX = Matrix4x4f.RotatedX(angleVector.x);
                    Matrix4x4f RotY = Matrix4x4f.RotatedY(angleVector.y);
                    Matrix4x4f RotZ = Matrix4x4f.RotatedZ(angleVector.z);
                    Vertex3f pos = cBone.BoneTransforms.LocalTransform.Position;
                    Matrix4x4f Rot = Matrix4x4f.Translated(pos.x, pos.y, pos.z) * RotZ * RotY * RotX;
                    cBone.BoneTransforms.LocalTransform = Rot;
                    //cBone.UpdateRootTransforms(isSelfIncluded: true);
                }

                //for (int i = 0; i < N; i++) Bn[i].UpdateLocalTransform();
                //Bn[N - 1].UpdateChildBone(isSelfIncluded: true);

                //points.Add(new ColorPoint(Bn[0].TipPosition, 0, 0.1f* iter, 0, 0.01f));

                //err = (Bn[0].TipPosition - T).Norm();
                iter++;
            }
            return points.ToArray();
        }

        /// <summary>
        /// 캐릭터 공간에서 IK를 푼다.
        /// </summary>
        /// <param name="target">캐릭터 공간의 위치벡터 (중요)캐릭터공간</param>
        /// <param name="bone"></param>
        /// <param name="chainLength"></param>
        /// <param name="iternations"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public static (float, Vertex3f[]) IKSolvedByCCD(Vertex3f target, Bone bone, int chainLength = 2, int iternations = 50, float epsilon = 0.05f)
        {
            // 말단뼈로부터 최상위 뼈까지 리스트를 만들고 가능한 Chain Length를 구함.
            List<Bone> bones = new List<Bone>();
            Bone parent = bone;
            bones.Add(parent);
            while (parent.Parent != null)
            {
                bones.Add(parent.Parent);
                parent = parent.Parent;
            }
            int rootChainLength = bones.Count;
            int N = Math.Min(chainLength, rootChainLength);

            // 뼈의 리스트 (말단의 뼈로부터 최상위 뼈로의 순서로)
            // 0번째가 말단뼈 --> ... --> N-1이 최상위 뼈
            Bone[] Bn = new Bone[N];

            // [초기값 설정] 캐릭터 공간 행렬과 뼈 공간 행렬을 만듦 
            for (int i = 0; i < N; i++) Bn[i] = bones[i];

            // 반복횟수와 오차범위안에서 반복하여 최적의 해를 찾는다.
            int iter = 0;
            float err = float.MaxValue;

            while (iter < iternations && err > epsilon)
            {
                // 최말단뼈부터 시작하여 최상위 뼈까지 회전을 적용한다.
                for (int i = 0; i < N; i++)
                {
                    //IKBoneRotate(Bn[i], target, Bn[0].TipPosition);
                }

                // 최종적으로 최말단뼈의 회전을 적용한다.
                //IKBoneRotate(Bn[0], target, Bn[0].TipPosition);
                //err = (Bn[0].TipPosition - target).Norm();

                iter++;
            }

            List<Vertex3f> points = new List<Vertex3f>();
            //points.Add(bone.BoneTransforms.RootTransform.Position);
            //points.Add(Bn[0].TipPosition);

            return (err, points.ToArray());
        }

        public static void LookAt(Bone bone, Vertex3f target)
        {
            //Vertex3f bz = bone.BoneTransforms.RootTransform.Position;
            // 추후 구현할 부분
            //ModifyChildBoneAnimatedTransform
        }

        public static Vertex3f[] FindTangentPoint3D(Vertex3f target, Matrix4x4f parentCharMat, Matrix4x4f curCharMat, Vertex4f theta)
        {
            List<Vertex3f> list = new List<Vertex3f>();
            Matrix4x4f charMat = parentCharMat;
            charMat[3, 0] = curCharMat.Column3.x;
            charMat[3, 1] = curCharMat.Column3.y;
            charMat[3, 2] = curCharMat.Column3.z;

            Matrix4x4f invCharMat = (charMat * 100.0f).Inverse * 100.0f;
            Vertex3f t = (invCharMat * target.Vertex4f()).Vertex3f();
            float length = t.Norm();
            float s = t.y;
            //s = (s < 0.001f) ? 0.001f : s;
            Vertex4f t2d = FindTangentPoint2D(t.z, t.x, s, theta);
            Vertex3f newTarget = new Vertex3f(t2d.y, Math.Abs(t.y), t2d.x).Normalized * length;
            Vertex3f nt = (charMat * newTarget.Vertex4f()).Vertex3f();
            list.Add(nt);
            return list.ToArray();
        }

        public static Vertex4f FindTangentPoint2D(float i, float j, float S, Vertex4f theta)
        {
            float RADIAN = 3.141502f / 180.0f;
            float phi1 = 0.0f;
            float phi2 = 0.0f;
            float epsilon = 0.1f;
            int iter = 0;

            float a = 0.0f;
            float b = 0.0f;

            if (i > 0 && j >= 0)
            {
                phi1 = 0.0f; phi2 = 90.0f;
                a = (float)(S * Math.Tan(theta.x * RADIAN));
                b = (float)(S * Math.Tan(theta.y * RADIAN));

                if (i * i / (a * a) + j * j / (b * b) < 1)
                {
                    return new Vertex4f(i, j, a, b);
                }

            }
            if (i <= 0 && j > 0)
            {
                phi1 = 90.0f; phi2 = 180.0f;
                a = (float)(S * Math.Tan(theta.y * RADIAN));
                b = (float)(S * Math.Tan(theta.z * RADIAN));
            }
            if (i < 0 && j <= 0)
            {
                phi1 = 180.0f; phi2 = 270.0f;
                a = (float)(S * Math.Tan(theta.z * RADIAN));
                b = (float)(S * Math.Tan(theta.w * RADIAN));
            }
            if (i >= 0 && j < 0)
            {
                phi1 = 270.0f; phi2 = 360.0f;
                a = (float)(S * Math.Tan(theta.w * RADIAN));
                b = (float)(S * Math.Tan(theta.x * RADIAN));
            }

            

            while (Math.Abs(phi1 - phi2) > epsilon && iter < 20)
            {
                float theta0 = (phi1 + phi2) * 0.5f;
                float d1 = Dot(phi1);
                float d2 = Dot(phi2);
                float d0 = Dot(theta0);
                if (d1 * d0 < 0)
                {
                    phi2 = theta0;
                }
                else if (d2 * d0 < 0)
                {
                    phi1 = theta0;
                }
                else if (d1 * d2 == 0)
                {
                    break;
                }
                iter++;
            }

            return new Vertex4f((float)(a * Math.Cos(phi1 * RADIAN)), (float)(b * Math.Sin(phi1 * RADIAN)), a, b);

            float Dot(float t)
            {
                float cos = (float)Math.Cos(t * RADIAN);
                float sin = (float)Math.Sin(t * RADIAN);
                float dot = (j - b * sin) * b * cos - a * sin * (i - a * cos);
                float xn = (float)Math.Sqrt((i - a * cos) * (i - a * cos) + (j - b * sin) * (j - b * sin));
                float yn = (float)Math.Sqrt(a * a * sin * sin + b * b * cos * cos);
                dot = dot / (xn * yn);
                return dot;
            }
        }


        /// <summary>
        /// 회전행렬로부터 기본회전행렬로 분해한 오일러회전각을 가져온다.<br/>
        /// R = Rz*Ry*Rx이므로 X->Y->Z순으로 분해한다.<br/>
        /// * Pitch = theta_x는 -180부터 180사이의 각이다. <br/>
        /// * Roll = theta_y는 -90부터 90사이의 각이다. <br/>
        /// * Yaw = theta_z는 -180부터 180사이의 각이다. <br/>
        /// * 참고사이트 https://nghiaho.com/?page_id=846<br/>
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Vertex3f[] EulerAngleFromRotationMatrixZYX(Matrix3x3f mat)
        {
            float R11 = mat[0, 0];
            float R12 = mat[1, 0];
            float R13 = mat[2, 0];
            float R21 = mat[0, 1];
            float R22 = mat[1, 1];
            float R23 = mat[2, 1];
            float R31 = mat[0, 2];
            float R32 = mat[1, 2];
            float R33 = mat[2, 2];

            float thetaX = 0.0f;
            float thetaY = 0.0f;
            float thetaZ = 0.0f;

            thetaX = ((float)Math.Atan2(R32, R33)).ToDegree();
            thetaY = ((float)Math.Atan2(-R31, Math.Sqrt(R32 * R32 + R33 * R33))).ToDegree();
            thetaZ = ((float)Math.Atan2(R21, R11)).ToDegree();

            return new Vertex3f[] { new Vertex3f(thetaX, thetaY, thetaZ) };
        }

        /// <summary>
        /// Gregory G.Slabaugh, "Computing Euler Angles from a Rotation Matrix."
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Vertex3f[] EulerAngleFromRotationMatrix(Matrix3x3f mat)
        {
            float R11 = mat[0, 0];
            float R12 = mat[1, 0];
            float R13 = mat[2, 0];
            float R21 = mat[0, 1];
            float R22 = mat[1, 1];
            float R23 = mat[2, 1];
            float R31 = mat[0, 2];
            float R32 = mat[1, 2];
            float R33 = mat[2, 2];

            float theta1 = 0.0f;
            float theta2 = 0.0f;
            float psi1 = 0.0f;
            float psi2 = 0.0f;
            float phi1 = 0.0f;
            float phi2 = 0.0f;

            if (R31 != 1 ||  R31 != -1)
            {
                theta1 = ((float)-Math.Asin(R31)).ToDegree();
                theta2 = 180.0f - theta1;
                float cos1 = (float)Math.Cos(theta1.ToRadian());
                float cos2 = (float)Math.Cos(theta2.ToRadian());
                psi1 = ((float)Math.Atan2(R32 / cos1, R33 / cos1)).ToDegree();
                psi2 = ((float)Math.Atan2(R32 / cos2, R33 / cos2)).ToDegree();
                phi1 = ((float)Math.Atan2(R21 / cos1, R11 / cos1)).ToDegree();
                phi2 = ((float)Math.Atan2(R21 / cos1, R11 / cos1)).ToDegree();
            }
            else
            {
                phi1 = phi2 = 0.0f;
                if (R31 == -1)
                {
                    theta1 = theta2 = 90.0f;
                    psi1 = psi2 = phi1 + ((float)Math.Atan2(R12, R13)).ToDegree();
                }
                else
                {
                    theta1 = theta2 = -90.0f;
                    psi1 = psi2 = -phi1 + ((float)Math.Atan2(-R12, -R13)).ToDegree();
                }
            }

            return new Vertex3f[] { new Vertex3f(psi1, theta1, phi1), new Vertex3f(psi2, theta2, phi2) };
        }

        public static float Angle(float a, float b)
        {
            float theta = ((float)Math.Asin(Math.Abs(b) / Math.Sqrt(a * a + b * b))).ToDegree();
            if (a < 0 && b > 0)
            {
                theta = 180 - theta;
            }
            else if (a < 0 && b < 0)
            {
                theta = 180 + theta;
            }
            else if (a > 0 && b < 0)
            {
                theta = 360 - theta;
            }

            return theta;
        }
    }
}
