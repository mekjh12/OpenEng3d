using Common.Abstractions;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZetaExt;

namespace Common
{
    public abstract class ShaderProgram<TEnum> : UniformLoader<TEnum>, IShaderProgram where TEnum : Enum
    {
        const string ERROR_COMPILE_PATH = "쉐이더를 사용하기 위해서는 실행 위치의 \\Shader\\{PATH}\\에 소스 코드를 넣어주세요.";
        // 바이너리 셰이더 저장 폴더
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

        // 파일 내용 해시를 저장하는 Dictionary
        private Dictionary<string, string> _fileContentHash = new Dictionary<string, string>();
        private bool _isInitialized = false;

        // 각 셰이더마다 하나의 재사용 버퍼
        private float[] _matrixBuffer = new float[16]; 

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

        public string FragFilename
        {
            set => _fragFilename = value;
        }

        public string TcsFilename
        {
            set => _tcsFilename = value;
        }

        public string TesFilename
        {
            set => _tesFilename = value;
        }

        /// <summary>
        /// 모든 유니폼 변수의 위치값을 자동으로 가져오는 함수
        /// </summary>
        protected virtual void GetAllUniformLocations()
        {
            _location = new Dictionary<string, int>();

            // enum의 모든 값 가져오기
            var values = Enum.GetValues(typeof(TEnum));
            // 마지막 값(Count) 구하기
            int maxValue = (int)(object)values.Cast<TEnum>().Max();

            foreach (TEnum uniformName in values)
            {
                // 현재 enum 값이 마지막 값(Count)이 아닌 경우에만 처리
                if ((int)(object)uniformName != maxValue)
                {
                    string name = uniformName.ToString();
                    _location[name] = GetUniformLocation(name);
                }
            }
        }

        protected abstract void BindAttributes();

        public void SetInt(string name, int value)
        {
            Gl.Uniform1(Gl.GetUniformLocation(_programID, name), value);
        }

        protected void SetVec3(string uniformName, Vertex3f vec3)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            base.LoadVector(loc, vec3);
        }

