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

struct Grid{
    int dimension;
    vec3 boundsMin;
    vec3 boundsMax;
};

uniform Volume volume;

//PRNG function found on the internet
float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

bool RayBoxIntersect(vec3 rayOrigin, vec3 rayDirection, vec3 boundsMin, vec3 boundsMax, out float tMin, out float tMax){
    vec3 invDir = 1.0 / rayDirection;
    vec3 t0 = (boundsMin - rayOrigin) * invDir;
    vec3 t1 = (boundsMax - rayOrigin) * invDir;

    vec3 tNear = min(t0, t1);
    vec3 tFar = max(t0, t1);

    tMin = max(max(tNear.x, tNear.y), tNear.z);
    tMax = min(min(tFar.x, tFar.y), tFar.z);

    return tMax >= tMin && tMax >= 0.0;
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

int[3] GetVoxel(Grid grid, vec3 worldPosition)
{
    vec3 size = grid.boundsMax - grid.boundsMin;
    vec3 pLocal = (worldPosition - grid.boundsMin) / size;
    vec3 pVoxel = pLocal * grid.dimension;

    int xi = int(floor(pVoxel.x));
    int yi = int(floor(pVoxel.y));
    int zi = int(floor(pVoxel.z));

    if(max(max(xi, yi), zi) > grid.dimension - 1) return int[3](-1, -1, -1);

    return int[3](xi, yi, zi);
}

float sampleDensity(Grid grid, vec3 pos)
{
    int voxel[3] = GetVoxel(grid, pos);
    return length(vec3(voxel[0], voxel[1], voxel[2]));
}

vec3 traceScene(vec3 rayOrigin, vec3 rayDirection)
{
    float t0, t1;
//     vec3 bgColor = texture(screenTexture, TexCoords.st).rgb;
    vec3 bgColor = normalize(vec3(0.572, 0.772, 0.921));

    Grid grid;
    grid.boundsMin = vec3(-1, -1, -1) * volume.radius + volume.position;
    grid.boundsMax = vec3(1, 1, 1) * volume.radius + volume.position;
    grid.dimension = 128;

    if(RayBoxIntersect(rayOrigin, rayDirection, grid.boundsMin, grid.boundsMax, t0, t1)){
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

            float density = sampleDensity(grid, sample_pos);

            float sample_transparency = exp(-step_size * density * sigma_t);
            transparency *= sample_transparency;


            float lgt_t0, lgt_t1;
            RayBoxIntersect(sample_pos, -light.direction, grid.boundsMin, grid.boundsMax, lgt_t0, lgt_t1);
            if(lgt_t1 > 0.0) //Inside circle
            {
                int numStepsLight = int(ceil(lgt_t1 / step_size));
                float strideLight = lgt_t1 / numStepsLight;
                float tau = 0;

                for(int j = 0; j < numStepsLight; j++)
                {
                    float tLight = strideLight * (j + 0.5);
                    vec3 lightSamplePos = sample_pos - light.direction * tLight;
                    tau += sampleDensity(grid, lightSamplePos);
                }

                float light_att = exp(-tau * strideLight * sigma_t);
                float cos_theta = dot(rayDirection, light.direction);

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