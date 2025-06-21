#version 430

// thank to https://github.com/m-pilia/volume-raycasting/blob/master/shaders/alpha_blending.frag
// lecture https://martinopilia.com/posts/2018/09/17/volume-raycasting.html
// data file https://www.cg.tuwien.ac.at/research/publications/2002/dataset-christmastree/
// reference http://www.real-time-volume-graphics.org/

in vec3 pos;
out vec4 a_colour;

uniform mat4 model;
uniform mat4 proj;
uniform mat4 view;

uniform vec3 ray_origin;

uniform vec2 viewport_size;
uniform float focal_length;
uniform float aspect_ratio;
uniform float gamma;
uniform float step_length;

uniform int octaves;
uniform float amplitude;
uniform float frequency;
uniform float persistence;
uniform vec3 boundSize;

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

// A very simple colour transfer function
vec4 colour_transfer(float intensity)
{
    vec3 high = vec3(1.0, 1.0, 1.0);
    vec3 low = vec3(0.0, 0.0, 0.0);
    float alpha = (exp(intensity) - 1.0) / (exp(1.0) - 1.0);
    return vec4(intensity * high + (1.0 - intensity) * low, alpha);
}

// Precision-adjusted variations of https://www.shadertoy.com/view/4djSRW
float hash(float p) { p = fract(p * 0.011); p *= p + 7.5; p *= p + p; return fract(p); }
float hash(vec2 p) {vec3 p3 = fract(vec3(p.xyx) * 0.13); p3 += dot(p3, p3.yzx + 3.333); return fract((p3.x + p3.y) * p3.z); }


float noise(vec3 x) {
    const vec3 step = vec3(110, 241, 171);

    vec3 i = floor(x);
    vec3 f = fract(x);
 
    // For performance, compute the base input to a 1D hash from the integer part of the argument and the 
    // incremental change to the 1D based on the 3D -> 1D wrapping
    float n = dot(i, step);

    vec3 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(mix( hash(n + dot(step, vec3(0, 0, 0))), hash(n + dot(step, vec3(1, 0, 0))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 0))), hash(n + dot(step, vec3(1, 1, 0))), u.x), u.y),
               mix(mix( hash(n + dot(step, vec3(0, 0, 1))), hash(n + dot(step, vec3(1, 0, 1))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 1))), hash(n + dot(step, vec3(1, 1, 1))), u.x), u.y), u.z);
}

float fbm(vec3 x) {
	float v = 0.0;
	float a = 0.5;
	vec3 shift = vec3(100);
	for (int i = 0; i < octaves; i++) 
	{
		v += a * noise(x);
		x = x * 2.0 + shift;
		a *= 0.5;
	}
	return v;
}


float random(in vec2 st)
{
	return 2.0f * fract(sin(dot(st.xy, vec2(12.9898,78.233)))*43758.5453123) - 1.5f;
}


float noise(in vec2 st)
{
	vec2 i = floor(st);
	vec2 f = fract(st);

	float a = random(i);
	float b = random(i + vec2(1,0));
	float c = random(i + vec2(0,1));
	float d = random(i + vec2(1,1));

	vec2 u = f * f * (3.0 - 2.0 * f);

	return mix(a, b, u.x) + (c-a)*u.y * (1.0f-u.x) + (d-b)*u.x*u.y;
}

float fbm(in vec2 st)
{
	float total = 0.0f;
	float amp = amplitude;
	float fre = frequency;
	for(int i=0; i<octaves; i++)
	{
		total += amp * abs(noise(st * fre));
		fre *= 2;
		amp *= persistence;
	}
	return total;
}

void main()
{
	vec3 top = vec3(1, 1, 1);
	vec3 bottom= vec3(-1, -1, -1);
	vec3 background_colour = vec3(0, 0, 0);

	// Ray march 
	vec3 ray_direction;
	ray_direction.xy = (2.0f * gl_FragCoord.xy / viewport_size) - 1.0f; // screen-space coordinate
	ray_direction.x *= aspect_ratio;
	ray_direction.z = focal_length; // view-space coordinate
	ray_direction = (vec4(ray_direction, 1) * view).xyz; // p^T = r^T * V

	float t_0, t_1;
	Ray casting_ray = Ray(ray_origin, ray_direction);
	AABB bounding_box = AABB(top, bottom);
	ray_box_intersection(casting_ray, bounding_box, t_0, t_1);

	vec3 ray_start = (ray_origin + ray_direction * t_0 - bottom) / (top - bottom);
	vec3 ray_stop = (ray_origin + ray_direction * t_1 - bottom) / (top - bottom);

	vec3 ray = ray_stop - ray_start;
	float ray_length = length(ray);
    vec3 step_vector = step_length * ray / ray_length;

	vec3 position = ray_start;
    vec4 colour = vec4(0.0);

    // Ray march until reaching the end of the volume, or colour saturation
    while (ray_length > 0 && colour.a < 1.0) {

        float intensity = 0.003f * fbm(2.0f * position);//texture(volume, position).r;

        vec4 c = colour_transfer(intensity);

        // Alpha-blending
        colour.rgb += c.a * c.rgb + (1 - c.a) * colour.a * colour.rgb;
        colour.a = c.a + (1 - c.a) * colour.a;

        ray_length -= step_length;
        position += step_vector;
    }

	// Blend background
    colour.rgb = colour.rgb + (1 - colour.a) * pow(background_colour, vec3(gamma)).rgb;
    colour.a = 1.0;

    // Gamma correction
    a_colour.rgb = pow(colour.rgb, vec3(1.0 / gamma));
    a_colour.a = colour.a;

}

