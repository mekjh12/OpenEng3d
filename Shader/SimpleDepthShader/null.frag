#version 430
in vec4 viewPos;

void main()
{
	gl_FragDepth = viewPos.z / 1000.0f;
}
