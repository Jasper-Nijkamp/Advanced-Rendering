#ifndef RAWENGINE_PERLIN_H
#define RAWENGINE_PERLIN_H
#define STB_PERLIN_IMPLEMENTATION

#include "stb_perlin.h" // https://github.com/nothings/stb
#include <vector>

std::vector<float> generateVoxelGrid(int size) {
    std::vector<float> grid(size * size * size);

    for (int z = 0; z < size; z++) {
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float u = x / (float)size;
                float v = y / (float)size;
                float w = z / (float)size;

                float noise = stb_perlin_noise3(u * 4.0, v * 4.0, w * 4.0, 0, 0, 0);
                noise = (noise + 1.0f) * 0.5f; //Map [-1, 1] to [0, 1]

                grid[x + y * size + z * size * size] = noise;
            }
        }
    }

    return grid;
}

#endif //RAWENGINE_PERLIN_H