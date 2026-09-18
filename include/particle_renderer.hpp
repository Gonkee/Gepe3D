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
        float particleVisualRadius,
        glm::vec3 lightPosition,
        glm::mat4 cameraViewMatrix,
        glm::mat4 cameraProjectionMatrix
    );

    // shader uniform locations
    static constexpr GLint UNIFORM_LOCATION_LIGHT_POS         = 0;
    static constexpr GLint UNIFORM_LOCATION_MAX_X             = 1;
    static constexpr GLint UNIFORM_LOCATION_PARTICLE_RADIUS   = 2;
    static constexpr GLint UNIFORM_LOCATION_PROJECTION_MATRIX = 3;
    static constexpr GLint UNIFORM_LOCATION_VIEW_MATRIX       = 4;


    GLFWwindow* window;
    const unsigned int shaderProgram;
    const std::array<float, 18> billboardQuadVertices;
    const unsigned int billboardQuadVerticesVBO;
    const unsigned int particlePositionsVBO;
    const unsigned int particleColoursVBO;
    // particlesVAO must come after the 3 VBOs as it depends on them
    const unsigned int particlesVAO;
};
