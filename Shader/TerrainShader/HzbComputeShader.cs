using Common;
using OpenGL;
using System.Xml.Linq;
using System;

public class HzbComputeShader : ShaderProgramBase
{
    const string COMPUTE_FILE = @"\Shader\TerrainShader\hzb_compute.comp";

    private int loc_inputSize;
    private int loc_outputSize;

    public HzbComputeShader(string projectPath) : base()
    {
        _name = this.GetType().Name;
        ComputeFileName = projectPath + COMPUTE_FILE;

        try
        {
            InitCompileShader();
            Console.WriteLine($"[{_name}] 컴파일 성공, Program ID: {ProgramID}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_name}] 컴파일 실패: {ex.Message}");
            throw;
        }
    }

    protected override void GetAllUniformLocations()
    {
        loc_inputSize = GetUniformLocation("inputSize");
        loc_outputSize = GetUniformLocation("outputSize");

        // ✅ 유니폼 위치 확인
        Console.WriteLine($"[{_name}] Uniform 'inputSize' location: {loc_inputSize}");
        Console.WriteLine($"[{_name}] Uniform 'outputSize' location: {loc_outputSize}");

        if (loc_inputSize == -1)
            Console.WriteLine($"⚠️ Warning: 'inputSize' uniform not found!");
        if (loc_outputSize == -1)
            Console.WriteLine($"⚠️ Warning: 'outputSize' uniform not found!");
    }

    protected override void BindAttributes()
    {
        // 컴퓨트 셰이더는 BindAttributes가 필요 없음
    }

    public void LoadInputSize(Vertex2i inputSize)
    {
        if (loc_inputSize == -1) return;

        // ✅ Gl.Uniform2 사용 (오버로드 자동 선택)
        Gl.Uniform2(loc_inputSize, inputSize.x, inputSize.y);
    }

    public void LoadOutputSize(Vertex2i outputSize)
    {
        if (loc_outputSize == -1) return;

        // ✅ Gl.Uniform2 사용
        Gl.Uniform2(loc_outputSize, outputSize.x, outputSize.y);
    }
}