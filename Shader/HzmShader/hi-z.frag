#version 400 core

uniform sampler2D DepthBuffer;
uniform ivec2 LastMipSize;

in vec2 TexCoord;
out float depth;

void main(void)
{
	vec4 texels;
	texels.x = texture( DepthBuffer, TexCoord ).x;
	texels.y = textureOffset( DepthBuffer, TexCoord, ivec2(-1, 0) ).x;
	texels.z = textureOffset( DepthBuffer, TexCoord, ivec2(-1,-1) ).x;
	texels.w = textureOffset( DepthBuffer, TexCoord, ivec2( 0,-1) ).x;
	
	float maxZ = max( max( texels.x, texels.y ), max( texels.z, texels.w ) );

	// gl_FragCoord.xy는 윈도우 공간 좌표로 제공됩니다.
	// 좌표는 현재 렌더링 중인 뷰포트(Viewport)의 왼쪽 아래를 (0, 0)으로 하는 좌표 체계
	vec3 extra;
	// if we are reducing an odd-width texture then the edge fragments have to fetch additional texels
	if ( ( (LastMipSize.x & 1) != 0 ) && ( int(gl_FragCoord.x) == LastMipSize.x-3 ) ) 
	{
		// if both edges are odd, fetch the top-left corner texel
		if (((LastMipSize.y & 1) != 0 ) && ( int(gl_FragCoord.y) == LastMipSize.y-3)) 
		{
			extra.z = textureOffset( DepthBuffer, TexCoord, ivec2( 1, 1) ).x;
			maxZ = max( maxZ, extra.z );
		}
		extra.x = textureOffset( DepthBuffer, TexCoord, ivec2( 1, 0) ).x;
		extra.y = textureOffset( DepthBuffer, TexCoord, ivec2( 1,-1) ).x;
		maxZ = max( maxZ, max( extra.x, extra.y ) );
	} 
	else
	{
		// if we are reducing an odd-height texture then the edge fragments have to fetch additional texels
		if ( ( (LastMipSize.y & 1) != 0 ) && ( int(gl_FragCoord.y) == LastMipSize.y-3 ) ) 
		{
			extra.x = textureOffset( DepthBuffer, TexCoord, ivec2( 0, 1) ).x;
			extra.y = textureOffset( DepthBuffer, TexCoord, ivec2(-1, 1) ).x;
			maxZ = max( maxZ, max( extra.x, extra.y ) );
		}	
	}

	depth = maxZ;
}
