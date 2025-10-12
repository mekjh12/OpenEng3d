using OpenGL;
using System;
using System.Text;

namespace Ui3d
{
    /// <summary>
    /// 빌보드 렌더링용 셰이더
    /// </summary>
    public class BillboardShader : IDisposable
    {
        private uint _programId;

        private const string VERTEX_SHADER = @"
#version 430 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

uniform mat4 mvp;

out vec2 TexCoord;

void main()
{
    gl_Position = mvp * vec4(aPosition, 1.0);
    TexCoord = aTexCoord;
}
";

        private const string FRAGMENT_SHADER = @"
#version 430 core
in vec2 TexCoord;
out vec4 FragColor;
uniform sampler2D billboardTexture;
uniform float alpha;
void main() {
    vec4 texCol = texture(billboardTexture, TexCoord);
    texCol.a *= alpha;
    if(texCol.a < 0.01) discard;    
    FragColor = texCol;
}";


        public BillboardShader()
        {
            CompileShader();
        }

        private void CompileShader()
        {
            // 버텍스 셰이더 컴파일
            uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vertexShader, new string[] { VERTEX_SHADER });
            Gl.CompileShader(vertexShader);
            CheckShaderCompileErrors(vertexShader, "BillboardShader VERTEX");

            // 프래그먼트 셰이더 컴파일
            uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fragmentShader, new string[] { FRAGMENT_SHADER });
            Gl.CompileShader(fragmentShader);
            CheckShaderCompileErrors(fragmentShader, "BillboardShader FRAGMENT");

            // 프로그램 링크
            _programId = Gl.CreateProgram();
            Gl.AttachShader(_programId, vertexShader);
            Gl.AttachShader(_programId, fragmentShader);
            Gl.LinkProgram(_programId);
            CheckProgramLinkErrors(_programId);

            // 셰이더 삭제 (이미 링크됨)
            Gl.DeleteShader(vertexShader);
            Gl.DeleteShader(fragmentShader);
        }

        /// <summary>
        /// 셰이더 컴파일 에러 체크
        /// </summary>
        private void CheckShaderCompileErrors(uint shader, string type)
        {
            int[] success = new int[1];
            Gl.GetShader(shader, ShaderParameterName.CompileStatus, success);

            if (success[0] == 0)
            {
                // 로그 길이 얻기
                int[] logLength = new int[1];
                Gl.GetShader(shader, ShaderParameterName.InfoLogLength, logLength);

                if (logLength[0] > 0)
                {
                    // 로그 가져오기
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

        /// <summary>
        /// 프로그램 링크 에러 체크
        /// </summary>
        private void CheckProgramLinkErrors(uint program)
        {
            int[] success = new int[1];
            Gl.GetProgram(program, ProgramProperty.LinkStatus, success);

            if (success[0] == 0)
            {
                // 로그 길이 얻기
                int[] logLength = new int[1];
                Gl.GetProgram(program, ProgramProperty.InfoLogLength, logLength);

                if (logLength[0] > 0)
                {
                    // 로그 가져오기
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

        public void Use()
        {
            Gl.UseProgram(_programId);
        }

        private float[] _matrixBuffer = new float[16];

        public void SetMatrix(string name, Matrix4x4f matrix)
        {
            int location = Gl.GetUniformLocation(_programId, name);
            if (location == -1)
            {
                Console.WriteLine($"Warning: uniform '{name}' not found");
                return;
            }

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
            Gl.UniformMatrix4(location, false, _matrixBuffer);
        }

        public void SetFloat(string name, float value)
        {
            int location = Gl.GetUniformLocation(_programId, name);
            if (location == -1)
            {
                Console.WriteLine($"Warning: uniform '{name}' not found");
                return;
            }
            Gl.Uniform1(location, value);
        }

        public void SetInt(string name, int value)
        {
            int location = Gl.GetUniformLocation(_programId, name);
            if (location == -1)
            {
                Console.WriteLine($"Warning: uniform '{name}' not found");
                return;
            }
            Gl.Uniform1(location, value);
        }

        public void SetTexture(string name, uint textureId, int slot)
        {
            int location = Gl.GetUniformLocation(_programId, name);
            if (location == -1)
            {
                Console.WriteLine($"Warning: uniform '{name}' not found");
                return;
            }

            Gl.ActiveTexture(TextureUnit.Texture0 + slot);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
            Gl.Uniform1(location, slot);
        }

        public void Dispose()
        {
            if (_programId != 0)
            {
                Gl.DeleteProgram(_programId);
                _programId = 0;
            }
        }
    }
}