#include <glad/glad.h>
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>
#include <glm/gtc/type_ptr.hpp>
#include <fstream>
#include <sstream>
#include <algorithm>
#include "core/mesh.h"
#include "core/assimpLoader.h"
#include "core/texture.h"
#include "core/RenderPass.h"

//#define MAC_CLION
#define VSTUDIO

#ifdef MAC_CLION
#include "imgui.h"
#include "backends/imgui_impl_glfw.h"
#include "backends/imgui_impl_opengl3.h"
#endif

#ifdef VSTUDIO
// Note: install imgui with:
//     ./vcpkg.exe install imgui[glfw-binding,opengl3-binding]
#include <imgui.h>
#include <imgui_impl_glfw.h>
#include <imgui_impl_opengl3.h>
#endif

int g_width = 1920;
int g_height = 1080;

void processInput(GLFWwindow *window) {
    if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
        glfwSetWindowShouldClose(window, true);
}

void framebufferSizeCallback(GLFWwindow *window,
                             int width, int height) {
    g_width = width;
    g_height = height;
    glViewport(0, 0, width, height);
}

std::string readFileToString(const std::string &filePath) {
    std::ifstream fileStream(filePath, std::ios::in);
    if (!fileStream.is_open()) {
        printf("Could not open file: %s\n", filePath.c_str());
        return "";
    }
    std::stringstream buffer;
    buffer << fileStream.rdbuf();
    return buffer.str();
}

GLuint generateShader(const std::string &shaderPath, GLuint shaderType) {
    printf("Loading shader: %s\n", shaderPath.c_str());
    const std::string shaderText = readFileToString(shaderPath);
    const GLuint shader = glCreateShader(shaderType);
    const char *s_str = shaderText.c_str();
    glShaderSource(shader, 1, &s_str, nullptr);
    glCompileShader(shader);
    GLint success = 0;
    glGetShaderiv(shader, GL_COMPILE_STATUS, &success);
    if (!success) {
        char infoLog[512];
        glGetShaderInfoLog(shader, 512, NULL, infoLog);
        printf("Error! Shader issue [%s]: %s\n", shaderPath.c_str(), infoLog);
    }
    return shader;
}

void deactivateRenderPasses() {
    glBindFramebuffer(GL_FRAMEBUFFER, 0);
    glClearColor(1.0f , 1.0f , 1.0f , 1.0f);
    glClear(GL_COLOR_BUFFER_BIT);
}


void SetUniform(const unsigned int shaderProgram, const char *uniformName, const float x, const float y, const float z) {
    glUniform3f(glGetUniformLocation(shaderProgram, uniformName), x, y, z);
}

void SetUniform(const unsigned int shaderProgram, const char *uniformName, const glm::vec3 vec) {
    SetUniform(shaderProgram, uniformName, vec.x, vec.y, vec.z);
}

void SetUniform(const unsigned int shaderProgram, const char *uniformName, const float values[3]) {
    SetUniform(shaderProgram, uniformName, values[0], values[1], values[2]);
}

void SetUniform(const unsigned int shaderProgram, const char *uniformName, const float value) {
    glUniform1f(glGetUniformLocation(shaderProgram, uniformName), value);
}

void SetUniform(const unsigned int shaderProgram, const char *uniformName, const glm::mat4 matrix) {
    glUniformMatrix4fv(glGetUniformLocation(shaderProgram, uniformName), 1, GL_FALSE, glm::value_ptr(matrix));
}


int main() {
    glfwInit();
    glfwWindowHint(GLFW_SAMPLES, 4);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
#ifdef __APPLE__
    glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);
