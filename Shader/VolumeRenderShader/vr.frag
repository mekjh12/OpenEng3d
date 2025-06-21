#version 420 core

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
uniform sampler3D volume;
uniform vec2 viewport_size;
uniform float focal_length;
uniform float aspect_ratio;
uniform float gamma;
uniform float step_length;

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

void main()
{
	vec3 top = vec3(1, 1, 1);
	vec3 bottom= vec3(-1, -1, -1);
	vec3 background_colour = vec3(0.8f,0.8f,0.1f);

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

        float intensity = texture(volume, position).r;

        vec4 c = colour_transfer(intensity);

        // Alpha-blending
        colour.rgb += c.rgb;//.a * c.rgb + (1 - c.a) * colour.a * colour.rgb;
        //colour.a = c.a + (1 - c.a) * colour.a;

        ray_length -= step_length;
        position += step_vector;
    }

	// Blend background
    colour.rgb = colour.rgb + (1 - colour.a) * pow(background_colour, vec3(gamma)).rgb;
    colour.a = 1.0;

    // Gamma correction
    a_colour.rgb = pow(colour.rgb, vec3(1.0 / gamma));
    a_colour.a = colour.a;

	//colour.rgb *= (1000.0f * step_length);
	//colour.a = 1.0f;
	//a_colour = colour;
}

