#version 450 core

in vec2 pass_texCoord;
in vec3 pass_worldPosition;
in float pass_distanceToCamera;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp; 
    vec4 cameraPos;
} camera;

out vec4 fragColor;

uniform sampler2D fogTexture;
uniform sampler2D structureTexture;
uniform vec3 fogColor;
uniform float fogDensity;
uniform float alphaThreshold;
uniform int width;
uniform int height;

void main() 
{    
    // 화면 UV 좌표 계산
    vec2 screenUV = gl_FragCoord.xy / vec2(width, height);
    
    // 구조 버퍼에서 지형 깊이 복원
    vec4 structure = texture(structureTexture, screenUV);    
    float structureDepth = structure.b + structure.a;  // 양수 깊이
    
    // 빌보드 프래그먼트의 뷰 공간 깊이
    vec4 viewSpacePos = camera.view * vec4(pass_worldPosition, 1.0);
    float fragmentDepth = abs(viewSpacePos.z);
    
    // 깊이 차이 계산
    float depthDiff = abs(fragmentDepth - structureDepth);
    
    // 지형 표면 근처 페이드 (0~5 유닛)
    float depthFade = smoothstep(0.0, 5.0, depthDiff);
    
    // 외곽 페이드 (크로스 빌보드 가장자리)
    vec2 centeredUV = pass_texCoord - 0.5;
    float distFromCenter = length(centeredUV);
    float edgeFade = 1.0 - smoothstep(0.2, 0.5, distFromCenter);
    
    // 안개 텍스처 샘플링
    vec4 fogSample = texture(fogTexture, pass_texCoord);
    
    // 거리 페이드 (먼 거리)
    float distanceFade = 1.0 - smoothstep(400.0, 500.0, pass_distanceToCamera);
    
    // 최종 알파 계산
    float finalAlpha = fogSample.a * fogDensity * edgeFade * distanceFade * depthFade;
    
    // 알파가 너무 낮으면 조기 폐기 (성능 최적화)
    if (finalAlpha < alphaThreshold) {
        discard;
    }
    
    // 최종 색상 혼합
    vec3 finalColor = mix(fogColor, fogSample.rgb, 0.3);
    
    fragColor = vec4(finalColor, finalAlpha);
}