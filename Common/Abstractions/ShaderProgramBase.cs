using Common.Abstractions;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZetaExt;

namespace Common
{
    /// <summary>
    /// Zero-allocation 셰이더 프로그램 베이스 클래스
    /// Enum 제네릭 제약을 제거하여 GC 압력을 없앰
    /// </summary>
    public abstract class ShaderProgramBase : IShaderProgram
    {
        const string ERROR_COMPILE_PATH = "쉐이더를 사용하기 위해서는 실행 위치의 \\Shader\\{PATH}\\에 소스 코드를 넣어주세요.";
        private const string SHADER_BINARY_FOLDER = "ShaderBinaries";

        protected string _name;
        protected uint _programID;
        private uint _vertexShaderID;
        private uint _geometryShaderID;
        private uint _fragmentShaderID;
        private uint _tcsShaderID;
        private uint _tesShaderID;
        private uint _computeShaderID;

        protected string _vertFilename = "";
        protected string _geomFilename = "";
        protected string _tcsFilename = "";
        protected string _tesFilename = "";
        protected string _fragFilename = "";
        protected string _compFilename = "";

        private bool _isInitialized = false;

        // ✅ 재사용 버퍼 (Zero-allocation)
        private float[] _matrixBuffer = new float[16];
        private float[] _matrix3Buffer = new float[9];

        public string Name => _name;
        public uint ProgramID => _programID;

        public string ComputeFileName
        {
            get => _compFilename;
            set => _compFilename = value;
        }

        public string VertFileName
        {
            set => _vertFilename = value;
        }

        public string GeomFileName
        {
            set => _geomFilename = value;
        }

        public string FragFileName
        {
            set => _fragFilename = value;
        }

        public string TcsFileName
        {
            set => _tcsFilename = value;
        }

        public string TesFileName
        {
            set => _tesFilename = value;
        }

        /// <summary>
        /// 유니폼 위치를 초기화하는 추상 메서드
        /// 각 셰이더는 이 메서드에서 필요한 유니폼 위치를 캐싱해야 함
        /// </summary>
        protected abstract void GetAllUniformLocations();

        /// <summary>
        /// 어트리뷰트 바인딩을 위한 추상 메서드
        /// </summary>
        protected abstract void BindAttributes();

        #region Zero-allocation Uniform Helpers

        /// <summary>
        /// 유니폼 위치를 가져옴 (내부 사용용)
        /// </summary>
        protected int GetUniformLocation(string uniformName)
        {
            return Gl.GetUniformLocation(_programID, uniformName);
        }

        /// <summary>
        /// int 유니폼 로드
        /// </summary>
        protected void LoadUniform1i(int location, int value)
        {
            Gl.Uniform1i<int>(location, 1, value);
        }

        /// <summary>
        /// float 유니폼 로드
        /// </summary>
        protected void LoadUniform1f(int location, float value)
        {
            Gl.Uniform1f<float>(location, 1, value);
        }

        /// <summary>
        /// bool 유니폼 로드
        /// </summary>
        protected void LoadUniformBool(int location, bool value)
        {
            Gl.Uniform1i<int>(location, 1, value ? 1 : 0);
        }

        /// <summary>
        /// vec2 유니폼 로드 (Vertex2f)
        /// </summary>
        protected void LoadUniform2f(int location, Vertex2f vec)
        {
            Gl.Uniform2f(location, 1, vec);
        }

        /// <summary>
        /// ivec2 유니폼 로드
        /// </summary>
        protected void LoadUniform2i(int location, int x, int y)
        {
            Gl.Uniform2i(location, x, y);
        }

        /// <summary>
        /// ivec2 유니폼 로드 (Vertex2i)
        /// </summary>
        // ShaderProgramBase.cs (또는 해당 베이스 클래스)
        protected void LoadUniform2i(int location, Vertex2i value)
        {
            if (location == -1) return;  // 유니폼이 없으면 스킵

            Gl.Uniform2i(location, value.x, value.y);

            // ✅ 에러 체크
            ErrorCode error = Gl.GetError();
            if (error != ErrorCode.NoError)
            {
                throw new InvalidOperationException($"Uniform2i 설정 실패: {error}");
            }
        }

        /// <summary>
        /// vec3 유니폼 로드 (Vertex3f)
        /// </summary>
        protected void LoadUniform3f(int location, Vertex3f vec)
        {
            Gl.Uniform3f(location, 1, vec);
        }

