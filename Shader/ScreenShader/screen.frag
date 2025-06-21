#version 420 core
out vec4 FragColor;
in vec2 TexCoords;

uniform sampler2D screenTexture1;
uniform sampler2D depthTexture1;
uniform sampler2D screenTexture2;
uniform sampler2D depthTexture2;

uniform vec3 backgroundColor;
uniform vec2 viewport_size;
uniform bool isPositionInBox;

const float offset = 1.0 / 300.0;

void main()
{

    vec3 fogColor = vec3(1,1,1);
    vec2 uv = gl_FragCoord.xy / viewport_size;

    float cloudAlpha = texture(screenTexture1, uv).r; // 0<alpha<0.5f, negative
    float cloudDepth = texture(depthTexture1, uv).r;
    vec3 color = texture(screenTexture2, uv).xyz;
    float backgndDepth = texture(depthTexture2, uv).r;            

    vec2 offsets[9] = vec2[](
        vec2(-offset,  offset), // top-left
        vec2( 0.0f,    offset), // top-center
        vec2( offset,  offset), // top-right
        vec2(-offset,  0.0f),   // center-left
        vec2( 0.0f,    0.0f),   // center-center
        vec2( offset,  0.0f),   // center-right
        vec2(-offset, -offset), // bottom-left
        vec2( 0.0f,   -offset), // bottom-center
        vec2( offset, -offset)  // bottom-right    
    );

    float kernel[9] = float[](
        0.1f, 0.1f, 0.1f,
        0.1f, 0.2f, 0.1f,
        0.1f, 0.1f, 0.1f
    );
    
    float sampleTex[9];
    for(int i = 0; i < 9; i++)
    {
        sampleTex[i] = texture(screenTexture1, uv.st + offsets[i]).r;
    }

    float col = 0.0f;
    for(int i = 0; i < 9; i++)
    {
        //if (sampleTex[i]<0.01f) break;
        col += sampleTex[i] * kernel[i];
    }        

    if (isPositionInBox)
    {
        vec3 cloudColor = vec3(2.0f*cloudAlpha);
        FragColor = vec4(cloudColor + color * (1-cloudAlpha), 1);
    }
    else
    {
        if (cloudDepth >= backgndDepth)
        {   
            // 배경이 구름 앞쪽에 있음.
            FragColor = vec4(color, 1);
        }
        else
        {
            // 구름이 배경 앞쪽에 있어
            vec3 cloudColor = vec3(2.0f*cloudAlpha);
            FragColor = vec4(cloudColor + color * (1-cloudAlpha), 1);
        }
    }
}