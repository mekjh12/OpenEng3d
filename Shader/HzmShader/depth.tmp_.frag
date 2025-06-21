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

void main(void)
{
	float depth = IsPerspective ? LinearizeDepth(TexCoord):  textureLod(DepthTexture, TexCoord, LOD).x;
	fragColor = vec4(depth, depth, depth, 1);
}
