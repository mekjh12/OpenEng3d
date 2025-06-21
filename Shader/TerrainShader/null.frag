#version 420 core
in vec4 viewPos;
//out float fragColor;

void main()
{
	//fragColor = viewPos.z / 500.0f;
	gl_FragDepth = viewPos.z / 10000.0f; 
}
