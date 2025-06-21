#version 420

in vec2 pass_textureCoords;

out vec4 out_colour;

uniform vec4 colour;
uniform sampler2D fontAtlas;

void main(void)
{
	vec4 color = texture(fontAtlas, pass_textureCoords);
	out_colour = vec4(colour.xyz, colour.a * color.a);
}
