#version 430
uniform mat4 view;

uniform vec3 ray_origin;
uniform vec2 viewport_size;
uniform float focal_length;	
uniform float aspect_ratio;

uniform int octaves;
uniform float amplitude;
uniform float frequency;
uniform float persistence;
uniform bool isInBox;
uniform vec2 seed;
uniform vec3 boundSize;
uniform vec3 centerPosition;

out vec4 fragColor;

//#include<./../common/boxIntersectFuncs.glsl>
struct Ray { vec3 origin; vec3 direction;};
struct AABB {vec3 top; 	vec3 bottom;};
void ray_box_intersection(Ray ray, AABB box, out float t_0, out float t_1);

//#include <./../common/noiseFuncs.glsl>
float hash(float p);
float hash(vec2 p);
float noise(vec3 x);
float fbm(vec3 x, int octaves);
float random(in vec2 st);
float noise(in vec2 st);
float fbm(in vec2 st, in float amplitude, in float persistence, in float frequency, in int octaves);


//-----------------------------------------------------------------------------
// Main
//-----------------------------------------------------------------------------
void main()
{
	vec3 top = + boundSize;
	vec3 bottom = - boundSize;

	// Ray march 
	vec3 ray_direction;
	ray_direction.xy = (2.0f * gl_FragCoord.xy / viewport_size) - 1.0f; // screen-space coordinate
	ray_direction.x *= aspect_ratio;
	ray_direction.z = focal_length;										// view-space coordinate
	ray_direction = (vec4(ray_direction, 1) * view).xyz;				// p^T = r^T * V

	float t_0, t_1;														// 교차점을 찾는다.
	Ray casting_ray = Ray(ray_origin, ray_direction);					
	AABB bounding_box = AABB(top, bottom);
	ray_box_intersection(casting_ray, bounding_box, t_0, t_1);
	vec3 ray_start = (ray_origin + ray_direction * t_0 - bottom) / (top - bottom);
	vec3 ray_stop =  (ray_origin + ray_direction * t_1 - bottom) / (top - bottom);

	if (isInBox)
	{
		//ray_start = (ray_origin - bottom) / (top - bottom);
	}
	
	float step_length = 0.05f;
	vec3 ray = ray_stop - ray_start;
	float ray_length = length(ray);
    vec3 step_vector = step_length * ray / ray_length;
		
	vec3 position = ray_start;

    vec4 colour = vec4(0.0);
	
	float T = 1.0f;
	float absorption = 100.;
	vec4 color=vec4(.0);
	int num = 0;

    // Ray march until reaching the end of the volume, or colour saturation
    while (ray_length > 0 && num < 10) 
	{
		float density = noise(8.0f * position - vec3(8));
		
		if(density>0.)
		{
			float tmp = density / float(64);
			T *= 1. -tmp * absorption;
			if( T <= 0.01) break;
			color += vec4(1.)*80.*tmp*T;				
		}

        ray_length -= step_length;
        position += step_vector;
		num++;
    }
	
	if (color.a < 0.001f) discard;

	fragColor = color;

	return;

}

