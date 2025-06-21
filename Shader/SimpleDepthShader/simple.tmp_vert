#version 430

in vec3 position;
out vec4 viewPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 proj;

void main()
{
	vec4 fragPos = model * vec4(position, 1.0);
	viewPos = view * fragPos;
	gl_Position = proj * viewPos;
}
