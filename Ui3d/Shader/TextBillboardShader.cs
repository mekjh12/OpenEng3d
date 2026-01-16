using OpenGL;
using System;
using System.Text;

namespace Ui3d
{
    /// <summary>
    /// 인스턴싱 기반 텍스트 빌보드 렌더링용 정적 셰이더
    /// 하나의 쿼드를 여러 번 인스턴싱하여 효율적으로 텍스트 렌더링
    /// </summary>
    public static class TextBillboardShader
    {
        private static uint _programId;
        private static bool _isInitialized = false;

        // Uniform location 캐시
        private static int _mvpLocation;
        private static int _colorLocation;
        private static int _alphaLocation;
        private static int _textureLocation;
        private static int _nearDistanceLocation;   // ✅ 추가
        private static int _farDistanceLocation;    // ✅ 추가

        // 계산용 임시 변수
        private static float[] _matrixBuffer = new float[16];
        private static float[] _colorBuffer = new float[4];

        // ✅ 거리 설정 (조절 가능)
        private const float DEFAULT_NEAR_DISTANCE = 5.0f;   // 5m 이내: 부드럽게
        private const float DEFAULT_FAR_DISTANCE = 30.0f;   // 30m 이상: 선명하게

        private const string VERTEX_SHADER =
@"#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aOffset;
layout(location = 3) in vec4 aUVRect;
layout(location = 4) in vec2 aCharSize;

uniform mat4 mvp;

out vec2 TexCoord;
out float Distance;

void main()
{
    vec3 normalizedPos = aPosition + vec3(0.5, 0.5, 0.0);
    vec3 scaledPos = normalizedPos * vec3(aCharSize.x, aCharSize.y, 1.0);
    vec3 worldPos = scaledPos + aOffset;
    
    gl_Position = mvp * vec4(worldPos, 1.0);
    TexCoord = aUVRect.xy + aTexCoord * aUVRect.zw;
    
    Distance = length(worldPos);
}
";

        private const string FRAGMENT_SHADER =
        @"#version 430 core

in vec2 TexCoord;
in float Distance;
out vec4 FragColor;

uniform sampler2D atlasTexture;
uniform vec4 textColor;
uniform float alpha;
uniform float nearDistance;
uniform float farDistance;

void main() 
{
    vec4 texSample = texture(atlasTexture, TexCoord);
    
    float nearGamma = 1.8;
    float farGamma = 1.0;
    
    float t = clamp((Distance - nearDistance) / (farDistance - nearDistance), 0.0, 1.0);
    float gamma = mix(nearGamma, farGamma, t);
    
    float correctedAlpha = pow(texSample.a, gamma);
    
    vec3 color = textColor.rgb;
    float finalAlpha = correctedAlpha * alpha;
    
    if(finalAlpha < 0.01) discard;
    
    FragColor = vec4(color, finalAlpha);
}
";

        /// <summary>
        /// 셰이더 초기화 (앱 시작 시 한 번만 호출)
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                Console.WriteLine("Warning: TextBillboardShader already initialized");
                return;
            }

