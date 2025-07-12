using Common.Mathematics;
using OpenGL;

namespace Common.Abstractions
{
    /// <summary>
    /// 월드공간에 변환성을 붙인다.
    /// </summary>
    public interface ITransformable
    {
        // Properties
        Matrix4x4f LocalBindMatrix { get; }

        Vertex3f Size { get; set; }

        Vertex3f Position { get; set; }

        Matrix4x4f ModelMatrix { get; }

        bool IsMoved { get; set; }

        Pose Pose { get; }

        // Transform Methods
        void LocalBindTransform(float sx = 1.0f, float sy = 1.0f, float sz = 1.0f,
            float rotx = 0, float roty = 0, float rotz = 0,
            float x = 0, float y = 0, float z = 0);

        void Scale(float scaleX, float scaleY, float scaleZ);

        void Translate(float dx, float dy, float dz);

        // Rotation Methods
        void SetRollPitchAngle(float pitch, float yaw, float roll); // 직접 각도 설정

        void Yaw(float deltaDegree);      // 상대적 회전
        void Roll(float deltaDegree);     // 상대적 회전
        void Pitch(float deltaDegree);    // 상대적 회전
    }
}
