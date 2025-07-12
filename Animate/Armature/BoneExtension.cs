using OpenGL;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using ZetaExt;

namespace Animate
{
    public static class BoneExtension
    {

        public static string ToString(this Bone bone)
        {
            string txt = "";
            Matrix4x4f m = bone.BoneTransforms.LocalTransform;
            for (uint i = 0; i < 4; i++)
            {
                txt += $"{Cut(m[0, i])} {Cut(m[1, i])} {Cut(m[2, i])} {Cut(m[3, i])}"
                    + ((i < 3) ? " / " : "");
            }

            string invBind = "";
            Matrix4x4f n = bone.BoneTransforms.InverseBindPoseTransform;
            for (uint i = 0; i < 4; i++)
            {
                invBind += $"{Cut(n[0, i])} {Cut(n[1, i])} {Cut(n[2, i])} {Cut(n[3, i])}"
                + ((i < 3) ? " / " : "");
            }
            return $"[{bone.Index},{bone.Name}] Parent={bone.Parent?.Name}, BindMatrix {txt} InvBindMatrix {invBind}";

            float Cut(float a) => ((float)Math.Abs(a) < 0.000001f) ? 0.0f : a;
        }


        /// <summary>
        /// 현재 뼈에 캐릭터 공간 회전행렬을 적용하여 부모뻐로부터의 뒤틀림(twist) 회전각을 가져온다.
        /// </summary>
        /// <param name="rotAMat"></param>
        /// <returns></returns>
        public static float TwistAngle(this Bone bone, Matrix4x4f rotAMat)
        {
            Matrix4x4f BrAMat = bone.BoneTransforms.AnimatedTransform;
            BrAMat = bone.Parent.BoneTransforms.AnimatedTransform.Inverse * rotAMat * BrAMat;
            Vertex3f nX = BrAMat.Column0.Vertex3f().Normalized;
            float theta = (float)Math.Acos(Vertex3f.UnitX.Dot(nX)) * 180.0f / 3.141502f;
            theta = nX.z < 0 ? theta : -theta;
            return theta;
        }


        public static void ModifyPitch(this Bone bone, float phi)
        {
            if (phi < bone.RestrictAngle.ConstraintAngle.x)
            {
                Matrix4x4f localMat = bone.BoneTransforms.LocalTransform;
                Vertex3f lowerYAxis = localMat.Column1.Vertex3f();
                lowerYAxis.y = 0;
                lowerYAxis = lowerYAxis.Normalized * -Math.Sin(bone.RestrictAngle.ConstraintAngle.x.ToRadian());
                lowerYAxis.y = (float)Math.Cos(bone.RestrictAngle.ConstraintAngle.x.ToRadian());
                Matrix4x4f localRotMat = localMat.Column1.Vertex3f().RotateBetween(lowerYAxis);
                bone.BoneTransforms.LocalTransform = localMat * localRotMat;
                bone.UpdatePropTransform(isSelfIncluded: true);
            }
            else if (phi > bone.RestrictAngle.ConstraintAngle.y)
            {
                Matrix4x4f localMat = bone.BoneTransforms.LocalTransform;
                Vertex3f lowerYAxis = localMat.Column1.Vertex3f();
                lowerYAxis.y = 0;
                lowerYAxis = lowerYAxis.Normalized * Math.Sin(bone.RestrictAngle.ConstraintAngle.y.ToRadian());
                lowerYAxis.y = (float)Math.Cos(bone.RestrictAngle.ConstraintAngle.y.ToRadian());
                Matrix4x4f localRotMat = localMat.Column1.Vertex3f().RotateBetween(lowerYAxis);
                bone.BoneTransforms.LocalTransform = localMat * localRotMat;
                bone.UpdatePropTransform(isSelfIncluded: true);
            }
        }

        public static void ModifyYaw(this Bone bone, float phi)
        {
            if (phi < bone.RestrictAngle.ConstraintAngle.z)
            {
                Matrix4x4f localMat = bone.BoneTransforms.LocalTransform;
                Vertex3f lowerZAxis = localMat.Column2.Vertex3f();
                lowerZAxis.z = 0;
                lowerZAxis = lowerZAxis.Normalized * -Math.Sin(bone.RestrictAngle.ConstraintAngle.z.ToRadian());
                lowerZAxis.y = (float)Math.Cos(bone.RestrictAngle.ConstraintAngle.z.ToRadian());
                Matrix4x4f localRotMat = localMat.Column2.Vertex3f().RotateBetween(lowerZAxis);
                bone.BoneTransforms.LocalTransform = localMat * localRotMat;
                bone.UpdatePropTransform(isSelfIncluded: true);
            }
            else if (phi > bone.RestrictAngle.ConstraintAngle.w)
            {
                Matrix4x4f localMat = bone.BoneTransforms.LocalTransform;
                Vertex3f lowerZAxis = localMat.Column2.Vertex3f();
                lowerZAxis.z = 0;
                lowerZAxis = lowerZAxis.Normalized * Math.Sin(bone.RestrictAngle.ConstraintAngle.w.ToRadian());
                lowerZAxis.z = (float)Math.Cos(bone.RestrictAngle.ConstraintAngle.w.ToRadian());
                Matrix4x4f localRotMat = localMat.Column2.Vertex3f().RotateBetween(lowerZAxis);
                bone.BoneTransforms.LocalTransform = localMat * localRotMat;
                bone.UpdatePropTransform(isSelfIncluded: true);
            }
        }

