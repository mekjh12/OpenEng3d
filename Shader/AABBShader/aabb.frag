#version 430

// 지오메트리 셰이더에서 전달받은 입력 데이터
in GS_OUT {
    vec4 color;
    vec3 normal;
    vec3 worldPos;
} fs_in;

// 프래그먼트 셰이더의 출력 색상
out vec4 fragColor;

void main()
{
    // 간단한 조명 계산 (옵션)
    vec3 lightDir = normalize(vec3(0.5, 0.5, 1.0));
    float diff = max(dot(fs_in.normal, lightDir), 0.3); // 최소 밝기 0.3
    
    // 최종 색상 출력
    fragColor = vec4(fs_in.color.rgb * diff, fs_in.color.a);
}