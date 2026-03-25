#ifndef RAWENGINE_RENDERPASS_H
#define RAWENGINE_RENDERPASS_H
#include "mesh.h"

namespace core {
    class RenderPass {
    private:
        GLuint VBO;
        GLuint EBO;
        GLuint VAO;
        GLuint FBO;
        GLuint RBO;
        std::vector<GLuint> indices;

        GLuint texture;

        glm::vec2 textureSize;



    public:
        RenderPass(glm::ivec2);
        void render();
        void activate();

    private:
        void setupBuffers();
    };


}
#endif //RAWENGINE_RENDERPASS_H