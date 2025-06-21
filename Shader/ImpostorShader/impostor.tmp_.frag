#version 430

// 지오메트리 셰이더에서 전달받은 입력 데이터
in GS_OUT {
    vec2 texCoord;    // 텍스처 좌표
} fs_in;

// 프래그먼트 셰이더의 출력 색상
out vec4 fragColor;

// 유니폼 변수들
uniform sampler2D impostorAtlas;     // 임포스터 아틀라스 텍스처
uniform float atlasSize;             // 아틀라스 전체 크기
uniform float individualSize;         // 개별 임포스터 이미지 크기
uniform vec2 atlasOffset;            // 아틀라스 내에서의 오프셋

// 상수 정의
const float EDGE_THRESHOLD = 0.0001f;  // 엣지 검출을 위한 임계값
const float ALPHA_THRESHOLD = 0.1f;   // 알파 테스트 임계값

void main()
{
    // 엣지 검출
    // 테두리 부분에 대한 처리를 개선하여 하드코딩된 값 제거
    bool isEdge = any(lessThan(fs_in.texCoord, vec2(EDGE_THRESHOLD))) || 
                  any(greaterThan(fs_in.texCoord, vec2(1.0 - EDGE_THRESHOLD)));
                  
    if (isEdge)
    {
        fragColor = vec4(0.5f, 0.5f, 0.0f, 0.5f);
        return;
    }
    
    // UV 좌표 계산 및 유효성 검사
    float uvScale = individualSize / atlasSize;
    vec2 localUV = fs_in.texCoord * uvScale;
    vec2 finalUV = atlasOffset + localUV;
    
    // UV 좌표가 유효 범위를 벗어나는지 검사
    if (any(greaterThan(finalUV, vec2(1.0))) || any(lessThan(finalUV, vec2(0.0))))
    {
        discard;
    }
    
    // 텍스처 샘플링 및 알파 테스트
    vec4 color = texture(impostorAtlas, finalUV);
    if (color.a < ALPHA_THRESHOLD)
    {
        discard;
    }
    
    fragColor = color;
}
