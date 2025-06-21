#version 430
layout (location = 0) in vec3 position;

uniform mat4 mvp;
uniform mat4 view;
uniform mat4 model;

out vec3 fragPos;

void main(void)
{
	fragPos = (view * model * vec4(position, 1.0)).xyz;
	gl_Position = mvp * vec4(position, 1.0);
	//fragPos = gl_Position;
}