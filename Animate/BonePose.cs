using System;
using Assimp;
using OpenGL;
using ZetaExt;

namespace Animate
{
    public class BonePose
    {
        Vertex3f _scaling;
        Vertex3f _position;
        ZetaExt.Quaternion _rotation;

        public Vertex3f Scaling
        {
            get=>_scaling; 
            set=> _scaling = value;
        }

        public Vertex3f Position
        {
            get => _position;
            set => _position = value;
        }

        public ZetaExt.Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        public BonePose(Vertex3f position)
        {
            _position = position;
            _rotation = new ZetaExt.Quaternion(Vertex3f.UnitX, 0);
            _scaling = Vertex3f.One;
        }

        public BonePose(Vertex3f position, ZetaExt.Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
            _scaling = Vertex3f.One;
        }

        public BonePose(Vector3D position, ZetaExt.Quaternion rotation, Vector3D scaling)
        {
            _position = new Vertex3f((float)position.X, (float)position.Y, (float)position.Z);
            _rotation = rotation;
            _scaling = new Vertex3f((float)scaling.X, (float)scaling.Y, (float)scaling.Z);
        }

        public override string ToString()
        {
            string txt = "";
            txt += " scale=" + _scaling.ToString();
            txt += " pos=" + _position.ToString();
            txt += " rot=" + _rotation.ToString();
            return txt;
        }

        public BonePose()
        {
            //throw new System.NotImplementedException();
        }

        public Matrix4x4f LocalTransform
        {
            get
            {
                Matrix4x4f T = Matrix4x4f.Translated(_position.x, _position.y, _position.z);
                Matrix4x4f R = (Matrix4x4f)_rotation;
                Matrix4x4f S = Matrix4x4f.Scaled(_scaling.x, _scaling.y, _scaling.z);
                return T * R * S;
            }
        }

        public static Matrix4x4f InterpolateSlerp(Matrix4x4f frameA, Matrix4x4f frameB, float progression)
        {
            Vertex3f pos = InterpolateLerp(frameA.Position, frameB.Position, progression);

            ZetaExt.Quaternion qa = frameA.ToQuaternion();
            ZetaExt.Quaternion qb = frameB.ToQuaternion();

            ZetaExt.Quaternion rot = qa.Interpolate(qb, progression);

            Matrix4x4f res = (Matrix4x4f)rot;

            Vertex3f c0 = res.Column0.Vertex3f() * frameA.Column0.Vertex3f().Norm();
            Vertex3f c1 = res.Column1.Vertex3f() * frameA.Column1.Vertex3f().Norm();
            Vertex3f c2 = res.Column2.Vertex3f() * frameA.Column2.Vertex3f().Norm();

            res = new Matrix4x4f(c0.x, c0.y, c0.z, 0, 
                                c1.x, c1.y, c1.z, 0, 
                                c2.x, c2.y, c2.z, 0, 
                                pos.x, pos.y, pos.z, 1);
            return res;
        }

        public static BonePose InterpolateSlerp(BonePose frameA, BonePose frameB, float progression)
        {
            if (frameA == null && frameB != null) frameA = frameB;
            if (frameA != null && frameB == null) frameB = frameA;

            Vertex3f pos = InterpolateLerp(frameA.Position, frameB.Position, progression);
            ZetaExt.Quaternion rot = frameA.Rotation.Interpolate(frameB.Rotation, progression);
            return new BonePose(pos, rot);
        }

        private static Vertex3f InterpolateLerp(Vertex3f start, Vertex3f end, float progression)
        {
            return start + (end - start) * progression;
        }

    }
}
