using Common.Abstractions;
using OpenGL;
using Shader;

namespace Animate
{
    // 1. 공통 인터페이스 정의
    public interface IAnimActor
    {
        string Name { get; }
        AnimRig AniRig { get; }
        Animator Animator { get; }
        Transform Transform { get; }
        float MotionTime { get; }
        Motionable CurrentMotion { get; }
        Matrix4x4f[] AnimatedTransforms { get; }
        Matrix4x4f ModelMatrix { get; }
        PolygonMode PolygonMode { get; set; }

        // 공통 기능들
        void Update(int deltaTime);
        void Render(Camera camera, Matrix4x4f vp, AnimateShader ashader, StaticShader sshader,
            bool isSkinVisible = true, bool isBoneVisible = false, bool isBoneParentCurrentVisible = false);
        void SetMotion(string motionName, float blendingInterval = 0.2f);
        void SetMotionOnce(string motionName);
        void SetBlendMotionFactor(string name, float blendFactor);
    }
}
