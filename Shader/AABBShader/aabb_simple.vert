#version 430

// 입력 데이터 불필요 (Uniform 사용)

void main()
{
    // 지오메트리 셰이더가 실행되도록 더미 위치만 전달
    gl_Position = vec4(0.0, 0.0, 0.0, 1.0); 
}