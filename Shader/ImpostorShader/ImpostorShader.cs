using OpenGL;
using System.IO;
using System;
using Common;

namespace Shader
{
    /// <summary>
    /// 임포스터(게임에서 멀리 있는 오브젝트를 최적화하여 렌더링하는 기법) 쉐이더를 구현하는 클래스
    /// </summary>
    public class ImpostorShader : ShaderProgramBase
    {
        // 쉐이더 파일 경로 상수 정의
        private const string VERTEX_FILE = @"\Shader\ImpostorShader\impostor.vert";
        private const string FRAGMENT_FILE = @"\Shader\ImpostorShader\impostor.frag";
        private const string GEOMETRY_FILE = @"\Shader\ImpostorShader\impostor.gem.glsl";

        // 유니폼 위치 (캐싱)
        private int loc_atlasOffset;
        private int loc_atlasSize;
        private int loc_impostorAtlas;
        private int loc_enableEdgeLine;
        private int loc_model;
        private int loc_vp;
        private int loc_worldPosition;
        private int loc_cameraPosition;
        private int loc_individualSize;
        private int loc_aabbSizeModel;
        private int loc_aabbCenterEntity;

        public ImpostorShader(string projectPath) : base()
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
            loc_atlasOffset = GetUniformLocation("atlasOffset");
            loc_atlasSize = GetUniformLocation("atlasSize");
            loc_impostorAtlas = GetUniformLocation("impostorAtlas");
            loc_enableEdgeLine = GetUniformLocation("enableEdgeLine");

            // 변환 행렬 관련
            loc_model = GetUniformLocation("model");
            loc_vp = GetUniformLocation("vp");

            // 위치 관련
            loc_worldPosition = GetUniformLocation("worldPosition");
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
        /// 텍스처 아틀라스 오프셋 설정
        /// </summary>
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
            Gl.Uniform1(loc_impostorAtlas, (uint)unit);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }

        /// <summary>
        /// 테두리 렌더링 활성화 여부
        /// </summary>
        public void LoadEnableEdgeLine(bool enable)
        {
            Gl.Uniform1i(loc_enableEdgeLine, 1, enable ? 1 : 0);
        }

        /// <summary>
        /// 모델 변환 행렬 설정
        /// </summary>
        public void LoadModelMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_model, 1, false, matrix);
        }

        /// <summary>
        /// View-Projection 행렬 설정
        /// </summary>
        public void LoadVPMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_vp, 1, false, matrix);
        }

        /// <summary>
        /// 월드 공간 위치 설정
        /// </summary>
        public void LoadWorldPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_worldPosition, 1, position);
        }

        /// <summary>
        /// 카메라 위치 설정
        /// </summary>
        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3f(loc_cameraPosition, 1, position);
        }

        /// <summary>
        /// 개별 임포스터 크기 설정
        /// </summary>
        public void LoadIndividualSize(float size)
        {
            Gl.Uniform1f(loc_individualSize, 1, size);
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
            Gl.Uniform3f(loc_aabbCenterEntity, 1, center);
        }
    }
}