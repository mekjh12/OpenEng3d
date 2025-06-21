#version 430

in vec3 position;
in vec2 textureCoords;

uniform mat4 mvp;

out vec2 texCoords;

void main(void)
{
	gl_Position = mvp * vec4(position, 1.0);
	texCoords = textureCoords;
}
