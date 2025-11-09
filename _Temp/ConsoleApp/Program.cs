using System;
using GLFW;
using glfw3;
using OpenGL;

class Program
{
    private const string VertexShaderSource = @"
        #version 330 core
        layout(location = 0) in vec3 aPos;
        void main()
        {
            gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
        }";

    private const string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        void main()
        {
            FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
        }";

    static void Main()
    {
        // GLFW 초기화
        Glfw.Init();

        // OpenGL 3.3 Core Profile 설정
        Glfw.WindowHint(Hint.ContextVersionMajor, 3);
        Glfw.WindowHint(Hint.ContextVersionMinor, 3);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

        // 윈도우 생성
        var window = Glfw.CreateWindow(800, 600, "OpenGL Window", Monitor.None, Window.None);
        if (window == Window.None)
        {
            Glfw.Terminate();
            return;
        }

        Glfw.MakeContextCurrent(window);

        // 버텍스 데이터
        float[] vertices = {
            -0.5f, -0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
             0.0f,  0.5f, 0.0f
        };

        // VBO, VAO 생성
        uint vao = Gl.CreateVertexArray();
        uint vbo = Gl.CreateBuffer();

        Gl.BindVertexArray(vao);
        Gl.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        Gl.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices.Length, vertices, BufferUsageHint.StaticDraw);

        // 버텍스 속성 설정
        Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 3 * sizeof(float), IntPtr.Zero);
        Gl.EnableVertexAttribArray(0);

        // 셰이더 생성
        var vertexShader = Gl.CreateShader(ShaderType.VertexShader);
        Gl.ShaderSource(vertexShader, VertexShaderSource);
        Gl.CompileShader(vertexShader);

        var fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
        Gl.ShaderSource(fragmentShader, FragmentShaderSource);
        Gl.CompileShader(fragmentShader);

        // 셰이더 프로그램 생성 및 링크
        var shaderProgram = Gl.CreateProgram();
        Gl.AttachShader(shaderProgram, vertexShader);
        Gl.AttachShader(shaderProgram, fragmentShader);
        Gl.LinkProgram(shaderProgram);

        // 셰이더 객체 삭제
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);

        // 메인 루프
        while (!Glfw.WindowShouldClose(window))
        {
            // 배경색 설정 및 버퍼 초기화
            Gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            // 셰이더 프로그램 사용 및 삼각형 그리기
            Gl.UseProgram(shaderProgram);
            Gl.BindVertexArray(vao);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

            Glfw.SwapBuffers(window);
            Glfw.PollEvents();

            // ESC 키로 종료
            if (Glfw.GetKey(window, Keys.Escape) == InputState.Press)
                Glfw.SetWindowShouldClose(window, true);
        }

        // 리소스 정리
        Gl.DeleteVertexArray(vao);
        Gl.DeleteBuffer(vbo);
        Gl.DeleteProgram(shaderProgram);

        Glfw.Terminate();
    }
}