#version 430

in vec2 texCoords;
out vec4 fragColor;
uniform sampler2D modelTexture;

void main(void)
{
	vec4 textureColor4 = texture(modelTexture, texCoords);
	if (textureColor4.a < 0.05f) discard;
	fragColor = textureColor4;
}
