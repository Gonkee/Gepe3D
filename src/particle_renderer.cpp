#include "particle_renderer.hpp"
#include "point_sphere.vert.h"
#include "point_sphere.frag.h"

#include <glm/gtc/type_ptr.hpp>
#include <stdexcept>

unsigned int loadShader(GLenum shaderType, const GLchar* shaderSource) {
    unsigned int shader = glCreateShader(shaderType);
    glShaderSource(shader, 1, &shaderSource, NULL);
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
    unsigned int vertexShader = loadShader(GL_VERTEX_SHADER, reinterpret_cast<const GLchar*>(point_sphere_vert));
    unsigned int fragmentShader = loadShader(GL_FRAGMENT_SHADER, reinterpret_cast<const GLchar*>(point_sphere_frag));
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

unsigned int genVBO() {
    unsigned int VBO;
    glGenBuffers(1, &VBO);
    return VBO;
}

unsigned int genVAO() {
    unsigned int VAO;
    glGenVertexArrays(1, &VAO);
    return VAO;
}

unsigned int genParticlesVAO(
    unsigned int billboardQuadVerticesVBO,
    unsigned int particlePositionsVBO,
    unsigned int particleColoursVBO
) {
    unsigned int VAO;
    glGenVertexArrays(1, &VAO);
    glBindVertexArray(VAO);

    auto setFloatVertexAttrib = [](
        unsigned int VBO,
        unsigned int attribIndex,
        unsigned int attribSize,
        bool instanced
    ) {
        glBindBuffer(GL_ARRAY_BUFFER, VBO);
        glVertexAttribPointer(
            attribIndex,
            attribSize,
            GL_FLOAT,
            GL_FALSE,
            attribSize * sizeof(float),
            reinterpret_cast<void*>(0)
        );
        if (instanced) glVertexAttribDivisor(attribIndex, 1);
        glEnableVertexAttribArray(attribIndex);
    };

    setFloatVertexAttrib(billboardQuadVerticesVBO, 0, 3, false);
    setFloatVertexAttrib(particlePositionsVBO,     1, 3, true);
    setFloatVertexAttrib(particleColoursVBO,       2, 3, true);
    return VAO;
}

ParticleRenderer ParticleRenderer::create(
    int width,
    int height,
    const char* title,
    size_t particleCount,
    float particleVisualRadius,
    glm::vec3 lightPosition,
    glm::mat4 cameraViewMatrix,
    glm::mat4 cameraProjectionMatrix
) {
    if (glfwInit() != GLFW_TRUE) {
        throw std::runtime_error("Failed to init GLFW");
    }
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
    GLFWwindow* window = glfwCreateWindow(width, height, title, NULL, NULL);
    if (!window) {
        glfwTerminate();
        throw std::runtime_error("Failed to create GLFW window");
    }
    glfwMakeContextCurrent(window);
    gladLoadGL(glfwGetProcAddress);
    glfwSwapInterval(0); // 0 disables vsync for max FPS

    return ParticleRenderer(
        window,
        particleCount,
        particleVisualRadius,
        lightPosition,
        cameraViewMatrix,
        cameraProjectionMatrix
    );
}

ParticleRenderer::ParticleRenderer(
    GLFWwindow* window,
    size_t particleCount,
    float particleVisualRadius,
    glm::vec3 lightPosition,
    glm::mat4 cameraViewMatrix,
    glm::mat4 cameraProjectionMatrix
)
    : window(window),
      shaderProgram(createShaderProgram()),
      lightPosUniformLocation        (glGetUniformLocation(shaderProgram, "lightPos")),
      particleRadiusUniformLocation  (glGetUniformLocation(shaderProgram, "particleRadius")),
      projectionMatrixUniformLocation(glGetUniformLocation(shaderProgram, "projectionMatrix")),
      viewMatrixUniformLocation      (glGetUniformLocation(shaderProgram, "viewMatrix")),
      billboardQuadVertices{
         // triangle 1
        -particleVisualRadius / 2, -particleVisualRadius / 2, 0,
         particleVisualRadius / 2, -particleVisualRadius / 2, 0,
         particleVisualRadius / 2,  particleVisualRadius / 2, 0,
         // triangle 2
        -particleVisualRadius / 2, -particleVisualRadius / 2, 0,
         particleVisualRadius / 2,  particleVisualRadius / 2, 0,
        -particleVisualRadius / 2,  particleVisualRadius / 2, 0,
      },
      billboardQuadVerticesVBO(genVBO()),
      particlePositionsVBO(genVBO()),
      particleColoursVBO(genVBO()),
      particlesVAO(genVAO()),
      // particlesVAO(genParticlesVAO(
      //   billboardQuadVerticesVBO,
      //   particlePositionsVBO,
      //   particleColoursVBO
      // )),
      particleCount(particleCount),
      colourData (std::vector<float>(particleCount * 3))
{
    // TODO: actually load data into VBOs
    //...
    glUseProgram(shaderProgram);
    glUniform3f(lightPosUniformLocation      , lightPosition.x, lightPosition.y, lightPosition.z);
    glUniform1f(particleRadiusUniformLocation, particleVisualRadius);
    glUniformMatrix4fv(projectionMatrixUniformLocation, 1, false, glm::value_ptr(cameraProjectionMatrix));
    glUniformMatrix4fv(viewMatrixUniformLocation      , 1, false, glm::value_ptr(cameraViewMatrix));

    auto vboAllocateSpaceAndSetVertexAttrib = [](
        unsigned int VBO,
        size_t numFloatsInBuffer,
        unsigned int attribIndex,
        unsigned int attribSize,
        bool instanced
    ) {
        glBindBuffer(GL_ARRAY_BUFFER, VBO);
        glBufferData(GL_ARRAY_BUFFER, numFloatsInBuffer * sizeof(float), nullptr, GL_STATIC_DRAW);
        glVertexAttribPointer(
            attribIndex,
            attribSize,
            GL_FLOAT,
            GL_FALSE,
            attribSize * sizeof(float),
            reinterpret_cast<void*>(0)
        );
        if (instanced) glVertexAttribDivisor(attribIndex, 1);
        glEnableVertexAttribArray(attribIndex);
    };

    glBindVertexArray(particlesVAO);
    vboAllocateSpaceAndSetVertexAttrib(billboardQuadVerticesVBO, billboardQuadVertices.size(), 0, 3, false);
    vboAllocateSpaceAndSetVertexAttrib(particlePositionsVBO, particleCount * 3, 1, 3, true);
    vboAllocateSpaceAndSetVertexAttrib(particleColoursVBO  , particleCount * 3, 2, 3, true);
}

ParticleRenderer::~ParticleRenderer() {
    glfwDestroyWindow(window);
    glfwTerminate();
}

int ParticleRenderer::shouldClose() { return glfwWindowShouldClose(window); }

void ParticleRenderer::render(const float *posData) {
    int width, height;
    glfwGetFramebufferSize(window, &width, &height);
    // const float ratio = width / (float) height;

    glViewport(0, 0, width, height);

    // ...
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    if (colourDirty) {
        glBindBuffer(GL_ARRAY_BUFFER, particleColoursVBO);
        glBufferSubData(GL_ARRAY_BUFFER, 0, particleCount * 3 * sizeof(float), colourData.data());
        colourDirty = false;
    }
    glBindBuffer(GL_ARRAY_BUFFER, particlePositionsVBO);
    glBufferSubData(GL_ARRAY_BUFFER, 0, particleCount * 3 * sizeof(float), posData);

    // set stuff
    glBindVertexArray(particlesVAO);
    glDrawArraysInstanced(GL_TRIANGLES, 0, 6, particleCount);

    glfwSwapBuffers(window);
    glfwPollEvents();
}

