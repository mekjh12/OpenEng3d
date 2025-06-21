using Common.Abstractions;
using OpenGL;

namespace Camera3d
{
    /// <summary>
    /// 객관적인 제3자 시점을 제공하는 관찰자 카메라 클래스입니다.
    /// 메인 카메라와는 독립적으로 시점과 각도를 제어할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 주요 기능:
    /// - 관찰 대상과 독립된 시점 제어
    /// - 자유로운 각도 조절
    /// - 메인 카메라와 다른 관점 제공
    /// 
    /// 사용 예시:
    /// - 게임의 리플레이/관전 모드
    /// - 시뮬레이션의 관찰자 시점
    /// - 교육용 보조 시점
    /// </remarks>
    public class SpectatorCamera : Camera
    {
        public SpectatorCamera(string name, float x, float y, float z) : base(name, x, y, z)
        {
        }

        public override Matrix4x4f ProjectiveRevMatrix 
        {
            get
            {
                throw new System.Exception("아직 기능이 제공되지 않습니다.");
                return Matrix4x4f.Identity;
            }
        }

        protected override void UpdateCameraVectors()
        {
            throw new System.NotImplementedException();
        }
    }
}