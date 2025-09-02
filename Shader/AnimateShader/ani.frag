#version 420 core

out vec4 out_colour;

in vec2 pass_textureCoords;
in vec3 pass_normals;
in vec4 pass_weights;

uniform sampler2D diffuseMap;
uniform vec3 lightDirection;

void main(void)
{
	//float factor = clamp(dot(lightDirection, pass_normals), 0, 1);
	vec4 color = texture(diffuseMap, pass_textureCoords);
	//vec3 finalColor = factor * color + 0.3f * color;
	//vec4 diffuseColour = vec4(finalColor, 1.0f);	

	// 투명 프래그먼트는 버림
	if (color.a < 0.1) discard;

	// 출력 색상 설정
	out_colour = vec4(color.xyz, 1.0f);
}