        /// <summary>
        /// vec4 유니폼 로드 (Vertex4f)
        /// </summary>
        protected void LoadUniform4f(int location, Vertex4f vec)
        {
            Gl.Uniform4f(location, 1, vec);
        }

        /// <summary>
        /// mat4 유니폼 로드 (재사용 버퍼 사용)
        /// </summary>
        protected void LoadUniformMatrix4(int location, in Matrix4x4f matrix)
        {
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

        /// <summary>
        /// mat3 유니폼 로드 (재사용 버퍼 사용)
        /// </summary>
        protected void LoadUniformMatrix3(int location, in Matrix3x3f matrix)
        {
            _matrix3Buffer[0] = matrix[0, 0];
            _matrix3Buffer[1] = matrix[0, 1];
            _matrix3Buffer[2] = matrix[0, 2];
            _matrix3Buffer[3] = matrix[1, 0];
            _matrix3Buffer[4] = matrix[1, 1];
            _matrix3Buffer[5] = matrix[1, 2];
            _matrix3Buffer[6] = matrix[2, 0];
            _matrix3Buffer[7] = matrix[2, 1];
            _matrix3Buffer[8] = matrix[2, 2];

            Gl.UniformMatrix3(location, false, _matrix3Buffer);
        }

        #endregion

        #region Legacy Helper Methods (호환성 유지)

        /// <summary>
        /// [Legacy] 문자열로 유니폼 설정 (성능 저하 가능)
        /// 새 코드에서는 캐싱된 location을 사용하는 것을 권장
        /// </summary>
        public void SetInt(string name, int value)
        {
            int loc = Gl.GetUniformLocation(_programID, name);
            LoadUniform1i(loc, value);
        }

        protected void SetVec3(string uniformName, Vertex3f vec3)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            LoadUniform3f(loc, vec3);
        }

        protected void SetVec2(string uniformName, Vertex2f vec2)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            LoadUniform2f(loc, vec2);
        }

