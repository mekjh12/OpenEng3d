#version 430

const vec3 Pos[4] = vec3[4] (
    vec3(-1.0, -1.0f, 0.01f),
    vec3(1.0, -1.0f, 0.01f),
    vec3(1.0, 1.0f, 0.01f),
    vec3(-1.0, 1.0f, 0.01f)
);

out vec3 WorldPos;

const int Indices[6] = int[6] (0, 1, 2, 2, 3, 0);

void main()
{
    int Index = Indices[gl_VertexID];
    vec3 vPos3 = Pos[Index];
    vec4 vPos4 = vec4(vPos3, 1.0f);
    gl_Position = vPos4;
    WorldPos = vPos3;
}