#version 430 core

in vec4 gViewPos;


void main()
{
    gl_FragDepth = gViewPos.z / 10000.0f;
}