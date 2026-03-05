#include "RenderPass.h"

namespace core {
    RenderPass::RenderPass() : VBO(), EBO(), VAO()
    {
        setupBuffers();
    }

    struct Vert {
        glm::vec2 pos;
        glm::vec2 uv;
    };

    void RenderPass::setupBuffers() {
        const Vert vertices[] = {
            { glm::vec2(-1.0f, -1.0f), glm::vec2(0, 0) },
            { glm::vec2( 1.0f, -1.0f), glm::vec2(1, 0) },
            { glm::vec2(-1.0f,  1.0f), glm::vec2(0, 1) },
            { glm::vec2( 1.0f, -1.0f), glm::vec2(1, 0) },
            { glm::vec2( 1.0f,  1.0f), glm::vec2(1, 1) },
            { glm::vec2(-1.0f,  1.0f), glm::vec2(0, 1) },
        };
        indices = {0, 1, 2, 3, 4, 5};

        glGenVertexArrays(1, &VAO);
        glGenBuffers(1, &VBO);
        glGenBuffers(1, &EBO);
        glBindVertexArray(VAO);
        glBindBuffer(GL_ARRAY_BUFFER, VBO);
        glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), &vertices[0],
                     GL_STATIC_DRAW);

        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
        glBufferData(GL_ELEMENT_ARRAY_BUFFER, static_cast<GLsizei>(sizeof(unsigned int) * indices.size()),
                     &indices[0], GL_STATIC_DRAW);

        glEnableVertexAttribArray(0);
        glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, sizeof(Vert), (void *) offsetof(Vert, pos));
        glEnableVertexAttribArray(1);
        glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, sizeof(Vert), (void *) offsetof(Vert, uv));
        glBindVertexArray(0);
    }

    void RenderPass::render() {
        glBindVertexArray(VAO);
        glDrawElements(GL_TRIANGLES, static_cast<GLsizei>(indices.size()), GL_UNSIGNED_INT, 0);
    }
}
