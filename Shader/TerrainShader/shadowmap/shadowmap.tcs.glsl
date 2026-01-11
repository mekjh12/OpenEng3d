#version 430

layout(vertices = 4) out;

in vec2 Tex1[];
out vec2 Tex2[];

void main()
{
    // 통과 데이터
    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;
    Tex2[gl_InvocationID] = Tex1[gl_InvocationID];

    if (gl_InvocationID == 0)
    {
        // Shadow Map은 고정된 낮은 테셀레이션 레벨 사용
        // 성능과 품질의 균형을 위해 8~16 정도 권장
        const float SHADOW_TESS_LEVEL = 12.0;
        
        gl_TessLevelOuter[0] = SHADOW_TESS_LEVEL;
        gl_TessLevelOuter[1] = SHADOW_TESS_LEVEL;
        gl_TessLevelOuter[2] = SHADOW_TESS_LEVEL;
        gl_TessLevelOuter[3] = SHADOW_TESS_LEVEL;
        gl_TessLevelInner[0] = SHADOW_TESS_LEVEL;
        gl_TessLevelInner[1] = SHADOW_TESS_LEVEL;
    }
}