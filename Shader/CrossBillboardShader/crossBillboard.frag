#version 450 core
in vec2 fTexCoord;
in flat int fPlaneIndex;
in vec3 vNormal;
out vec4 fragColor;

uniform sampler2D atlasTexture;
uniform sampler2D normalTexture;
uniform bool enableEdgeLine;
uniform mat4 model;

const float EDGE_THRESHOLD = 0.01;

// 라이팅 UBO
layout(std140, binding = 1) uniform LightingBlock {    vec3 ambientColor;    vec3 lightDirection;    vec3 lightColor;} lighting;

layout(std140, binding = 0) uniform CameraBlock {    mat4 view;     mat4 proj;     mat4 vp;} camera;

void main()
{
    vec4 texColor = texture(atlasTexture, fTexCoord);

    // ✅ 엣지 라인 디버깅
    if (enableEdgeLine)
    {
        bool isEdge = any(lessThan(fTexCoord, vec2(EDGE_THRESHOLD))) || 
                      any(greaterThan(fTexCoord, vec2(1.0 - EDGE_THRESHOLD)));
                      
        if (isEdge)
        {
            fragColor = vec4(1.0, 1.0, 0.0, 1.0);
            return;
        }
    }

    if (texColor.a < 0.45) discard;
    
    // ✅ Normal Texture에서 노멀 샘플링
    vec4 normalSample = texture(normalTexture, fTexCoord);

    // ✅ 0~1 범위를 -1~1 범위로 디코딩
    vec3 objectSpaceNormal = normalSample.rgb * 2.0 - 1.0;
    objectSpaceNormal = normalize(objectSpaceNormal);
    
    // ✅ Model Space → World Space 변환
    // normal 변환은 inverse transpose를 사용해야 하지만
    // uniform scale이라면 mat3(model)만으로도 충분
    mat3 normalMatrix = mat3(transpose(inverse(model)));
    vec3 worldNormal = normalMatrix * objectSpaceNormal;
    
    // ✅ normalTexture의 방향 사용
    vec3 finalNormal = normalize(worldNormal);
    
    // ✅ 디버깅: 법선 방향 시각화 (주석 처리/해제로 토글)
    //fragColor = vec4(finalNormal * 0.5 + 0.5, 1.0);
    //return;
    
    // ✅ 디버깅: Object Space 노멀 확인
    // fragColor = vec4(objectSpaceNormal * 0.5 + 0.5, 1.0);
    // return;
    
    // ✅ 디버깅: 원본 Normal Texture 확인
    // fragColor = vec4(normalSample.rgb, 1.0);
    // return;
    
    // ✅ 라이팅 계산
    vec3 ambient = lighting.ambientColor;
    
    // ✅ 앞면/뒷면 구분
    vec3 litNormal = finalNormal;
    if (!gl_FrontFacing) {
        litNormal = -litNormal;
    }
    
    // ✅ Diffuse 계산
    float diff = max(dot(litNormal, -lighting.lightDirection), 0.0);
    
    // ✅ 뒷면은 약간 어둡게
    if (!gl_FrontFacing) {
        diff *= 0.2;
    }
    
    vec3 diffuse = diff * lighting.lightColor;
    vec3 finalLighting = ambient + diffuse;
    
    fragColor = vec4(texColor.rgb * finalLighting, texColor.a);
}