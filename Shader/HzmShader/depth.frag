#version 420
uniform sampler2D DepthTexture;
uniform float LOD;
uniform float CameraNear;
uniform float CameraFar;
uniform bool IsPerspective;
in vec2 TexCoord;
out vec4 fragColor;

float LinearizeDepth(vec2 uv)
{
  float n = CameraNear;		// camera z near
  float f = CameraFar;		// camera z far
  float z = textureLod(DepthTexture, uv, LOD).x;
  return (2.0 * n) / (f + n - z * (f - n));
}

// 깊이 값을 파란색(작은 값) -> 빨간색(큰 값)으로 변환
vec3 DepthToColor(float depth)
{
    vec3 color;
    
    if (depth < 0.005)
    {
        // 0.0 ~ 0.005: 순수 파란색
        color = vec3(0.0, 0.0, 1.0);
    }
    else if (depth < 0.02)
    {
        // 0.005 ~ 0.02: 파란색 -> 초록색
        float t = (depth - 0.005) / 0.015;  // (depth - 0.005) / (0.02 - 0.005)
        color = vec3(0.0, t, 1.0 - t);
    }
    else if (depth < 0.1)
    {
        // 0.02 ~ 0.1: 초록색 -> 빨간색
        float t = (depth - 0.02) / 0.08;  // (depth - 0.02) / (0.1 - 0.02)
        color = vec3(t, 1.0 - t, 0.0);
    }
    else
    {
        // 0.1 ~ 1.0: 빨간색 -> 하얀색
        float t = (depth - 0.1) / 0.9;  // (depth - 0.1) / (1.0 - 0.1)
        color = vec3(1.0, t, t);
    }
    
    return color;
}

void main(void)
{
	float depth = IsPerspective ? LinearizeDepth(TexCoord) : textureLod(DepthTexture, TexCoord, LOD).x;
	
	// 색상으로 변환
	vec3 color = DepthToColor(depth);
	fragColor = vec4(color, 1.0);
}