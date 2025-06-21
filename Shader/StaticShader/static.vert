#version 420 core

// usefull shortcuts
#define saturate(x) clamp(x, 0.0, 1.0)

in vec3 position;
in vec2 textureCoords;
in vec3 color;

uniform mat4 model;
uniform mat4 proj;
uniform mat4 view;
uniform mat4 mvp;

out vec2 texCoords;
out vec4 fcolor;
out vec3 fragPos;
out vec4 finalPosition;

void main(void)
{
	finalPosition = mvp * vec4(position, 1.0);
	gl_Position = finalPosition;

	vec4 worldPos = model * vec4(position, 1.0);
	fragPos = (view * worldPos).xyz;

	texCoords = textureCoords;
	fcolor = vec4(color, 1.0f);
}