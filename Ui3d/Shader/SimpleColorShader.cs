using OpenGL;
using System;
using System.Drawing;

namespace Ui3d
{

    /// <summary>
    /// 간단한 색상 렌더링용 정적 셰이더
    /// 2D UI 배경과 같은 단색 사각형 렌더링에 사용
    /// </summary>
    public static class SimpleColorShader
    {
        private static uint _programId;
        private static bool _isInitialized = false;

        // Uniform location 캐시
        private static int _mvpLocation;
        private static int _colorLocation;
        private static int _alphaLocation;

        // 계산용 임시 변수 (GC 최소화)
        private static float[] _matrixBuffer = new float[16];
        private static float[] _colorBuffer = new float[4];

        private const string VERTEX_SHADER =
@"#version 330 core

layout(location = 0) in vec2 aPosition;

uniform mat4 uMVP;

void main()
{
    gl_Position = uMVP * vec4(aPosition, 0.0, 1.0);
}
";

        private const string FRAGMENT_SHADER =
@"#version 330 core

out vec4 FragColor;

uniform vec4 uColor;
uniform float uAlpha;

void main()
{
    FragColor = vec4(uColor.rgb, uColor.a * uAlpha);
}
";

        /// <summary>
        /// 셰이더 초기화 (앱 시작 시 한 번만 호출)
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                Console.WriteLine("Warning: SimpleColorShader already initialized");
                return;
            }

            CompileShader();
            _isInitialized = true;
            Console.WriteLine("SimpleColorShader initialized successfully");
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
            CheckShaderCompileErrors(vertexShader, "SimpleColorShader VERTEX");

            // 프래그먼트 셰이더 컴파일
            uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fragmentShader, new string[] { FRAGMENT_SHADER });
            Gl.CompileShader(fragmentShader);
            CheckShaderCompileErrors(fragmentShader, "SimpleColorShader FRAGMENT");

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
            _mvpLocation = Gl.GetUniformLocation(_programId, "uMVP");
            _colorLocation = Gl.GetUniformLocation(_programId, "uColor");
            _alphaLocation = Gl.GetUniformLocation(_programId, "uAlpha");

            // 디버깅용 경고
            if (_mvpLocation == -1) Console.WriteLine("Warning: uniform 'uMVP' not found");
            if (_colorLocation == -1) Console.WriteLine("Warning: uniform 'uColor' not found");
            if (_alphaLocation == -1) Console.WriteLine("Warning: uniform 'uAlpha' not found");
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
                    System.Text.StringBuilder infoLog = new System.Text.StringBuilder(logLength[0]);
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
                    System.Text.StringBuilder infoLog = new System.Text.StringBuilder(logLength[0]);
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
                throw new InvalidOperationException("SimpleColorShader not initialized. Call Initialize() first.");
            }
            Gl.UseProgram(_programId);
        }

        /// <summary>
        /// MVP 행렬 설정
        /// </summary>
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
        /// 색상 설정 (RGBA, 0.0 ~ 1.0)
        /// </summary>
        public static void SetColor(float r, float g, float b, float a = 1.0f)
        {
            if (_colorLocation == -1) return;

            _colorBuffer[0] = r;
            _colorBuffer[1] = g;
            _colorBuffer[2] = b;
            _colorBuffer[3] = a;

            Gl.Uniform4(_colorLocation, _colorBuffer);
        }

        /// <summary>
        /// 색상 설정 (Color 객체)
        /// </summary>
        public static void SetColor(Color color)
        {
            SetColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        /// <summary>
        /// 투명도 설정
        /// </summary>
        public static void SetAlpha(float alpha)
        {
            if (_alphaLocation == -1) return;
            Gl.Uniform1(_alphaLocation, alpha);
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
            Console.WriteLine("SimpleColorShader cleaned up");
        }
    }
}
