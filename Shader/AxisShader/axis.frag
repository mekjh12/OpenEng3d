#version 430

in vec4 vertexColor;
out vec4 fragColor;

void main(void)
{
	fragColor = vertexColor;
}