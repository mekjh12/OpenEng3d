using Common;
using OpenGL;
using System;

namespace Shader
{
    public class AnimateShader : ShaderProgram<AnimateShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            // 변환 행렬 유니폼
            vp,                 // 뷰-투영 변환 행렬
            model,              // 모델 행렬
            diffuseMap,         // 텍스처맵
            isSkinningEnabled,  // 스키닝 활성화 여부
            rigidBoneIndex,     // 강체 본 인덱스
            pmodel,
            // 총 유니폼 개수
            Count
        }

        const string VERTEx_FILE = @"\Shader\AnimateShader\ani.vert";
        const string FRAGMENT_FILE = @"\Shader\AnimateShader\ani.frag";

        // UBO 관련 멤버
        private BoneMatrixUBO _boneMatrixUBO;

        private int _vp = 0;
        private int _model = 0;
        private int _isSkinningEnabled = 0;

        public AnimateShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEx_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;

            // UBO 초기화
            _boneMatrixUBO = new BoneMatrixUBO();

            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "in_position");
            base.BindAttribute(1, "in_textureCoords");
            base.BindAttribute(2, "in_normal");
            base.BindAttribute(4, "in_jointIndices");
            base.BindAttribute(5, "in_weights");
        }

        public override void InitCompileShader()
        {
            // 부모 클래스의 셰이더 컴파일 실행
            base.InitCompileShader();

            // 셰이더 컴파일 성공 후 UBO 초기화 및 바인딩
            if (ProgramID > 0)
            {
                _boneMatrixUBO.Initialize();
                _boneMatrixUBO.BindToShader(ProgramID, "BoneMatrices");
                Console.WriteLine($"UBO initialized for {_name}");
            }
        }

        /// <summary>
        /// UBO를 사용한 모든 본 행렬 업로드
        /// 기존 for 루프를 대체하여 성능을 크게 향상시킵니다.
        /// </summary>
        /// <param name="boneMatrices">본 행렬 배열</param>
        public void LoadAllBoneMatrices(Matrix4x4f[] boneMatrices)
        {
            _boneMatrixUBO.UploadBoneMatrices(boneMatrices);
        }

        public void LoadPMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["pmodel"], matrix);
        }

        protected override void GetAllUniformLocations()
        {
            // 기본 유니폼 변수 위치 찾기
            base.GetAllUniformLocations();

            // pmodel 유니폼 위치 찾기
            UniformLocation("pmodel");

            // 뷰-투영 및 모델 행렬 유니폼 위치 찾기
            _vp = GetUniformLocation("vp");
            _model = GetUniformLocation("model");
            _isSkinningEnabled = GetUniformLocation("isSkinningEnabled");
        }

        public void LoadVPMatrix(Matrix4x4f matrix)
        {
            LoadMatrix(_vp, matrix);
        }

        public void LoadModelMatrix(Matrix4x4f matrix)
        {
            LoadMatrix(_model, matrix);
        }

        public void LoadIsSkinningEnabled(bool isEnabled)
        {
            base.LoadBoolean(_isSkinningEnabled, isEnabled);
        }

        public override void CleanUp()
        {
            // UBO 정리
            _boneMatrixUBO?.Dispose();

            // 부모 클래스의 정리 작업
            base.CleanUp();
        }
    }
}