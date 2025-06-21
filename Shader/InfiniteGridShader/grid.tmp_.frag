#version 420 core

in vec3 WorldPos;

layout(location = 0) out vec4 FragColor;

uniform vec3 gCameraFocusWorldPos;
uniform vec3 gCameraWorldPos;
uniform float gGridSize = 100.0;
uniform float gGridMinPixelsBetweenCells = 2.0;
uniform float gGridCellSize = 0.025;
uniform vec4 gGridColorThin = vec4(0.5, 0.5, 0.5, 1.0);
uniform vec4 gGridColorThick = vec4(0.0, 0.0, 0.0, 1.0);

uniform mat4 view;
uniform vec2 viewport_size;
uniform float aspect_ratio;
uniform float focal_length;

float log10(float x) { return log(x) / log(10.0); }
float satf(float x) { return clamp(x, 0.0, 1.0); }
vec2 satv(vec2 x) { return clamp(x, vec2(0.0), vec2(1.0)); }
float max2(vec2 v) { return max(v.x, v.y); }

void main()
{
    vec2 dvx = vec2(dFdx(WorldPos.x), dFdy(WorldPos.x));
    vec2 dvy = vec2(dFdx(WorldPos.y), dFdy(WorldPos.y));

    float lx = length(dvx);
    float ly = length(dvy);

    vec2 dudv = vec2(lx, ly);

    float l = length(dudv);

    float LOD = max(0.0, log10(l * gGridMinPixelsBetweenCells / gGridCellSize) + 1.0);

    float GridCellSizeLod0 = gGridCellSize * pow(10.0, floor(LOD));
    float GridCellSizeLod1 = GridCellSizeLod0 * 10.0;
    float GridCellSizeLod2 = GridCellSizeLod1 * 10.0;

    dudv *= 4.0;

    vec2 mod_div_dudv = mod(WorldPos.xy, GridCellSizeLod0) / dudv;
    float Lod0a = max2(vec2(1.0) - abs(satv(mod_div_dudv) * 2.0 - vec2(1.0)) );

    mod_div_dudv = mod(WorldPos.xy, GridCellSizeLod1) / dudv;
    float Lod1a = max2(vec2(1.0) - abs(satv(mod_div_dudv) * 2.0 - vec2(1.0)) );
    
    mod_div_dudv = mod(WorldPos.xy, GridCellSizeLod2) / dudv;
    float Lod2a = max2(vec2(1.0) - abs(satv(mod_div_dudv) * 2.0 - vec2(1.0)) );

    float LOD_fade = fract(LOD);
    vec4 Color;

    if (Lod2a > 0.0) {
        Color = gGridColorThick;
        Color.a *= Lod2a;
    } else {
        if (Lod1a > 0.0) {
            Color = mix(gGridColorThick, gGridColorThin, LOD_fade);
	        Color.a *= Lod1a;
        } else {
            Color = gGridColorThin;
	        Color.a *= (Lod0a * (1.0 - LOD_fade));
        }
    }
    
    float OpacityFalloff = (1.0 - satf(length(WorldPos.xy - gCameraFocusWorldPos.xy) / gGridSize));

    Color.a *= OpacityFalloff;

    FragColor = Color;

    // 좌표축을 그린다.
    float epsilon = length(gCameraWorldPos) * 0.001f;
    if ( WorldPos.x > 0 &&  abs(WorldPos.y) < epsilon && abs(WorldPos.z) < epsilon) FragColor = vec4(1, 0, 0, 1);
    if ( WorldPos.y > 0 &&abs(WorldPos.x) < epsilon && abs(WorldPos.z) < epsilon) FragColor = vec4(0, 1, 0, 1);

    vec3 d; // z축을 그린다.
	d.xy = (2.0f * gl_FragCoord.xy / viewport_size) - 1.0f; // screen-space coordinate
	d.x *= aspect_ratio;
	d.z = focal_length; // view-space coordinate
	d = (vec4(d, 1) * view).xyz; // p^T = r^T * V
    d = normalize(d);

    vec3 c = gCameraWorldPos;
    float t = 0.0f;
    if (abs(d.x)==0.01f)
    {
        t = (abs(d.y) < 0.01f) ? -c.z/d.z: -c.y/d.y;
    }
    else
    {
        t = -c.x/d.x;
    }

    vec3 p = c + d * t;
    if ( p.z > 0 &&abs(p.x) < epsilon && abs(p.y) < epsilon) FragColor = vec4(0, 0, 1, 1);

}
