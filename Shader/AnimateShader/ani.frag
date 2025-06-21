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
	vec3 color = texture(diffuseMap, pass_textureCoords).xyz;
	//vec3 finalColor = factor * color + 0.3f * color;
	//vec4 diffuseColour = vec4(finalColor, 1.0f);	
	out_colour = vec4(color, 1.0f);
}