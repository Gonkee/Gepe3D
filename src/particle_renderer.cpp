#include "particle_renderer.hpp"
#include <stdexcept>

#include "point_sphere.vert.h"
#include "point_sphere.frag.h"

ParticleRenderer::ParticleRenderer(int width, int height, const char* title) {
    if (glfwInit() != GLFW_TRUE) {
        throw std::runtime_error("Failed to init GLFW");
    }
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
    window = glfwCreateWindow(width, height, title, NULL, NULL);
    if (!window) {
        glfwTerminate();
        throw std::runtime_error("Failed to create GLFW window");
    }
    glfwMakeContextCurrent(window);
    gladLoadGL(glfwGetProcAddress);
    glfwSwapInterval(0); // 0 disables vsync for max FPS
}

ParticleRenderer::~ParticleRenderer() {
    glfwDestroyWindow(window);
    glfwTerminate();
}

int ParticleRenderer::shouldClose() { return glfwWindowShouldClose(window); }

void ParticleRenderer::render() {
    int width, height;
    glfwGetFramebufferSize(window, &width, &height);
    const float ratio = width / (float) height;

    glViewport(0, 0, width, height);
    glClear(GL_COLOR_BUFFER_BIT);

    glfwSwapBuffers(window);
    glfwPollEvents();
}

void ParticleRenderer::loadShader() {
    // TODO
}

void ParticleRenderer::createShaderProgram() {
    // TODO
}
