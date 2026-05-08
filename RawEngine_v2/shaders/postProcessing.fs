#version 400 core
out vec4 FragColor;

#define PI 3.1415926538

uniform vec3 cameraPos;
uniform vec3 cameraDir;
uniform mat4 invProjView;

uniform float time;

in vec2 TexCoords;

uniform sampler2D screenTexture;

struct Light{
    vec3 direction;
    vec3 color;
};

uniform Light light;

struct Volume{
    vec3 position;
    float radius;
    float sigma_a;
    float sigma_s;
    float density;
    float speed;
};

uniform Volume volume;

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

//The following functions are used in perlin-noise generation, as this is not the focus of this project I simply copied them
//from the scratchapixel repo: https://github.com/scratchapixel/scratchapixel-code/blob/main/volume-rendering-for-developers/raymarch-chap4.cpp
//the functions have also been improved using AI in order to remove repetition in the noise
const int p[512] = int[512](
151, 160, 137,  91,  90,  15, 131,  13, 201,  95,  96,  53, 194, 233,   7, 225,
140,  36, 103,  30,  69, 142,   8,  99,  37, 240,  21,  10,  23, 190,   6, 148,
247, 120, 234,  75,   0,  26, 197,  62,  94, 252, 219, 203, 117,  35,  11,  32,
57, 177,  33,  88, 237, 149,  56,  87, 174,  20, 125, 136, 171, 168,  68, 175,
74, 165,  71, 134, 139,  48,  27, 166,  77, 146, 158, 231,  83, 111, 229, 122,
60, 211, 133, 230, 220, 105,  92,  41,  55,  46, 245,  40, 244, 102, 143,  54,
65,  25,  63, 161,   1, 216,  80,  73, 209,  76, 132, 187, 208,  89,  18, 169,
200, 196, 135, 130, 116, 188, 159,  86, 164, 100, 109, 198, 173, 186,   3,  64,
52, 217, 226, 250, 124, 123,   5, 202,  38, 147, 118, 126, 255,  82,  85, 212,
207, 206,  59, 227,  47,  16,  58,  17, 182, 189,  28,  42, 223, 183, 170, 213,
119, 248, 152,   2,  44, 154, 163,  70, 221, 153, 101, 155, 167,  43, 172,   9,
129,  22,  39, 253,  19,  98, 108, 110,  79, 113, 224, 232, 178, 185, 112, 104,
218, 246,  97, 228, 251,  34, 242, 193, 238, 210, 144,  12, 191, 179, 162, 241,
81,  51, 145, 235, 249,  14, 239, 107,  49, 192, 214,  31, 181, 199, 106, 157,
184,  84, 204, 176, 115, 121,  50,  45, 127,   4, 150, 254, 138, 236, 205,  93,
222, 114,  67,  29,  24,  72, 243, 141, 128, 195,  78,  66, 215,  61, 156, 180,
// repeat:
151, 160, 137,  91,  90,  15, 131,  13, 201,  95,  96,  53, 194, 233,   7, 225,
140,  36, 103,  30,  69, 142,   8,  99,  37, 240,  21,  10,  23, 190,   6, 148,
247, 120, 234,  75,   0,  26, 197,  62,  94, 252, 219, 203, 117,  35,  11,  32,
57, 177,  33,  88, 237, 149,  56,  87, 174,  20, 125, 136, 171, 168,  68, 175,
74, 165,  71, 134, 139,  48,  27, 166,  77, 146, 158, 231,  83, 111, 229, 122,
60, 211, 133, 230, 220, 105,  92,  41,  55,  46, 245,  40, 244, 102, 143,  54,
65,  25,  63, 161,   1, 216,  80,  73, 209,  76, 132, 187, 208,  89,  18, 169,
200, 196, 135, 130, 116, 188, 159,  86, 164, 100, 109, 198, 173, 186,   3,  64,
52, 217, 226, 250, 124, 123,   5, 202,  38, 147, 118, 126, 255,  82,  85, 212,
207, 206,  59, 227,  47,  16,  58,  17, 182, 189,  28,  42, 223, 183, 170, 213,
119, 248, 152,   2,  44, 154, 163,  70, 221, 153, 101, 155, 167,  43, 172,   9,
129,  22,  39, 253,  19,  98, 108, 110,  79, 113, 224, 232, 178, 185, 112, 104,
218, 246,  97, 228, 251,  34, 242, 193, 238, 210, 144,  12, 191, 179, 162, 241,
81,  51, 145, 235, 249,  14, 239, 107,  49, 192, 214,  31, 181, 199, 106, 157,
184,  84, 204, 176, 115, 121,  50,  45, 127,   4, 150, 254, 138, 236, 205,  93,
222, 114,  67,  29,  24,  72, 243, 141, 128, 195,  78,  66, 215,  61, 156, 180
);

