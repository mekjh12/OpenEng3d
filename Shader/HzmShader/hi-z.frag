#version 400 core

uniform sampler2D DepthBuffer;
uniform ivec2 LastMipSize;
in vec2 TexCoord;
out float depth;

void main(void)
{
	// TexCoord (0~1)를 픽셀 좌표로 변환
	vec2 pixelCoord = TexCoord * vec2(LastMipSize);
	
	// 2x2 영역의 시작점 계산
	ivec2 baseCoord = ivec2(floor(pixelCoord * 0.5)) * 2;
	
	// 텍셀 크기 (정규화된 좌표)
	vec2 texelSize = 1.0 / vec2(LastMipSize);
	
	// 2x2 영역의 깊이값 샘플링
	vec4 texels;
	texels.x = texture(DepthBuffer, (vec2(baseCoord.x + 0, baseCoord.y + 0) + vec2(0.5)) * texelSize).x;
	texels.y = texture(DepthBuffer, (vec2(baseCoord.x + 1, baseCoord.y + 0) + vec2(0.5)) * texelSize).x;
	texels.z = texture(DepthBuffer, (vec2(baseCoord.x + 1, baseCoord.y + 1) + vec2(0.5)) * texelSize).x;
	texels.w = texture(DepthBuffer, (vec2(baseCoord.x + 0, baseCoord.y + 1) + vec2(0.5)) * texelSize).x;
	
	float maxZ = max(max(texels.x, texels.y), max(texels.z, texels.w));
	
	// 홀수 너비 처리 - 오른쪽 가장자리
	if ((LastMipSize.x & 1) != 0 && baseCoord.x + 2 == LastMipSize.x - 1) {
		float extra_x = texture(DepthBuffer, (vec2(baseCoord.x + 2, baseCoord.y + 0) + vec2(0.5)) * texelSize).x;
		float extra_y = texture(DepthBuffer, (vec2(baseCoord.x + 2, baseCoord.y + 1) + vec2(0.5)) * texelSize).x;
		maxZ = max(maxZ, max(extra_x, extra_y));
	}
	
	// 홀수 높이 처리 - 아래쪽 가장자리
	if ((LastMipSize.y & 1) != 0 && baseCoord.y + 2 == LastMipSize.y - 1) {
		float extra_x = texture(DepthBuffer, (vec2(baseCoord.x + 0, baseCoord.y + 2) + vec2(0.5)) * texelSize).x;
		float extra_y = texture(DepthBuffer, (vec2(baseCoord.x + 1, baseCoord.y + 2) + vec2(0.5)) * texelSize).x;
		maxZ = max(maxZ, max(extra_x, extra_y));
	}
	
	depth = maxZ;
}