#endif

    GLFWwindow *window = glfwCreateWindow(g_width, g_height, "Volume Rendering", NULL, NULL);
    if (window == NULL) {
        printf("Failed to create GLFW window\n");
        glfwTerminate();
        return -1;
    }
    glfwMakeContextCurrent(window);

    glfwSetFramebufferSizeCallback(window, framebufferSizeCallback);

    if (!gladLoadGLLoader((GLADloadproc) glfwGetProcAddress)) {
        printf("Failed to initialize GLAD\n");
        return -1;
    }

    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGuiIO &io = ImGui::GetIO();
    io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;

    //Setup platforms
    ImGui_ImplGlfw_InitForOpenGL(window, true);
    ImGui_ImplOpenGL3_Init("#version 400");

    glEnable(GL_DEPTH_TEST);
    glFrontFace(GL_CCW);
    glEnable(GL_CULL_FACE);
    glCullFace(GL_BACK);
    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

    const GLuint modelVertexShader = generateShader("shaders/modelVertex.vs", GL_VERTEX_SHADER);
    const GLuint fragmentShader = generateShader("shaders/fragment.fs", GL_FRAGMENT_SHADER);

    const GLuint postProcessingVertexShader = generateShader("shaders/postProcessing.vs", GL_VERTEX_SHADER);
    const GLuint postProcessingFragmentShader = generateShader("shaders/postProcessing.fs", GL_FRAGMENT_SHADER);



    int success;
    char infoLog[512];
    const unsigned int modelShaderProgram = glCreateProgram();
    glAttachShader(modelShaderProgram, modelVertexShader);
    glAttachShader(modelShaderProgram, fragmentShader);
    glLinkProgram(modelShaderProgram);
    glGetProgramiv(modelShaderProgram, GL_LINK_STATUS, &success);
    if (!success) {
        glGetProgramInfoLog(modelShaderProgram, 512, NULL, infoLog);
        printf("Error! Making Shader Program: %s\n", infoLog);
    }
    const unsigned int postProcessingShaderProgram = glCreateProgram();
    glAttachShader(postProcessingShaderProgram, postProcessingVertexShader);
    glAttachShader(postProcessingShaderProgram, postProcessingFragmentShader);
    glLinkProgram(postProcessingShaderProgram);
    glGetProgramiv(postProcessingShaderProgram, GL_LINK_STATUS, &success);
    if (!success) {
        glGetProgramInfoLog(postProcessingShaderProgram, 512, NULL, infoLog);
        printf("Error! Making Shader Program: %s\n", infoLog);
    }

    glDeleteShader(modelVertexShader);
    glDeleteShader(fragmentShader);
    glDeleteShader(postProcessingVertexShader);
    glDeleteShader(postProcessingFragmentShader);

    core::RenderPass volumetricPass = core::RenderPass(glm::ivec2(g_width, g_height));

    core::Model suzanne = core::AssimpLoader::loadModel("models/nonormalmonkey.obj");
    suzanne.translate(glm::vec3(0.0f, 0.0f, -3.0f));
    suzanne.scale(glm::vec3(1.5, 1.5, 1.5));

    glm::vec4 clearColor = glm::vec4(0.2f, 0.2f, 0.2f, 1.0f);
    glClearColor(clearColor.r,
                 clearColor.g, clearColor.b, clearColor.a);

    glm::vec3 cameraPos = glm::vec3(0.0f, 0.0f, 10.0f);
    glm::vec3 cameraTarget = glm::vec3(0.0f, 0.0f, 0.0f);
    glm::vec3 cameraDirection = glm::normalize(cameraPos - cameraTarget);
    glm::vec3 up = glm::vec3(0.0f, 1.0f, 0.0f);
    glm::vec3 cameraRight = glm::normalize(glm::cross(up, cameraDirection));
    glm::vec3 cameraUp = glm::cross(cameraDirection, cameraRight);

    //VP
    glm::mat4 view = glm::lookAt(cameraPos, cameraTarget, cameraUp);
    glm::mat4 projection = glm::perspective(glm::radians(45.0f), static_cast<float>(g_width) / static_cast<float>(g_height), 0.1f, 100.0f);

    double currentTime = glfwGetTime();
    double finishFrameTime = 0.0;
    float deltaTime = 0.0f;
    float rotationStrength = 100.0f;

    float lightHorizontalAngle = glm::radians(260.0f);
    float lightVerticalAngle = 0.0;
    float lightColor[3] = { 1.0f, 1.0f, 1.0f };

    float volumePosition[3] = { 0.0f, 0.0f, 0.0f };
    float volumeRadius = 4.0f;
    float volumeAbsorptionCoefficient = 0.2f;
    float volumeScatteringCoefficient = 0.8f;
    float volumeSpeed = 5.0f;

    while (!glfwWindowShouldClose(window)) {
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);


        ImGui_ImplOpenGL3_NewFrame();
        ImGui_ImplGlfw_NewFrame();
        ImGui::NewFrame();
        ImGui::Begin("Light Settings");
        ImGui::SliderAngle("Light Horizontal Angle", &lightHorizontalAngle, 0.0f, 360.0f);
        ImGui::SliderAngle("Light Vertical Angle", &lightVerticalAngle, -90.0f, 90.0f);
        glm::vec3 lightDirection = glm::vec3(
            cos(lightVerticalAngle) * cos(lightHorizontalAngle),
            sin(lightVerticalAngle),
            cos(lightVerticalAngle) * sin(lightHorizontalAngle)
        );
        ImGui::Text("Direction: %.2f, %.2f, %.2f", lightDirection.x, lightDirection.y, lightDirection.z);
        ImGui::ColorPicker3("Light Color", lightColor);
        ImGui::End();

        ImGui::Begin("Volume Settings");
        ImGui::SliderFloat3("Position", &volumePosition[0], -10.0f, 10.0f);
        ImGui::SliderFloat("Radius", &volumeRadius, 0.1f, 10.0f);
        ImGui::SliderFloat("Absorption Coefficient", &volumeAbsorptionCoefficient, 0.0f, 1.0f);
        ImGui::SliderFloat("Scattering Coefficient", &volumeScatteringCoefficient, 0.0f, 1.0f);
        ImGui::SliderFloat("Speed", &volumeSpeed, 0.0f, 10.0f);
        ImGui::End();


        processInput(window);

        volumetricPass.activate();


        suzanne.rotate(glm::vec3(0.0f, 1.0f, 0.0f), glm::radians(rotationStrength) * static_cast<float>(deltaTime));

        glUseProgram(modelShaderProgram);
        SetUniform(modelShaderProgram, "mvpMatrix", projection * view * suzanne.getModelMatrix());
        suzanne.render();
        glBindVertexArray(0);

        deactivateRenderPasses();


        //Set camera variables in shader
        glUseProgram(postProcessingShaderProgram);
        glm::mat4x4 inv_projView = glm::inverse(projection * view);
        SetUniform(postProcessingShaderProgram, "invProjView", inv_projView);
        SetUniform(postProcessingShaderProgram, "cameraPos", cameraPos);
        SetUniform(postProcessingShaderProgram, "cameraDir", cameraDirection);

        //Set light variables
        SetUniform(postProcessingShaderProgram, "light.direction", lightDirection);
        SetUniform(postProcessingShaderProgram, "light.color", lightColor);

        //Set volume variables
        SetUniform(postProcessingShaderProgram, "volume.position", volumePosition);
        SetUniform(postProcessingShaderProgram, "volume.radius", volumeRadius);
        SetUniform(postProcessingShaderProgram, "volume.sigma_a", volumeAbsorptionCoefficient);
        SetUniform(postProcessingShaderProgram, "volume.sigma_s", volumeScatteringCoefficient);
        SetUniform(postProcessingShaderProgram, "volume.speed", volumeSpeed);

        SetUniform(postProcessingShaderProgram, "time", static_cast<float>(glfwGetTime()));

        volumetricPass.render();

        glBindVertexArray(0);
        glActiveTexture(GL_TEXTURE0);

        ImGui::Render();
        ImGui_ImplOpenGL3_RenderDrawData(ImGui::GetDrawData());

        glfwSwapBuffers(window);
        glfwPollEvents();
        finishFrameTime = glfwGetTime();
        deltaTime = static_cast<float>(finishFrameTime - currentTime);
        currentTime = finishFrameTime;

    }

    glDeleteProgram(modelShaderProgram);
    ImGui_ImplOpenGL3_Shutdown();
    ImGui_ImplGlfw_Shutdown();
    ImGui::DestroyContext();

    glfwTerminate();
    return 0;
}