        protected void SetVec4(string uniformName, Vertex4f vec4)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            LoadUniform4f(loc, vec4);
        }

        protected void SetFloat(string uniformName, float value)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            LoadUniform1f(loc, value);
        }

        protected void SetBoolean(string uniformName, bool value)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            LoadUniformBool(loc, value);
        }

        protected void SetMatrix4x4(string uniformName, Matrix4x4f value)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            LoadUniformMatrix4(loc, value);
        }

        protected void SetMatrix3x3(string uniformName, Matrix3x3f value)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            LoadUniformMatrix3(loc, value);
        }

        #endregion

        public ShaderProgramBase()
        {
        }

        public virtual void InitCompileShader()
        {
            string shaderName = _name;

            // ─────────────────────────────────────────────────────
            // _isInitialized 상태에 따른 분기:
            //   false              → 첫 컴파일, GPU 자원 없음, 그냥 진행
            //   true + 파일 미변경 → 재컴파일 불필요, 조기 리턴
            //   true + 파일 변경됨 → 기존 GPU 자원 해제 후 재컴파일
            // ─────────────────────────────────────────────────────

            // 1. 조기 리턴: 변경 사항이 없으면 아무것도 하지 않음
            if (_isInitialized && !AnyShaderFileModified())
            {
                ShaderManager.AddCompileMessage($"[재컴파일 생략] {shaderName}");
                return;
            }

            // 2. 기존 자원 정리
            if (_isInitialized) CleanUp();

            // 3. 바이너리 로드 시도 (파일 수정이 없을 때만)
            if (!AnyShaderFileModified() && LoadCompiledShaderBinary(shaderName))
            {
                FinalizeShaderSetup();
                return;
            }

            // 4. 일반 컴파일 프로세스 시작
            _programID = Gl.CreateProgram();
            int compiledCount = 0;

            // 셰이더 타입별 파일 처리 (반복 구조 개선)
            compiledCount += TryAttachShader(_vertFilename, ShaderType.VertexShader, ref _vertexShaderID);
            compiledCount += TryAttachShader(_fragFilename, ShaderType.FragmentShader, ref _fragmentShaderID);
            compiledCount += TryAttachShader(_geomFilename, ShaderType.GeometryShader, ref _geometryShaderID);
            compiledCount += TryAttachShader(_tcsFilename, ShaderType.TessControlShader, ref _tcsShaderID);
            compiledCount += TryAttachShader(_tesFilename, ShaderType.TessEvaluationShader, ref _tesShaderID);
            compiledCount += TryAttachShader(_compFilename, ShaderType.ComputeShader, ref _computeShaderID);

            BindAttributes();

            // 5. 링크 및 검증
            Gl.LinkProgram(_programID);
            bool isLinked = CheckLinkStatus(shaderName);

            if (isLinked)
            {
                Gl.ValidateProgram(_programID);
                ShaderManager.AddCompileMessage($"** 쉐이더 빌드 {shaderName}, 파일수={compiledCount}");

                // 성공 시 바이너리 저장
                SaveCompiledShaderBinary(shaderName, _programID);
            }

            // 6. 후처리 (중간 셰이더 객체 삭제 및 유니폼 로드)
            CleanupIntermediateShaders();
            FinalizeShaderSetup();
        }

        // --- 보조 메서드들 ---

        private int TryAttachShader(string filename, ShaderType type, ref uint shaderId)
        {
            if (string.IsNullOrEmpty(filename)) return 0;

            if (!File.Exists(filename))
                throw new FileNotFoundException($"{filename}이 없습니다. {ERROR_COMPILE_PATH}");

            shaderId = LoadShader(filename, type);
            if (shaderId >= 0)
            {
                Gl.AttachShader(_programID, shaderId);
                return 1;
            }
            return 0;
        }

        private bool CheckLinkStatus(string shaderName)
        {
            Gl.GetProgram(_programID, ProgramProperty.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                StringBuilder sb = new StringBuilder(512);
                Gl.GetProgramInfoLog(_programID, 512, out _, sb);
                ShaderManager.AddCompileMessage($"Shader Program Linking Error ({shaderName}):\n{sb}");
                return false;
            }
            return true;
        }

        private void CleanupIntermediateShaders()
        {
            uint[] shaders = { _vertexShaderID, _fragmentShaderID, _geometryShaderID, _tcsShaderID, _tesShaderID, _computeShaderID };
            foreach (var id in shaders)
            {
                if (id > 0) Gl.DeleteShader(id);
            }
        }

        private void FinalizeShaderSetup()
        {
            GetAllUniformLocations();
            _isInitialized = true;
        }

        private string[] LoadTextFile(string fileName)
        {
            if (!File.Exists(fileName)) return null;

            List<string> includedFunctions = new List<string>();
            List<string> includedStructs = new List<string>();

            StringBuilder shaderSource = new StringBuilder();
            try
            {
                StreamReader sr = new StreamReader(fileName);
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    line = AnalysisLine(fileName, line, out List<string> funcs, out List<string> structs);
                    shaderSource.Append(line).Append("\n");

                    if (funcs.Count > 0) includedFunctions.AddRange(funcs);
                    if (structs.Count > 0) includedStructs.AddRange(structs);
                }
                sr.Close();
            }
            catch (IOException e)
            {
                ZetaExt.Debug.WriteLine("Could not read file! " + e.Message);
            }

            // 인클루드한 함수명을 찾아서 선언적 함수를 제거한다.
            if (includedFunctions.Count > 0)
            {
                for (int i = 0; i < includedFunctions.Count; i++)
                {
                    string funcLine = includedFunctions[i];
                    shaderSource.Replace(funcLine, "");
                }
            }

            // 인클루드한 구조체를 찾아서 선언적 함수를 제거한다.
            if (includedStructs.Count > 0)
            {
                for (int i = 0; i < includedStructs.Count; i++)
                {
                    string funcLine = includedStructs[i].Replace("delete", "struct");
                    shaderSource.Replace(funcLine, "");
                }
            }
            shaderSource.Replace("delete ", "struct ");

            // 줄별로 분리하여 문자열 배열로 리턴한다.
            string[] shaderSources = new string[shaderSource.Length];
            for (int i = 0; i < shaderSource.Length; i++)
            {
                shaderSources[i] = shaderSource[i].ToString();
            }

            return shaderSources;
        }

        // include 파일 경로를 수집하는 메서드 추가
        private HashSet<string> CollectIncludeFiles(string fileName)
        {
            var result = new HashSet<string>();
            if (!File.Exists(fileName)) return result;

            foreach (var line in File.ReadAllLines(fileName))
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("#include")) continue;

                string includePath = trimmed
                    .Replace("#include", "")
                    .Replace("\"", "")
                    .Trim();

                string fullPath = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(fileName), includePath));

                if (File.Exists(fullPath))
                {
                    result.Add(fullPath);
                    // 재귀: include 안의 include도 추적
                    foreach (var nested in CollectIncludeFiles(fullPath))
                        result.Add(nested);
                }
            }
            return result;
        }

        private string AnalysisLine(string fileName, string shaderSourceOneLine, out List<string> funcs, out List<string> structs)
        {
            funcs = new List<string>();
            structs = new List<string>();
            string result = "";
            string txt = shaderSourceOneLine;
            string fileNameWithoutPath = Path.GetFileName(fileName);
            string dir = Path.GetDirectoryName(fileName);

            // 인클루드 라인이면
            if (txt.StartsWith("#include"))
            {
                txt = txt.Replace(@"#include", "").Replace("\"", "").Trim();
                string includeFileName = dir + "\\" + txt;

                // 인클루드 파일이 있으면
                if (File.Exists(includeFileName))
                {
                    // 인클루드 파일을 가져온다.
                    string inc = File.ReadAllText(includeFileName);

                    // #version 삭제한다.
                    int a = inc.IndexOf("#version");
                    int b = a;
                    if (a >= 0)
                    {
                        b = inc.IndexOf('\n', a);
                        inc = inc.Replace(inc.Substring(a, b - a), "").Trim();
                    }

                    // 구조체를 모두 찾아서 제거한다.
                    int start = 0;
                    int end = 0;
                    while (start < inc.Length)
                    {
                        start = inc.IndexOf("struct ", end);
                        if (start >= 0)
                        {
                            end = inc.IndexOf("};", start);
                            if (end > start)
                            {
                                string stc = inc.Substring(start, end - start + 1).Replace("  ", "");
                                string delText = stc.Replace("struct", "delete");
                                inc = inc.Substring(0, start) + delText + inc.Substring(start + stc.Length);
                                structs.Add(delText + ";");
                            }
                            else
                            {
                                throw new Exception($"glsl struct 구문 오류입니다. {includeFileName}");
                            }
                        }
                        else
                        {
                            start = inc.Length;
                        }
                    };

                    result += inc;

                    // 함수선언부를 모두 찾아 리스트에 담는다.
                    start = 0;
                    string[] lines = result.Split('\n');
                    List<string> list = new List<string>();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (!line.StartsWith("\t") && !line.StartsWith(" ") && !line.StartsWith("{") && !line.StartsWith("}") && !line.StartsWith("//"))
                        {
                            string funcDeclared = line.Replace("{", "").Trim();
                            if (funcDeclared != "") funcs.Add(funcDeclared.Replace("  ", "") + ";");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{fileNameWithoutPath}: include파일이 없습니다. {includeFileName}");
                }
            }
            // 인클루드 라인이 아니면 그대로 읽어온다.
            else
            {
                result += txt;
            }

            return result;
        }

        private void WriteSource(string fileName, string source)
        {
            StreamWriter sw = new StreamWriter(fileName, false);
            sw.WriteLine(source);
            sw.Close();
        }

        private uint LoadShader(string fileName, ShaderType type, bool debugWrite = true)
        {
            if (!File.Exists(fileName)) return 0;

            string[] shaderSources = LoadTextFile(fileName);

            string fullSources = String.Join("", shaderSources);
            string ext = Path.GetExtension(fileName);

            if (debugWrite)
            {
                WriteSource(fileName.Replace(ext, ".tmp"), fullSources);
            }

            uint shaderID = Gl.CreateShader(type);
            Gl.ShaderSource(shaderID, shaderSources);
            Gl.CompileShader(shaderID);

            int param;
            Gl.GetShader(shaderID, ShaderParameterName.CompileStatus, out param);
            string shortFileName = Path.GetFileName(fileName);
            if (param == Gl.FALSE)
            {
                string msg = $"--->[실패] {_name} GLSL 컴파일 실패 {type} {shaderID} {fileName}";
                ShaderManager.AddCompileMessage(msg + $" Shader Program 에러");
            }
            else
            {
                string shaderName = Path.GetFileName(shortFileName);
                ShaderManager.AddCompileMessage($"**성공** {_name} GLSL 빌드 {shaderName} {type} [{shaderID}]");
            }

            return shaderID;
        }


        protected void BindAttribute(uint attribute, string variableName)
        {
            Gl.BindAttribLocation(_programID, attribute, variableName);
        }

        public void Bind()
        {
            Gl.UseProgram(_programID);
        }

        public void Unbind()
        {
            Gl.UseProgram(0);
        }

        public virtual void CleanUp()
        {
            Unbind();
            if (_vertexShaderID > 0) Gl.DetachShader(_programID, _vertexShaderID);
            if (_fragmentShaderID > 0) Gl.DetachShader(_programID, _fragmentShaderID);
            if (_geometryShaderID > 0) Gl.DetachShader(_programID, _geometryShaderID);
            if (_tcsShaderID > 0) Gl.DetachShader(_programID, _tcsShaderID);
            if (_tesShaderID > 0) Gl.DetachShader(_programID, _tesShaderID);
            if (_computeShaderID > 0) Gl.DetachShader(_programID, _computeShaderID);

            if (_vertexShaderID > 0) Gl.DeleteShader(_vertexShaderID);
            if (_fragmentShaderID > 0) Gl.DeleteShader(_fragmentShaderID);
            if (_geometryShaderID > 0) Gl.DeleteShader(_geometryShaderID);
            if (_tcsShaderID > 0) Gl.DeleteShader(_tcsShaderID);
            if (_tesShaderID > 0) Gl.DeleteShader(_tcsShaderID);
            if (_computeShaderID > 0) Gl.DeleteShader(_computeShaderID);

            if (_programID > 0) Gl.DeleteProgram(_programID);
        }

        private bool AnyShaderFileModified()
        {
            bool modified = false;

            var topLevelFiles = new[]
            {
                _vertFilename, _fragFilename, _geomFilename, _tcsFilename, _tesFilename, _compFilename
            };

            foreach (var file in topLevelFiles)
            {
                if (string.IsNullOrEmpty(file)) continue;

                // 최상위 파일 체크
                if (FileHashManager.IsFileModified(file))
                    modified = true;

                // include 파일들도 재귀적으로 체크
                foreach (var includeFile in CollectIncludeFiles(file))
                {
                    if (FileHashManager.IsFileModified(includeFile))
                        modified = true;
                }
            }

            return modified;
        }

        private void SaveCompiledShaderBinary(string shaderName, uint programID)
        {
            try
            {
                Directory.CreateDirectory(SHADER_BINARY_FOLDER);

                Gl.GetProgram(programID, ProgramProperty.ProgramBinaryLength, out int length);

                if (length > 0)
                {
                    byte[] binary = new byte[length];
                    Gl.GetProgramBinary(programID, length, out int len, out int binaryFormat, binary);

                    string binaryFile = Path.Combine(SHADER_BINARY_FOLDER, shaderName + ".bin");
                    File.WriteAllBytes(binaryFile, binary);

                    File.WriteAllText(binaryFile + ".format", binaryFormat.ToString());

                    Console.WriteLine($"셰이더 바이너리 저장 완료: {shaderName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"셰이더 바이너리 저장 실패: {ex.Message}");
            }
        }

        private bool LoadCompiledShaderBinary(string shaderName)
        {
            string binaryFile = Path.Combine(SHADER_BINARY_FOLDER, shaderName + ".bin");
            string formatFile = binaryFile + ".format";

            if (!File.Exists(binaryFile) || !File.Exists(formatFile))
                return false;

            try
            {
                byte[] binary = File.ReadAllBytes(binaryFile);
                int format = int.Parse(File.ReadAllText(formatFile));

                _programID = Gl.CreateProgram();
                Gl.ProgramBinary(_programID, format, binary, binary.Length);

                int status;
                Gl.GetProgram(_programID, ProgramProperty.LinkStatus, out status);

                if (status == Gl.TRUE)
                {
                    ShaderManager.AddCompileMessage($"* 셰이더 바이너리 로드 성공: {_name}");
                    return true;
                }
                else
                {
                    StringBuilder infoLog = new StringBuilder(256);
                    Gl.GetProgramInfoLog(_programID, 256, out int len, infoLog);
                    ShaderManager.AddCompileMessage($"! 셰이더 바이너리 로드 실패: {infoLog.ToString()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"! 셰이더 바이너리 로드 오류: {ex.Message}");
                return false;
            }
        }
    }
}