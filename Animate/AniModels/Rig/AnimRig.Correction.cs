using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZetaExt;

namespace Animate
{
    public partial class AnimRig
    {
        // -----------------------------------------------------------------------
        // 멤버변수
        // -----------------------------------------------------------------------

        // 모델과 모션 보정 행렬
        protected Matrix4x4f _correctionMatrix = Matrix4x4f.Identity;
        protected bool _useCorrectionMatrix = false;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public Matrix4x4f CorrectionMatrix { get => _correctionMatrix; set => _correctionMatrix = value; }
        public bool UseCorrectionMatrix { get => _useCorrectionMatrix; set => _useCorrectionMatrix = value; }



        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// 모델의 좌표계와 모션의 좌표계를 맞추기 위한 보정 행렬을 설정합니다.
        /// </summary>
        /// <param name="fromUp"></param>
        /// <param name="fromForward"></param>
        /// <param name="toUp"></param>
        /// <param name="toForward"></param>
        /// <param name="scale"></param>
        public void SetModelCorrection(Vertex3f fromUp, Vertex3f fromForward, Vertex3f toUp, Vertex3f toForward, float scale = 1.0f)
        {
            // 입력 벡터들을 정규화
            Vertex3f u = fromUp.Normalized;
            Vertex3f f = fromForward.Normalized;
            Vertex3f r = f.Cross(u).Normalized;

            Vertex3f U = toUp.Normalized;
            Vertex3f F = toForward.Normalized;
            Vertex3f R = F.Cross(U).Normalized;

            // 소스 좌표계 행렬 (열 벡터로 구성)
            Matrix4x4f A = new Matrix4x4f(
                f.x, f.y, f.z, 0,
                u.x, u.y, u.z, 0,
                r.x, r.y, r.z, 0,
                0, 0, 0, 1
            );

            // 타겟 좌표계 행렬 (열 벡터로 구성)
            Matrix4x4f B = new Matrix4x4f(
                F.x, F.y, F.z, 0,
                U.x, U.y, U.z, 0,
                R.x, R.y, R.z, 0,
                0, 0, 0, 1
            );

            // 변환 행렬 = 타겟 * 소스의역행렬
            Matrix4x4f rotationTransform = B * A.Transposed;

            // 스케일 행렬
            Matrix4x4f scaleMatrix = Matrix4x4f.Scaled(scale, scale, scale);

            // 최종 보정 행렬 = 회전 * 스케일
            _correctionMatrix = rotationTransform * scaleMatrix;
            _useCorrectionMatrix = true;
        }

        public void SetScaleCorrection(float scale)
        {
            _correctionMatrix = Matrix4x4f.Scaled(scale, scale, scale);
            _useCorrectionMatrix = true;
        }

        public void SetCustomCorrection(Matrix4x4f customMatrix)
        {
            _correctionMatrix = customMatrix;
            _useCorrectionMatrix = true;
        }

        public void ResetCorrection()
        {
            _correctionMatrix = Matrix4x4f.Identity;
            _useCorrectionMatrix = false;
        }


    }
}
