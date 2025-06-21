using Camera3d;
using Common.Abstractions;
using OpenGL;
using Shader;

namespace Cloud
{
    public class Clouds
    {
        int _width = 0;
        int _height = 0;
        Vertex3f _centerPosition;
        Vertex3f _boundSize;
        uint _ssbo = 0;
        uint _numCellsPerAxis = 10;
        float _gamma = 0.6f;
        float _stepLength = 0.1f;
        float _densityPower = 64;
        float _absorption = 0.3f;

        bool _isInBox = false;

        public float Absorption
        {
            get => _absorption;
            set => _absorption = value;
        }

        public float DensityPower
        {
            get => _densityPower;
            set => _densityPower = value;
        }

        public float Gamma
        {
            get => _gamma;
            set => _gamma = value;
        }

        public float StepLength
        {
            get => _stepLength;
            set => _stepLength = value;
        }

        public Vertex3f CenterPosition
        {
            get => _centerPosition;
            set => _centerPosition = value;
        }

        public Vertex3f BoundSize
        {
            get => _boundSize;
            set => _boundSize = value;
        }

        public void Init(int width, int height)
        {
            _boundSize = Vertex3f.One;
            _centerPosition = Vertex3f.Zero;

            _width = width;
            _height = height;

            _ssbo = WorleyNoise.CreateWorleyPointsBuffer(_numCellsPerAxis, "");
        }

        public void Update(int width, int height, float duration, Camera camera)
        {
            // 카메라가 구름 렌더링 상자 안에 있는지 판별한다.
            Vertex3f camPosition = camera.Position;
            Vertex3f absRelCamPos = camPosition - _centerPosition;

            _isInBox = absRelCamPos.x > -_boundSize.x && absRelCamPos.x < _boundSize.x &&
                absRelCamPos.y > -_boundSize.y && absRelCamPos.y < _boundSize.y &&
                absRelCamPos.z > -_boundSize.z && absRelCamPos.z < _boundSize.z;

        }

        public void Render(WorleyNoiseShader shader, uint vao, int count, Camera camera)
        {
            Gl.Enable(EnableCap.Blend);
            //Gl.BlendFunc(BlendingFactor.OneMinusSrcAlpha, BlendingFactor.DstAlpha);
            //Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusDstAlpha);
            Gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);

            // 상자안에 카메라가 위치한 경우에 컬링모드를 변경한다. 
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(_isInBox ? CullFaceMode.Front : CullFaceMode.Back);

            // 쉐이더를 시작한다.
            shader.Bind();

            shader.BindSSBO(_ssbo, (int)_numCellsPerAxis);

            shader.LoadFocalLength(camera.FocalLength);
            shader.LoadViewportSize(new Vertex2f(_width, _height));
            shader.LoadRayOrgin(camera.Position);
            shader.LoadAspectRatio(camera.AspectRatio);
            shader.LoadGamma(_gamma);
            shader.LoadAbsorption(_absorption);
            shader.LoadDensityPower(_densityPower);
            shader.LoadStepLength(_stepLength);
            shader.LoadCenterPosition(_centerPosition);
            shader.LoadBoundSize(_boundSize);
            shader.LoadCameraInsideCube(_isInBox);

            shader.LoadNumOfCellPerAxis(numCellsPerAxis: (int)_numCellsPerAxis);

            shader.LoadProjMatrix(camera.ProjectiveMatrix);
            shader.LoadViewMatrix(camera.ViewMatrix);
            shader.LoadModelMatrix(Matrix4x4f.Translated(_centerPosition.x, _centerPosition.y, _centerPosition.z) * Matrix4x4f.Scaled(_boundSize.x, _boundSize.y, _boundSize.z));

            Gl.BindVertexArray(vao);
            Gl.EnableVertexAttribArray(0);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, count);
            Gl.DisableVertexAttribArray(0);
            Gl.BindVertexArray(0);


            shader.Unbind();

            Gl.CullFace(CullFaceMode.Back);
        }

        public static bool IsIncludedByBox(Vertex3f position, Vertex3f boxBottom, Vertex3f boxTop)
        {
            return position.x - boxBottom.x > 0 &&
                   position.y - boxBottom.y > 0 &&
                   position.z - boxBottom.z > 0 &&
                   boxTop.x - position.x > 0 &&
                   boxTop.y - position.y > 0 &&
                   boxTop.z - position.z > 0;
        }
    }
}