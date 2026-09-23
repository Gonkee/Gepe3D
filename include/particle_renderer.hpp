#pragma once

#define GLAD_GL_IMPLEMENTATION
#include <glad/gl.h>
#define GLFW_INCLUDE_NONE
#include <GLFW/glfw3.h>

#include <array>
#include <glm/vec3.hpp>
#include <glm/mat4x4.hpp>

class ParticleRenderer {
public:
    static ParticleRenderer create(
        int width,
        int height,
        const char* title,
        size_t particleCount,
        float particleVisualRadius,
        glm::vec3 lightPosition,
        glm::mat4 cameraViewMatrix,
        glm::mat4 cameraProjectionMatrix
    );

    ~ParticleRenderer();
    // no copy or move constructors/assignment
    ParticleRenderer(const ParticleRenderer&) = delete;
    ParticleRenderer& operator=(const ParticleRenderer&) = delete;
    ParticleRenderer(const ParticleRenderer&&) = delete;
    ParticleRenderer& operator=(const ParticleRenderer&&) = delete;

    int shouldClose();
    void render();

private:
    ParticleRenderer(
        GLFWwindow* window,
        size_t particleCount,
        float particleVisualRadius,
        glm::vec3 lightPosition,
        glm::mat4 cameraViewMatrix,
        glm::mat4 cameraProjectionMatrix
    );

    GLFWwindow* window;
    const unsigned int shaderProgram;
    const GLint lightPosUniformLocation;
    const GLint particleRadiusUniformLocation;
    const GLint projectionMatrixUniformLocation;
    const GLint viewMatrixUniformLocation;

    const std::array<float, 18> billboardQuadVertices;
    const unsigned int billboardQuadVerticesVBO;
    const unsigned int particlePositionsVBO;
    const unsigned int particleColoursVBO;
    // particlesVAO must come after the 3 VBOs as it depends on them
    const unsigned int particlesVAO;

    const size_t particleCount;
    std::vector<float> colourData;
    bool colourDirty = false;
};
