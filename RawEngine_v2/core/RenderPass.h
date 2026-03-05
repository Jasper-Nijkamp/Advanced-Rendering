#ifndef RAWENGINE_RENDERPASS_H
#define RAWENGINE_RENDERPASS_H
#include "mesh.h"

namespace core {
    class RenderPass {
    private:
        GLuint VBO;
        GLuint EBO;
        GLuint VAO;
        std::vector<GLuint> indices;


    public:
        RenderPass();
        void render();

    private:
        void setupBuffers();
    };


}
#endif //RAWENGINE_RENDERPASS_H