        protected void SetVec2(string uniformName, Vertex2f vec2)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            base.LoadVector(loc, vec2);
        }

        protected void SetVec4(string uniformName, Vertex4f vec4)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            base.LoadVector(loc, vec4);
        }

        protected void SetFloat(string uniformName, float value)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            base.LoadFloat(loc, value);
        }

        protected void SetBoolean(string uniformName, bool value)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            base.LoadBoolean(loc, value);
        }

        protected void SetMatrix4x4(string uniformName, Matrix4x4f value)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            base.LoadMatrix(loc, value);
        }

        protected void SetMatrix3x3(string uniformName, Matrix3x3f value)
        {
            int loc = Gl.GetUniformLocation(_programID, uniformName);
            base.LoadMatrix(loc, value);
        }

        protected override void LoadMatrix(int location, in Matrix4x4f matrix)
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

        protected int UniformLocation(string uniformName)
        {
            // Dictionary에 이미 있는지 확인
            if (_location.TryGetValue(uniformName, out int location))
                return location;

            // 없으면 새로 가져와서 저장
            int uni = GetUniformLocation(uniformName);
            _location[uniformName] = uni;
            return uni;
        }

        protected void UniformLocations(params string[] uniformNames)
        {
            foreach (string uniformName in uniformNames)
            {
                _location.Add(uniformName, GetUniformLocation(uniformName));
            }
        }

        public ShaderProgram()
        {

        }

        public virtual void InitCompileShader()
        {
            string shaderName = _name;
                        
            // 이미 초기화되었고 파일이 변경되지 않았다면 컴파일하지 않음
            if (_isInitialized && !AnyShaderFileModified())
            {
                Console.WriteLine($"[재컴파일을 생략] {_name}");
                return;
            }   

            // 기존 프로그램 정리
            if (_isInitialized)
            {
                CleanUp();
            }

            // 파일이 변경되지 않았고 바이너리가 있다면 바이너리 로드 시도
            if (!AnyShaderFileModified() && LoadCompiledShaderBinary(shaderName))
            {
                GetAllUniformLocations();
                _isInitialized = true;
                return;
            }

            // 바이너리 로드 실패 또는 파일 변경됨 - 일반 컴파일 진행
            _location = new Dictionary<string, int>();
            _programID = Gl.CreateProgram();

            // 셰이더 로드
            int success = 0;

            if (_vertFilename != "")
            {
                if (File.Exists(_vertFilename))
                {
                    _vertexShaderID = LoadShader(_vertFilename, ShaderType.VertexShader);
                    if (_vertexShaderID >= 0) success++;
                    Gl.AttachShader(_programID, _vertexShaderID);
                }
                else
                {
                    throw new Exception($"{_vertFilename}이 없습니다. {ERROR_COMPILE_PATH}");
                }
            }

            if (_fragFilename != "")
            {
                if (File.Exists(_fragFilename))
                {
                    _fragmentShaderID = LoadShader(_fragFilename, ShaderType.FragmentShader);
                    if (_fragmentShaderID >= 0) success++;
                    Gl.AttachShader(_programID, _fragmentShaderID);
                }
                else
                {
                    throw new Exception($"{_vertFilename}이 없습니다. {ERROR_COMPILE_PATH}");
                }
            }

            if (_geomFilename != "")
            {
                if (File.Exists(_geomFilename))
                {
                    _geometryShaderID = LoadShader(_geomFilename, ShaderType.GeometryShader);
                    if (_geometryShaderID >= 0) success++;
                    Gl.AttachShader(_programID, _geometryShaderID);
                }
                else
                {
                    throw new Exception($"{_vertFilename}이 없습니다. {ERROR_COMPILE_PATH}");
                }
            }

            if (_tcsFilename != "")
            {
                if (File.Exists(_tcsFilename))
                {
                    _tcsShaderID = LoadShader(_tcsFilename, ShaderType.TessControlShader);
                    if (_tcsShaderID >= 0) success++;
                    Gl.AttachShader(_programID, _tcsShaderID);
                }
                else
                {
                    throw new Exception($"{_vertFilename}이 없습니다. {ERROR_COMPILE_PATH}");
                }
            }

            if (_tesFilename != "")
            {
                if (File.Exists(_tesFilename))
                {
                    _tesShaderID = LoadShader(_tesFilename, ShaderType.TessEvaluationShader);
                    if (_tesShaderID >= 0) success++;
                    Gl.AttachShader(_programID, _tesShaderID);
                }
                else
                {
                    throw new Exception($"{_vertFilename}이 없습니다. {ERROR_COMPILE_PATH}");
                }
            }

            if (_compFilename != "")
            {
                if (File.Exists(_compFilename))
                {
                    _computeShaderID = LoadShader(_compFilename, ShaderType.ComputeShader);
                    if (_computeShaderID >= 0) success++;
                    Gl.AttachShader(_programID, _computeShaderID);
                }
                else
                {
                    throw new Exception($"{_compFilename}이 없습니다. {ERROR_COMPILE_PATH}");
                }
            }

            BindAttributes();

            Gl.LinkProgram(_programID);

            // **[추가] 셰이더 링크 오류 확인**
            int linkStatus;
            Gl.GetProgram(_programID,  ProgramProperty.LinkStatus, out linkStatus);
            if (linkStatus == 0)
            {
                StringBuilder stringBuilder = new StringBuilder(256);
                Gl.GetProgramInfoLog(_programID, 256, out int len, stringBuilder);
                Console.WriteLine($"Shader Program Linking Error ({shaderName}):\n{stringBuilder.ToString()}");
                // 링크 실패 시 프로그램 ID를 유효하지 않은 값으로 설정하거나 예외를 던지는 것을 고려할 수 있습니다.
                // _programID = 0;
                // throw new Exception($"Shader Program Linking Error: {stringBuilder.ToString()}");
            }

            Gl.ValidateProgram(_programID);

            ZetaExt.Debug.WriteLine($"** 쉐이더 빌드 {shaderName}, 파일수={success}");

            Gl.DeleteShader(_vertexShaderID);
            Gl.DeleteShader(_fragmentShaderID);
            Gl.DeleteShader(_geometryShaderID);
            Gl.DeleteShader(_tcsShaderID);
            Gl.DeleteShader(_tesShaderID);
            Gl.DeleteShader(_computeShaderID);

            GetAllUniformLocations();

            // 컴파일 성공 후 바이너리 저장 (링크가 성공해야 저장하는 것이 합리적입니다.)
            if (linkStatus != 0)
            {
                SaveCompiledShaderBinary(shaderName, _programID);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// * 한 줄에 include를 검출하여 읽어오고 <br/>
        /// * 없으면 그대로 읽어온다.<br/>
        /// * 함수명을 모두 반환한다.<br/>
        /// * include 파일의 규칙
        /// * //#include 'filename.ext'
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="shaderSourceOneLine"></param>
        /// <param name="funcs"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
                        inc = inc.Replace(inc.Substring(a, b-a), "").Trim();
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
                                inc = inc.Substring(0,start) + delText + inc.Substring(start + stc.Length);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="source"></param>
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

            //File.WriteAllText(fileName.Replace(ext, $".tmp_{ext.Replace(".","")}"), fullSources);

            if (debugWrite)
            {
                WriteSource(fileName.Replace(ext, ".tmp"), fullSources);
            }

            uint shaderID = Gl.CreateShader(type);
            Gl.ShaderSource(shaderID, shaderSources);
            Gl.CompileShader(shaderID);

            int param;
            Gl.GetShader(shaderID, ShaderParameterName.CompileStatus, out param);
            string shortFileName = Path.GetFileName(fileName);//.Replace(EngineLoop.PROJECT_PATH, "");
            if (param == Gl.FALSE)
            {
                string msg = $"--->[실패] {_name} GLSL 컴파일 실패 {type} {shaderID} {fileName}";
                Console.WriteLine(msg + $" Shader Program 에러");
            }
            else
            {
                string shaderName = Path.GetFileName(shortFileName);
                Console.WriteLine($"(성공) {_name} GLSL 빌드 {shaderName} {type} [{shaderID}]");
            }

            return shaderID;
        }

        protected override int GetUniformLocation(string uniformName)
        {
            return Gl.GetUniformLocation(_programID, uniformName);
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

        /// <summary>
        /// 모든 셰이더 파일이 변경되었는지 확인
        /// </summary>
        /// <returns>하나라도 변경된 파일이 있으면 true, 아니면 false</returns>
        private bool AnyShaderFileModified()
        {
            bool modified = false;

            if (_vertFilename != "" && FileHashManager.IsFileModified(_vertFilename))
                modified = true;

            if (_fragFilename != "" && FileHashManager.IsFileModified(_fragFilename))
                modified = true;

            if (_geomFilename != "" && FileHashManager.IsFileModified(_geomFilename))
                modified = true;

            if (_tcsFilename != "" && FileHashManager.IsFileModified(_tcsFilename))
                modified = true;

            if (_tesFilename != "" && FileHashManager.IsFileModified(_tesFilename))
                modified = true;

            if (_compFilename != "" && FileHashManager.IsFileModified(_compFilename))
                modified = true;

            return modified;
        }

        /// <summary>
        /// 컴파일된 셰이더 바이너리를 파일로 저장
        /// </summary>
        /// <param name="shaderName">셰이더 이름</param>
        /// <param name="programID">셰이더 프로그램 ID</param>
        private void SaveCompiledShaderBinary(string shaderName, uint programID)
        {
            try
            {
                // 저장 디렉토리 생성
                Directory.CreateDirectory(SHADER_BINARY_FOLDER);

                // 바이너리 크기 확인
                Gl.GetProgram(programID, ProgramProperty.ProgramBinaryLength,out int length);

                if (length > 0)
                {
                    // 바이너리 데이터 가져오기
                    byte[] binary = new byte[length];
                    Gl.GetProgramBinary(programID, length, out int len, out int binaryFormat, binary);

                    // 파일로 저장
                    string binaryFile = Path.Combine(SHADER_BINARY_FOLDER, shaderName + ".bin");
                    File.WriteAllBytes(binaryFile, binary);

                    // 포맷 정보 저장
                    File.WriteAllText(binaryFile + ".format", binaryFormat.ToString());

                    Console.WriteLine($"셰이더 바이너리 저장 완료: {shaderName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"셰이더 바이너리 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 저장된 셰이더 바이너리를 로드
        /// </summary>
        /// <param name="shaderName">셰이더 이름</param>
        /// <returns>로드 성공 여부</returns>
        private bool LoadCompiledShaderBinary(string shaderName)
        {
            string binaryFile = Path.Combine(SHADER_BINARY_FOLDER, shaderName + ".bin");
            string formatFile = binaryFile + ".format";

            // 바이너리와 포맷 파일이 모두 존재하는지 확인
            if (!File.Exists(binaryFile) || !File.Exists(formatFile))
                return false;

            try
            {
                // 바이너리와 포맷 로드
                byte[] binary = File.ReadAllBytes(binaryFile);
                int format = int.Parse(File.ReadAllText(formatFile));

                // 프로그램 생성 및 바이너리 로드
                _programID = Gl.CreateProgram();
                Gl.ProgramBinary(_programID, format, binary, binary.Length);

                // 링크 상태 확인
                int status;
                Gl.GetProgram(_programID, ProgramProperty.LinkStatus, out status);

                if (status == Gl.TRUE)
                {
                    Console.WriteLine($"* 셰이더 바이너리 로드 성공: {_name}");
                    return true;
                }
                else
                {
                    StringBuilder infoLog = new StringBuilder(256);
                    Gl.GetProgramInfoLog(_programID, 256, out int len, infoLog);
                    Console.WriteLine($"! 셰이더 바이너리 로드 실패: {infoLog.ToString()}");
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