        public static void ModifyRoll(this Bone bone, float phi)
        {
            if (phi < bone.RestrictAngle.TwistAngle.x)
            {
                Matrix4x4f localMat = bone.BoneTransforms.LocalTransform;
                Vertex3f lowerZAxis = localMat.Column2.Vertex3f();
                lowerZAxis.z = 0;
                lowerZAxis = lowerZAxis.Normalized * -Math.Sin(bone.RestrictAngle.ConstraintAngle.z.ToRadian());
                lowerZAxis.y = (float)Math.Cos(bone.RestrictAngle.ConstraintAngle.z.ToRadian());
                Matrix4x4f localRotMat = localMat.Column2.Vertex3f().RotateBetween(lowerZAxis);
                bone.BoneTransforms.LocalTransform = localMat * localRotMat;
                bone.UpdatePropTransform(isSelfIncluded: true);
            }
            else if (phi > bone.RestrictAngle.TwistAngle.y)
            {
                Matrix4x4f localMat = bone.BoneTransforms.LocalTransform;
                Vertex3f lowerZAxis = localMat.Column2.Vertex3f();
                lowerZAxis.z = 0;
                lowerZAxis = lowerZAxis.Normalized * Math.Sin(bone.RestrictAngle.ConstraintAngle.w.ToRadian());
                lowerZAxis.z = (float)Math.Cos(bone.RestrictAngle.ConstraintAngle.w.ToRadian());
                Matrix4x4f localRotMat = localMat.Column2.Vertex3f().RotateBetween(lowerZAxis);
                bone.BoneTransforms.LocalTransform = localMat * localRotMat;
                bone.UpdatePropTransform(isSelfIncluded: true);
            }
        }


        /// <summary>
        /// 피봇의 위치로부터 보는 곳의 위치로 좌표프레임을 변환한다.
        /// </summary>
        /// <param name="pivotPosition">피봇의 월드 위치</param>
        /// <param name="lookAt">보는 곳의 월드 위치</param>
        /// <param name="upVector">Vertex3f.UnitZ를 기본 사용 권장</param>
        public static void ApplyCoordinateFrame(this Bone bone, Vertex3f pivotPosition, Vertex3f lookAt, Vertex3f upVector, bool isRestrictAngle = true)
        {
            Vertex3f eyePos = bone.BoneTransforms.AnimatedTransform.Position + pivotPosition; // 캐릭터의 월드 공간 위치에 뼈의 위치를 더한다.

            Vertex3f z = (lookAt - eyePos).Normalized;
            Vertex3f x = upVector.Cross(z).Normalized;
            Vertex3f y = z.Cross(x).Normalized;

            Matrix4x4f loc = bone.BoneTransforms.LocalBindTransform;

            Matrix3x3f frame = Matrix3x3f.Identity.Frame(x, y, z) * bone.BoneTransforms.LocalBindTransform.Rot3x3f();
            Vertex3f pos = bone.BoneTransforms.AnimatedTransform.Position;
            bone.BoneTransforms.AnimatedTransform = frame.ToMat4x4f(pos);
            bone.UpdateLocalTransform();

            // 제한된 각도를 벗어나면 제한된 각도로 회귀한다.
            if (isRestrictAngle)
            {
                Vertex3f angleVector = Kinetics.EulerAngleFromRotationMatrixZYX(bone.BoneTransforms.LocalBindTransform.Rot3x3f())[0]; // 오일러 각을 가져온다.
                angleVector.x = angleVector.x.Clamp(bone.RestrictAngle.ConstraintAngle.x, bone.RestrictAngle.ConstraintAngle.y);
                angleVector.y = angleVector.y.Clamp(bone.RestrictAngle.TwistAngle.x, bone.RestrictAngle.TwistAngle.y);
                angleVector.z = angleVector.z.Clamp(bone.RestrictAngle.ConstraintAngle.z, bone.RestrictAngle.ConstraintAngle.w);
                Matrix4x4f RotX = Matrix4x4f.RotatedX(angleVector.x);
                Matrix4x4f RotY = Matrix4x4f.RotatedY(angleVector.y);
                Matrix4x4f RotZ = Matrix4x4f.RotatedZ(angleVector.z); // 회전을 계산한다.
                pos = bone.BoneTransforms.LocalTransform.Position;
                Matrix4x4f Rot = Matrix4x4f.Translated(pos.x, pos.y, pos.z) * RotZ * RotY * RotX; // 회전과 이동을 계산한다.
                float sx = bone.BoneTransforms.LocalTransform.Column0.Vertex3f().Norm();
                float sy = bone.BoneTransforms.LocalTransform.Column1.Vertex3f().Norm();
                float sz = bone.BoneTransforms.LocalTransform.Column2.Vertex3f().Norm();
                bone.BoneTransforms.LocalTransform = Rot * Matrix4x4f.Scaled(sx, sy, sz) * 100f; // 원본의 행렬의 열벡터의 크기를 가져온다.
                bone.UpdatePropTransform(isSelfIncluded: true);
            }
        }
    }
}
