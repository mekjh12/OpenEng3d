using OpenGL;
using System.IO;
using System;
using Common;

namespace Shader
{
    /// <summary>
    /// 인스턴싱을 지원하는 임포스터 셰이더
    /// GPU Driven Rendering에서 Indirect Drawing과 함께 사용
    /// </summary>
    public class ImpostorInstancedShader : ShaderProgramBase
    {
        // 쉐이더 파일 경로 상수 정의
        private const string VERTEX_FILE = @"\Shader\ImpostorShader\impostor_instanced.vert";
        private const string FRAGMENT_FILE = @"\Shader\ImpostorShader\impostor_instanced.frag";
        private const string GEOMETRY_FILE = @"\Shader\ImpostorShader\impostor_instanced.gem.glsl";

        // 유니폼 위치 (캐싱)
        private int loc_atlasOffset;        // 제거 예정 (geometry shader에서 계산)
        private int loc_atlasSize;
        private int loc_impostorAtlas;
        private int loc_enableEdgeLine;
        private int loc_vp;
        private int loc_cameraPosition;
        private int loc_individualSize;
        private int loc_aabbSizeModel;
        private int loc_aabbCenterEntity;
        private int loc_horizontalFrames;   // 추가
        private int loc_verticalFrames;     // 추가
        private int loc_verticalAngleMin;
        private int loc_verticalAngleMax;

        public ImpostorInstancedShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            // 텍스처 아틀라스 관련
            loc_atlasOffset = GetUniformLocation("atlasOffset");      // 호환성 유지 (제거 가능)
            loc_atlasSize = GetUniformLocation("atlasSize");
            loc_impostorAtlas = GetUniformLocation("impostorAtlas");
            loc_enableEdgeLine = GetUniformLocation("enableEdgeLine");

            // 아틀라스 프레임 정보 (추가)
            loc_horizontalFrames = GetUniformLocation("horizontalFrames");
            loc_verticalFrames = GetUniformLocation("verticalFrames");

            // 변환 행렬
            loc_vp = GetUniformLocation("vp");

            // 위치 관련
            loc_cameraPosition = GetUniformLocation("cameraPosition");

            // 크기 관련
            loc_individualSize = GetUniformLocation("individualSize");

            // 경계 상자(AABB) 관련
            loc_aabbSizeModel = GetUniformLocation("aabbSizeModel");
            loc_aabbCenterEntity = GetUniformLocation("aabbCenterEntity");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        // === Load 메서드들 ===

        /// <summary>
        /// 아틀라스의 가로 프레임 수 설정
        /// </summary>
        public void LoadHorizontalFrames(int frames)
        {
            Gl.Uniform1(loc_horizontalFrames, frames);
        }

        /// <summary>
        /// 아틀라스의 세로 프레임 수 설정
        /// </summary>
        public void LoadVerticalFrames(int frames)
        {
            Gl.Uniform1(loc_verticalFrames, frames);
        }

        /// <summary>
        /// [Deprecated] Atlas offset 설정 - Geometry Shader에서 자동 계산됨
        /// 호환성을 위해 남겨두되 사용하지 않음
        /// </summary>
        [Obsolete("Atlas offset is now calculated per-instance in geometry shader")]
        public void LoadAtlasOffset(Vertex2f offset)
        {
            Gl.Uniform2f(loc_atlasOffset, 1, offset);
        }

        /// <summary>
        /// 텍스처 아틀라스 크기 설정
        /// </summary>
        public void LoadAtlasSize(float size)
        {
            Gl.Uniform1(loc_atlasSize, size);
        }

        /// <summary>
        /// 임포스터 텍스처 아틀라스 바인딩
        /// </summary>
        public void LoadImpostorAtlas(TextureUnit unit, uint textureId)
        {
            Gl.Uniform1(loc_impostorAtlas, (int)unit - (int)TextureUnit.Texture0);
            Gl.ActiveTexture(unit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        /// <summary>
        /// 테두리 렌더링 활성화 여부
        /// </summary>
        public void LoadEnableEdgeLine(bool enable)
        {
            Gl.Uniform1(loc_enableEdgeLine, enable ? 1 : 0);
        }

        /// <summary>
        /// View-Projection 행렬 설정
        /// </summary>
        public void LoadVPMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_vp, 1, false, matrix);
        }

        /// <summary>
        /// 카메라 위치 설정
        /// </summary>
        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3(loc_cameraPosition, position.x, position.y, position.z);
        }

        /// <summary>
        /// 개별 임포스터 크기 설정
        /// </summary>
        public void LoadIndividualSize(float size)
        {
            Gl.Uniform1(loc_individualSize, size);
        }

        /// <summary>
        /// 모델 AABB 크기 설정
        /// </summary>
        public void LoadAABBSizeModel(float size)
        {
            Gl.Uniform1(loc_aabbSizeModel, size);
        }

        /// <summary>
        /// 엔티티 AABB 중심점 설정
        /// </summary>
        public void LoadAABBCenterEntity(Vertex3f center)
        {
            Gl.Uniform3(loc_aabbCenterEntity, center.x, center.y, center.z);
        }
    }
}