            CompileShader();
            _isInitialized = true;
            Console.WriteLine("TextBillboardShader initialized successfully");
        }

        /// <summary>
        /// 셰이더가 초기화되었는지 확인
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        private static void CompileShader()
        {
            // 버텍스 셰이더 컴파일
            uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vertexShader, new string[] { VERTEX_SHADER });
            Gl.CompileShader(vertexShader);
            CheckShaderCompileErrors(vertexShader, "TextBillboardShader VERTEX");

            // 프래그먼트 셰이더 컴파일
            uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fragmentShader, new string[] { FRAGMENT_SHADER });
            Gl.CompileShader(fragmentShader);
            CheckShaderCompileErrors(fragmentShader, "TextBillboardShader FRAGMENT");

            // 프로그램 링크
            _programId = Gl.CreateProgram();
            Gl.AttachShader(_programId, vertexShader);
            Gl.AttachShader(_programId, fragmentShader);
            Gl.LinkProgram(_programId);
            CheckProgramLinkErrors(_programId);

            // 셰이더 삭제 (이미 링크됨)
            Gl.DeleteShader(vertexShader);
            Gl.DeleteShader(fragmentShader);

            // Uniform location 한 번만 조회
            _mvpLocation = Gl.GetUniformLocation(_programId, "mvp");
            _colorLocation = Gl.GetUniformLocation(_programId, "textColor");
            _alphaLocation = Gl.GetUniformLocation(_programId, "alpha");
            _textureLocation = Gl.GetUniformLocation(_programId, "atlasTexture");
            _nearDistanceLocation = Gl.GetUniformLocation(_programId, "nearDistance");
            _farDistanceLocation = Gl.GetUniformLocation(_programId, "farDistance");

            // 디버깅용 경고
            if (_mvpLocation == -1) Console.WriteLine("Warning: uniform 'mvp' not found");
            if (_colorLocation == -1) Console.WriteLine("Warning: uniform 'textColor' not found");
            if (_alphaLocation == -1) Console.WriteLine("Warning: uniform 'alpha' not found");
            if (_textureLocation == -1) Console.WriteLine("Warning: uniform 'atlasTexture' not found");
            if (_nearDistanceLocation == -1) Console.WriteLine("Warning: uniform 'nearDistance' not found");
            if (_farDistanceLocation == -1) Console.WriteLine("Warning: uniform 'farDistance' not found");
        }

        private static void CheckShaderCompileErrors(uint shader, string type)
        {
            int[] success = new int[1];
            Gl.GetShader(shader, ShaderParameterName.CompileStatus, success);

            if (success[0] == 0)
            {
                int[] logLength = new int[1];
                Gl.GetShader(shader, ShaderParameterName.InfoLogLength, logLength);

                if (logLength[0] > 0)
                {
                    StringBuilder infoLog = new StringBuilder(logLength[0]);
                    Gl.GetShaderInfoLog(shader, logLength[0], out int length, infoLog);
                    Console.WriteLine($"셰이더 컴파일 에러 ({type}):");
                    Console.WriteLine(infoLog.ToString());
                }
                else
                {
                    Console.WriteLine($"셰이더 컴파일 에러 ({type}): 로그 없음");
                }
            }
        }

        private static void CheckProgramLinkErrors(uint program)
        {
            int[] success = new int[1];
            Gl.GetProgram(program, ProgramProperty.LinkStatus, success);

            if (success[0] == 0)
            {
                int[] logLength = new int[1];
                Gl.GetProgram(program, ProgramProperty.InfoLogLength, logLength);

                if (logLength[0] > 0)
                {
                    StringBuilder infoLog = new StringBuilder(logLength[0]);
                    Gl.GetProgramInfoLog(program, logLength[0], out int length, infoLog);
                    Console.WriteLine("프로그램 링크 에러:");
                    Console.WriteLine(infoLog.ToString());
                }
                else
                {
                    Console.WriteLine("프로그램 링크 에러: 로그 없음");
                }
            }
        }

        public static void Use()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("TextBillboardShader not initialized. Call Initialize() first.");
            }
            Gl.UseProgram(_programId);

            // ✅ 기본 거리 값 설정
            SetDistanceRange(DEFAULT_NEAR_DISTANCE, DEFAULT_FAR_DISTANCE);
        }

        public static void SetDistanceRange(float near, float far)
        {
            if (_nearDistanceLocation != -1)
                Gl.Uniform1(_nearDistanceLocation, near);
            if (_farDistanceLocation != -1)
                Gl.Uniform1(_farDistanceLocation, far);
        }

        public static void SetMVPMatrix(Matrix4x4f matrix)
        {
            if (_mvpLocation == -1) return;

            _matrixBuffer[0] = matrix[0, 0];
            _matrixBuffer[1] = matrix[0, 1];
            _matrixBuffer[2] = matrix[0, 2];
            _matrixBuffer[3] = matrix[0, 3];
            _matrixBuffer[4] = matrix[1, 0];
            _matrixBuffer[5] = matrix[1, 1];
            _matrixBuffer[6] = matrix[1, 2];
            _matrixBuffer[7] = matrix[1, 3];
            _matrixBuffer[8] = matrix[2, 0];
            _matrixBuffer[9] = matrix[2, 1];
            _matrixBuffer[10] = matrix[2, 2];
            _matrixBuffer[11] = matrix[2, 3];
            _matrixBuffer[12] = matrix[3, 0];
            _matrixBuffer[13] = matrix[3, 1];
            _matrixBuffer[14] = matrix[3, 2];
            _matrixBuffer[15] = matrix[3, 3];

            Gl.UniformMatrix4(_mvpLocation, false, _matrixBuffer);
        }

        /// <summary>
        /// 텍스트 색상 설정 (RGB)
        /// </summary>
        public static void SetTextColor(float r, float g, float b, float a = 1.0f)
        {
            if (_colorLocation == -1) return;

            _colorBuffer[0] = r;
            _colorBuffer[1] = g;
            _colorBuffer[2] = b;
            _colorBuffer[3] = a;

            Gl.Uniform4(_colorLocation, _colorBuffer);
        }

        /// <summary>
        /// 텍스트 색상 설정 (Color)
        /// </summary>
        public static void SetTextColor(System.Drawing.Color color)
        {
            SetTextColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        public static void SetAlpha(float value)
        {
            if (_alphaLocation == -1) return;
            Gl.Uniform1(_alphaLocation, value);
        }

        public static void SetAtlasTexture(uint textureId, int slot = 0)
        {
            if (_textureLocation == -1) return;

            Gl.ActiveTexture(TextureUnit.Texture0 + slot);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
            Gl.Uniform1(_textureLocation, slot);
        }

        /// <summary>
        /// 셰이더 리소스 정리 (앱 종료 시 호출)
        /// </summary>
        public static void Cleanup()
        {
            if (_programId != 0)
            {
                Gl.DeleteProgram(_programId);
                _programId = 0;
            }
            _isInitialized = false;
            Console.WriteLine("TextBillboardShader cleaned up");
        }
    }
}