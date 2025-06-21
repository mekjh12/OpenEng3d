#version 430

out vec4 fragColor;

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

//-----------------------------------------------------------------------------
// Ray Utils
//-----------------------------------------------------------------------------
struct Ray { vec3 origin; vec3 direction;};
struct AABB {vec3 top; 	vec3 bottom;};

// Slab method for ray-box intersection
void ray_box_intersection(Ray ray, AABB box, out float t_0, out float t_1)
{
	vec3 direction_inv = 1.0 / ray.direction;
	vec3 t_top = direction_inv * (box.top - ray.origin);
	vec3 t_bottom = direction_inv * (box.bottom - ray.origin);
	vec3 t_min = min(t_top, t_bottom);
	vec2 t = max(t_min.xx, t_min.yz);
	t_0 = max(0.0, max(t.x, t.y));
	vec3 t_max = max(t_top, t_bottom);
	t = min(t_max.xx, t_max.yz);
	t_1 = min(t.x, t.y);
}

//-----------------------------------------------------------------------------
// Maths Utils
//-----------------------------------------------------------------------------
float hash( float n ) { return fract(sin(n)*43758.5453); }

float noise( in vec3 x )
{
    vec3 p = floor(x);
    vec3 f = fract(x);

    f = f*f*(3.0-2.0*f); // 감마보정

    float n = p.x + p.y*seed.x + p.z*seed.y; // 59,113

    float res = mix(mix(mix( hash(n+  0.0), hash(n+  1.0),f.x),
                        mix( hash(n+ seed.x), hash(n+ seed.x + 1),f.x),f.y),
                    mix(mix( hash(n+ seed.y), hash(n+ seed.y + 1),f.x),
                        mix( hash(n+seed.x + seed.y), hash(n+seed.x + seed.y+1),f.x),f.y),f.z);
    return res;
}

float fbm( vec3 p )
{
    float f = 0.0f;
	float amp = amplitude;
	float total = 0.0f;
	float freq = frequency;

	for (int i=0; i<octaves; i++)
	{
		f += amp*noise(freq * p); 
		amp *= 0.5f;
		freq *= persistence;
	}
    return f;
}

float scene(vec3 p) { return .1-length(p)*.05+fbm(p*.3); }


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
		float density = scene(12.0f * position - vec3(6));
		
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

