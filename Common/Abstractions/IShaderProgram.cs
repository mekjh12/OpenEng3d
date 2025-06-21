namespace Common.Abstractions
{
    // 모든 셰이더가 구현해야 하는 인터페이스
    public interface IShaderProgram
    {
        string Name { get; }
        uint ProgramID { get; }

        void InitCompileShader();
        void Bind();
        void Unbind();
        void CleanUp();
    }
}
