#version 420 core

in vec2 TexCoords;

uniform vec4 color;
uniform sampler2D screenTexture;
uniform vec2 texcoordShift;
uniform vec2 texcoordScale;
uniform bool enableTexture;

out vec4 FragColor;

void main(void)
{
	if (enableTexture)
	{
		FragColor = color * texture(screenTexture, texcoordScale * TexCoords + texcoordShift);
	}
	else
	{
		FragColor = color;
	}
}
