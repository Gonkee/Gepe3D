#pragma once

#define GLAD_GL_IMPLEMENTATION
#include <glad/gl.h>
#define GLFW_INCLUDE_NONE
#include <GLFW/glfw3.h>

#include <array>

class ParticleRenderer {
public:
    static ParticleRenderer create(
        int width,
        int height,
        const char* title,
        float particleVisualRadius
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
    ParticleRenderer(GLFWwindow* window, float particleVisualRadius);

    GLFWwindow* window;
    const unsigned int shaderProgram;
    const std::array<float, 18> billboardQuadVertices;
    const unsigned int billboardQuadVerticesVBO;
    const unsigned int particlePositionsVBO;
    const unsigned int particleColoursVBO;
    // particlesVAO must come after the 3 VBOs as it depends on them
    const unsigned int particlesVAO;
};
