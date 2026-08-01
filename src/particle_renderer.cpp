#include "particle_renderer.hpp"
#include <stdexcept>

#include "point_sphere.vert.h"
#include "point_sphere.frag.h"


unsigned int loadShader(GLenum shaderType, const GLchar** shaderSource) {
    unsigned int shader = glCreateShader(shaderType);
    glShaderSource(shader, 1, shaderSource, NULL);
    glCompileShader(shader);
    int success;
    glGetShaderiv(shader, GL_COMPILE_STATUS, &success);
    if (!success) {
        char infoLog[512];
        glGetShaderInfoLog(shader, 512, NULL, infoLog);
        throw std::runtime_error("Error: shader compilation failed.\n" + std::string(infoLog));
    }
    return shader;
}

unsigned int createShaderProgram() {
    unsigned int vertexShader = loadShader(GL_VERTEX_SHADER, reinterpret_cast<const GLchar**>(&point_sphere_vert));
    unsigned int fragmentShader = loadShader(GL_FRAGMENT_SHADER, reinterpret_cast<const GLchar**>(&point_sphere_frag));
    unsigned int shaderProgram = glCreateProgram();
    glAttachShader(shaderProgram, vertexShader);
    glAttachShader(shaderProgram, fragmentShader);
    glLinkProgram(shaderProgram);
    int success;
    glGetProgramiv(shaderProgram, GL_LINK_STATUS, &success);
    if (!success) {
        char infoLog[512];
        glGetProgramInfoLog(shaderProgram, 512, NULL, infoLog);
        throw std::runtime_error("Error: shader program linking failed.\n" + std::string(infoLog));
    }
    glDeleteShader(vertexShader);
    glDeleteShader(fragmentShader);
    return shaderProgram;
}

ParticleRenderer::ParticleRenderer(
    int width,
    int height,
    const char* title,
    float particleVisualRadius
)
    : shaderProgram(createShaderProgram()),
      billboardQuadVertices{
         // triangle 1
        -particleVisualRadius / 2, -particleVisualRadius / 2, 0,
         particleVisualRadius / 2, -particleVisualRadius / 2, 0,
         particleVisualRadius / 2,  particleVisualRadius / 2, 0,
         // triangle 2
        -particleVisualRadius / 2, -particleVisualRadius / 2, 0,
         particleVisualRadius / 2,  particleVisualRadius / 2, 0,
        -particleVisualRadius / 2,  particleVisualRadius / 2, 0,
      }
{
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
    // const float ratio = width / (float) height;

    glViewport(0, 0, width, height);
    glClear(GL_COLOR_BUFFER_BIT);

    glfwSwapBuffers(window);
    glfwPollEvents();
}

void genVAO() {
    unsigned int VAO;
    glGenVertexArrays(1, &VAO);
    glBindVertexArray(VAO);
    // glBindBuffer(GL_ARRAY_BUFFER, VBO);
    // TODO
}
