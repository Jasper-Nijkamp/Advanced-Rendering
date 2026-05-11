#version 400 core
out vec4 FragColor;

#define PI 3.1415926538

uniform vec3 cameraPos;
uniform vec3 cameraDir;
uniform mat4 invProjView;

uniform sampler3D volumeTexture;

uniform float time;

in vec2 TexCoords;

uniform sampler2D screenTexture;

uniform float stepSizeFactor;

struct Light{
    vec3 direction;
    vec3 color;
};

uniform Light light;

struct Volume{
    vec3 position;
    float sigma_a;
    float sigma_s;
};

uniform Volume volume;

struct Grid{
    int resolution;
    vec3 boundsMin;
    vec3 boundsMax;
};

uniform Grid grid;

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

float phase(float g, float cosTheta)
{
    float denom = 1.0 + g * g - 2 * g * cosTheta;
    return 1.0 / (4.0 * PI) * (1.0 - g * g) / (denom * sqrt(denom));
}

float sampleDensity(vec3 worldPos)
{
    vec3 size = grid.boundsMax - grid.boundsMin;
    vec3 pLocal = (worldPos - grid.boundsMin) / size;

    return texture(volumeTexture, pLocal).r;
}

vec3 traceScene(vec3 rayOrigin, vec3 rayDirection)
{
//     vec3 bgColor = texture(screenTexture, TexCoords.st).rgb;
    vec3 bgColor = normalize(vec3(0.572, 0.772, 0.921));

    float t0, t1;
    if(RayBoxIntersect(rayOrigin, rayDirection, grid.boundsMin, grid.boundsMax, t0, t1)){
        float volumeSize = grid.boundsMax.x - grid.boundsMin.x;
        float voxelSize = volumeSize / grid.resolution;
        float stepSize = voxelSize * stepSizeFactor;

        int numSteps = int(ceil((t1 - t0) / stepSize));
        stepSize = (t1 - t0) / numSteps;

        float transparency = 1.0;
        vec3 result = vec3(0.0, 0.0, 0.0);

        float g = 0.6;
        float sigma_t = volume.sigma_a + volume.sigma_s;

        for(int i = 0; i < numSteps; ++i)
        {
            float t = t0 + stepSize * (i + 0.5);
            vec3 samplePos = rayOrigin + t * rayDirection;

            float density = sampleDensity(samplePos);

            float sampleTransparency = exp(-stepSize * density * sigma_t);
            transparency *= sampleTransparency;

            float lgt_t0, lgt_t1;
            RayBoxIntersect(samplePos, -light.direction, grid.boundsMin, grid.boundsMax, lgt_t0, lgt_t1);
            if(lgt_t1 > 0.0) //Inside volume
            {
                int numStepsLight = int(ceil(lgt_t1 / stepSize));
                float strideLight = lgt_t1 / numStepsLight;
                float tau = 0;

                for(int j = 0; j < numStepsLight; j++)
                {
                    float tLight = strideLight * (j + 0.5);
                    vec3 lightSamplePos = samplePos - light.direction * tLight;
                    tau += sampleDensity(lightSamplePos);
                }

                float lightAttenuation = exp(-tau * strideLight * sigma_t);
                float cosTheta = dot(rayDirection, light.direction);

                result += light.color * lightAttenuation * phase(cosTheta, g) * volume.sigma_s * transparency * stepSize * density;
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