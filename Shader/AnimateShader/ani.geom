#version 420 core

layout (triangles) in;
layout (line_strip, max_vertices = 6) out;

in VS_OUT {
	vec3 pass_normals;
	vec2 pass_textureCoords;
	vec3 pass_weights;
} gs_in[];

out vec2 pass_textureCoords;
out vec3 pass_normals;
out vec3 pass_weights;

const float MAGNITUDE = 0.4;

uniform mat4 proj;

void GenerateLine(int index)
{
	gl_Position = proj * gl_in[index].gl_Position;
	pass_textureCoords = gs_in[index].pass_textureCoords;
	pass_normals = gs_in[index].pass_normals;
	pass_weights = gs_in[index].pass_weights;
	EmitVertex();
	gl_Position = proj * (gl_in[index].gl_Position + vec4(gs_in[index].pass_normals * MAGNITUDE, 1.0));
	pass_textureCoords = gs_in[index].pass_textureCoords;
	pass_normals = gs_in[index].pass_normals;
	pass_weights = gs_in[index].pass_weights;
	EmitVertex();
	EndPrimitive();
}

void main()
{
   GenerateLine(0);
   GenerateLine(1);
   GenerateLine(2);
}

