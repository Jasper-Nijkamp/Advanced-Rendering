#version 330 core
out vec4 FragColor;

#define PI 3.1415926538

uniform vec3 cameraPos;
uniform vec3 cameraDir;
uniform mat4 invProjView;

in vec2 TexCoords;

uniform sampler2D screenTexture;

struct Light{
    vec3 direction;
    vec3 color;
    float intensity;
};

struct Volume{
    vec3 position;
    float radius;
    float sigma_a;
    float sigma_s;
    float density;
};

//PRNG function found on the internet
float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

bool raySphereIntersect(vec3 rayOrigin, vec3 rayDir, Volume sphere, out float t0, out float t1) {
    vec3 oc = rayOrigin - sphere.position;

    float a = dot(rayDir, rayDir);
    float b = 2.0 * dot(oc, rayDir);
    float c = dot(oc, oc) - sphere.radius * sphere.radius;

    float discriminant = b * b - 4.0 * a * c;

    if (discriminant < 0.0) {
        return false;
    }

    float sqrtDisc = sqrt(discriminant);
    t0 = (-b - sqrtDisc) / (2.0 * a);
    t1  = (-b + sqrtDisc) / (2.0 * a);

    return t0 > 0.0;
}

//AI-generated function to get the direction of a pixel using its uv-coorinate and camera position
vec3 GetPixelDirection(){
    // Convert UV [0,1] → NDC [-1,1]
    vec2 ndc = TexCoords * 2.0 - 1.0;

    // Unproject two points on the ray through this pixel
    vec4 nearPoint = invProjView * vec4(ndc, -1.0, 1.0);
    vec4 farPoint  = invProjView * vec4(ndc,  1.0, 1.0);

    // Perspective divide to get world positions
    vec3 near = nearPoint.xyz / nearPoint.w;
    vec3 far  = farPoint.xyz  / farPoint.w;

    return normalize(far - near);
}

float phase(float g, float cos_theta)
{
    float denom = 1 + g * g - 2 * g * cos_theta;
    return 1 / (4 * PI) * (1 - g * g) / (denom * sqrt(denom));
}

vec3 traceScene(vec3 rayOrigin, vec3 rayDirection, Volume sphere, Light light)
{
    float t0, t1;
//     vec3 bgColor = texture(screenTexture, TexCoords.st).rgb;
    vec3 bgColor = normalize(vec3(0.572, 0.772, 0.921));

    if(raySphereIntersect(rayOrigin, rayDirection, sphere, t0, t1)){

        float step_size = 0.2;
        int steps = int(ceil((t1 - t0) / step_size));
        step_size = (t1 - t0) / steps;

        float transparency = 1.0;
        int d = 2;

        vec3 result = vec3(0.0, 0.0, 0.0);

        float g = 0.8;

        for(int i = 0; i < steps; ++i)
        {
            float t = t0 + step_size * (i + rand(TexCoords.st));
            vec3 sample_pos = rayOrigin + t * rayDirection;

            float sample_attenuation = exp(-step_size * sphere.density * (sphere.sigma_a + sphere.sigma_s));
            transparency *= sample_attenuation;

            if(transparency < 1e-3)
            {
                if(rand(TexCoords) > 1.0 / d) break;
                else transparency *= d;
            }

            float lgt_t0, lgt_t1;
            raySphereIntersect(sample_pos, -light.direction, sphere, lgt_t0, lgt_t1);
            if(lgt_t1 > 0.0)
            {
                float cos_theta = dot(rayDirection, light.direction);
                float light_attenuation = exp(-sphere.density * lgt_t1 * (sphere.sigma_a + sphere.sigma_s));
                result += sphere.density * sphere.sigma_s * phase(g, cos_theta) * light_attenuation * light.color * step_size;
            }
        }

        return bgColor * transparency + result;

    }else{
        return bgColor;
    }
}

void main()
{
    Volume sphere;
    sphere.position = vec3(0.0, 0.0, 0.0);
    sphere.radius = 1.0;
    sphere.sigma_a = 0.1;
    sphere.sigma_s = 0.5;
    sphere.density = 1;

    Light light;
    light.direction = normalize(vec3(0.0, 0.0, 1.0));
    light.color = normalize(vec3(1.0, 1.0, 1.0));

    vec3 rayOrigin = cameraPos;
    vec3 rayDirection = GetPixelDirection();

    vec3 color = traceScene(rayOrigin, rayDirection, sphere, light);
    FragColor = vec4(color, 1.0);

    return;
}