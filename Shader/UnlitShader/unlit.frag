#version 430

in vec2 vTexCoord;
in float vMaterialID;

out vec4 fragColor;
uniform sampler2DArray texArray;

void main(void)
{
	vec4 texColor = texture(texArray, vec3(vTexCoord, vMaterialID));
	if (texColor.a < 0.05f) discard;
	fragColor = texColor;
}