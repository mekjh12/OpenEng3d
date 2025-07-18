using Common.Mathematics;
using OpenGL;
using ZetaExt;

namespace Model3d
{
    public class TransformComponent : ITransformable
    {
        private Pose _pose;
        private Vertex3f _size;

        private bool _isMoved = true;          // [이동플래그] 이전 프레임에서 물체가 이용하였는지 유무, 처음 시작시 업데이트를 위해 true
        private Matrix4x4f _localBindMatrix;

        /// <summary>
        /// 생성자
        /// </summary>
        public TransformComponent()
        {
            _pose = new Pose(Quaternion4.Identity, Vertex3f.Zero);
            _size = Vertex3f.One;
            _localBindMatrix = Matrix4x4f.Identity;
        }

        /// <summary>
        /// 모델공간에서 로컬공간으로의 변환행렬
        /// </summary>
        public Matrix4x4f LocalBindMatrix
        {
            get => _localBindMatrix;
            set => _localBindMatrix = value;
        }

        /// <summary>
        /// 로컬공간에서 월드공간으로의 변환행렬
        /// </summary>
        public Matrix4x4f ModelMatrix
        {
            get
            {
                Matrix4x4f S = Matrix4x4F.Scaled(_size);
                Matrix4x4f R = _pose.Matrix4x4f;
                Matrix4x4f T = Matrix4x4f.Translated(_pose.Position.x, _pose.Position.y, _pose.Position.z);
                return T * R * S; // [순서 중요] 연산순서는 S->R->T순
            }
        }

        public Vertex3f Size
        {
            get => _size;
            set => _size = value;
        }

        public Vertex3f Position
        {
            get => _pose.Position;
            set => _pose.Position = value;
        }

        public Pose Pose
        {
            get => Pose; 
            set => Pose = value;
        }

        public bool IsMoved
        {
            get => _isMoved; 
            set => _isMoved = value;
        }

        public void LocalBindTransform(float sx = 1.0f, float sy = 1.0f, float sz = 1.0f,
            float rotx = 0, float roty = 0, float rotz = 0,
            float x = 0, float y = 0, float z = 0)
        {
            _localBindMatrix = Matrix4x4f.Translated(x, y, z) *
                    Matrix4x4f.RotatedX(rotx) *
                    Matrix4x4f.RotatedY(roty) *
                    Matrix4x4f.RotatedZ(rotz) *
                    Matrix4x4f.Scaled(sx, sy, sz);
        }

        public void Scale(float scaleX, float scaleY, float scaleZ)
        {
            _size.x = scaleX;
            _size.y = scaleY;
            _size.z = scaleZ;
        }

        public void Translate(float dx, float dy, float dz)
        {
            _pose.Position += new Vertex3f(dx, dy, dz);
        }

        public void Yaw(float deltaDegree)
        {
            Vertex3f up = -_pose.Matrix4x4f.Column2.Vertex3f(); // z 오른손 법칙
            Quaternion4 q = new Quaternion4(up, -deltaDegree);
            _pose.Quaternion = q * _pose.Quaternion;
        }

        public void Roll(float deltaDegree)
        {
            Vertex3f forward = _pose.Matrix4x4f.Column0.Vertex3f(); // y
            Quaternion4 q = new Quaternion4(forward, deltaDegree);
            _pose.Quaternion = q * _pose.Quaternion;
        }

        public void Pitch(float deltaDegree)
        {
            Vertex3f right = _pose.Matrix4x4f.Column1.Vertex3f(); // x
            Quaternion4 q = new Quaternion4(right, deltaDegree);
            _pose.Quaternion = q * _pose.Quaternion;
        }

        public void SetRollPitchAngle(float pitch, float yaw, float roll)
        {
            Vertex3f right = _pose.Matrix4x4f.Column1.Vertex3f();
            Vertex3f up = -_pose.Matrix4x4f.Column2.Vertex3f(); // z 오른손 법칙
            Vertex3f forward = _pose.Matrix4x4f.Column0.Vertex3f();

            Quaternion4 q1 = new Quaternion4(right, pitch);
            Quaternion4 q2 = new Quaternion4(up, yaw);
            Quaternion4 q3 = new Quaternion4(forward, roll);
            _pose.Quaternion = q3 * q1* q2 * Quaternion4.Identity;
        }
    }
}