float fade(float t) { return t * t * t * (t * (t * 6 - 15) + 10); }
float lerp(float t, float a, float b) { return a + t * (b - a); }
float grad(int hash, float x, float y, float z)
{
    int h = hash & 15;
    float u = h<8 ? x : y,
    v = h<4 ? y : h==12||h==14 ? x : z;
    return ((h&1) == 0 ? u : -u) + ((h&2) == 0 ? v : -v);
}

float noise(float x, float y, float z)
{
    int X = int(floor(x)) & 255,
    Y = int(floor(y)) & 255,
    Z = int(floor(z)) & 255;
    x -= floor(x);
    y -= floor(y);
    z -= floor(z);
    float u = fade(x),
    v = fade(y),
    w = fade(z);
    int A = p[X  ]+Y, AA = p[A]+Z, AB = p[A+1]+Z,
    B = p[X+1]+Y, BA = p[B]+Z, BB = p[B+1]+Z;

    return lerp(w, lerp(v, lerp(u, grad(p[AA  ], x  , y  , z   ),
    grad(p[BA  ], x-1, y  , z   )),
    lerp(u, grad(p[AB  ], x  , y-1, z   ),
    grad(p[BB  ], x-1, y-1, z   ))),
    lerp(v, lerp(u, grad(p[AA+1], x  , y  , z-1 ),
    grad(p[BA+1], x-1, y  , z-1 )),
    lerp(u, grad(p[AB+1], x  , y-1, z-1 ),
    grad(p[BB+1], x-1, y-1, z-1 ))));
}
float noise(vec3 p) {return noise(p.x, p.y, p.z);}

float smoothstep(float lo, float hi, float x)
{
    float t = clamp((x - lo) / (hi - lo), 0.0, 1.0);
    return t * t * (3.0 - (2.0 * t));
}

float eval_density(vec3 sample_pos, Volume volume)
{
    vec3 vp = sample_pos - volume.position;
    vec3 vp_xform;

    float theta = mod(time * volume.speed, 120.0) / 120.0 * 2.0 * PI;
    vp_xform.x = cos(theta) * vp.x + sin(theta) * vp.z;
    vp_xform.y = vp.y;
    vp_xform.z = -sin(theta) * vp.x + cos(theta) * vp.z;

    float dist = min(1.0, length(vp) / volume.radius);
    float falloff = smoothstep(0.8, 1.0, dist);

    float frequency = 1.0;
    vp_xform *= frequency;
    const int octaves = 5;
    float lacunarity = 2.0;
    float H = 0.4;
    float value = 0.0;

    for(int i = 0; i < octaves; i++)
    {
        value += noise(vp_xform) * pow(lacunarity, -H * i);
        vp_xform *= lacunarity;
    }

    return max(0.0, value) * (1.0 - falloff);
}

vec3 traceScene(vec3 rayOrigin, vec3 rayDirection)
{
    float t0, t1;
//     vec3 bgColor = texture(screenTexture, TexCoords.st).rgb;
    vec3 bgColor = normalize(vec3(0.572, 0.772, 0.921));

    if(raySphereIntersect(rayOrigin, rayDirection, volume, t0, t1)){

        float step_size = 0.5;
        int steps = int(ceil((t1 - t0) / step_size));
        step_size = (t1 - t0) / steps;

        float transparency = 1.0;

        vec3 result = vec3(0.0, 0.0, 0.0);

        float g = 0.6;

        float sigma_t = volume.sigma_a + volume.sigma_s;

        for(int i = 0; i < steps; ++i)
        {
            float t = t0 + step_size * (i + 0.5);
            vec3 sample_pos = rayOrigin + t * rayDirection;

            float density = eval_density(sample_pos, volume);

            float sample_transparency = exp(-step_size * density * sigma_t);
            transparency *= sample_transparency;


            float lgt_t0, lgt_t1;
            raySphereIntersect(sample_pos, -light.direction, volume, lgt_t0, lgt_t1);
            if(lgt_t1 > 0.0) //Inside circle
            {
                int numStepsLight = int(ceil(lgt_t1 / step_size));
                float strideLight = lgt_t1 / numStepsLight;
                float tau = 0;

                for(int j = 0; j < numStepsLight; j++)
                {
                    float tLight = strideLight * (j + 0.5);
                    vec3 lightSamplePos = sample_pos + light.direction * tLight;
                    tau += eval_density(lightSamplePos, volume);
                }

                float light_att = exp(-tau * strideLight * sigma_t);
                float cos_theta = dot(rayDirection, -light.direction);
                result += light.color * light_att * phase(cos_theta, g) * volume.sigma_s * transparency * step_size * volume.density;
            }

            if(transparency < 1e-3)
            {
                break;
            }
        }

        return bgColor * transparency + result;

    }else{
        return bgColor;
    }
}

void main()
{
    vec3 rayOrigin = cameraPos;
    vec3 rayDirection = GetPixelDirection();

    vec3 color = traceScene(rayOrigin, rayDirection);
    FragColor = vec4(color, 1.0);
}