#define GLAD_GL_IMPLEMENTATION
#include <glad/gl.h>
#define GLFW_INCLUDE_NONE
#include <GLFW/glfw3.h>


class ParticleRenderer {
public:
    ParticleRenderer(int width, int height, const char* title);
    ~ParticleRenderer();

    // no copy or move constructors/assignment
    ParticleRenderer(const ParticleRenderer&) = delete;
    ParticleRenderer& operator=(const ParticleRenderer&) = delete;
    ParticleRenderer(const ParticleRenderer&&) = delete;
    ParticleRenderer& operator=(const ParticleRenderer&&) = delete;

    int shouldClose();
    void render();

private:
    GLFWwindow* window = nullptr;

    void loadShader();
    void createShaderProgram();
};
