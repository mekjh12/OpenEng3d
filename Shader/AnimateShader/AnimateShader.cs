using Common;
using OpenGL;

namespace Shader
{
    public class AnimateShader : ShaderProgram<AnimateShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            // 변환 행렬 유니폼
            model,              // 모델 변환 행렬
            view,               // 뷰 변환 행렬
            proj,               // 투영 변환 행렬
            mvp,                // 모델-뷰-투영 변환 행렬

            diffuseMap,         // 텍스처맵

            isSkinningEnabled,  // 스키닝 활성화 여부
            rigidBoneIndex,     // 강체 본 인덱스

            // 총 유니폼 개수
            Count
        }

        private static int MAX_JOINTS = 128;
        const string VERTEX_FILE = @"\Shader\AnimateShader\ani.vert";
        const string FRAGMENT_FILE = @"\Shader\AnimateShader\ani.frag";
        //const string GEOMETRY_FILE = @"\Shader\ani.geom";

        public AnimateShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
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

        public void LoadFinalAnimatedBoneMatrix(int index, Matrix4x4f matrix)
        {
            base.LoadMatrix(_location[$"finalAnimatedBoneMatrix[{index}]"], matrix);
        }

        protected override void GetAllUniformLocations()
        {
            // 유니폼 변수 이름을 이용하여 위치 찾기
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }

            for (int i = 0; i < MAX_JOINTS; i++)
            {
                UniformLocation($"finalAnimatedBoneMatrix[{i}]");
            }
        }
    }
}
