#include "particle_renderer.hpp"
#include "particle_simulator.hpp"
#include <glm/glm.hpp>
#include <glm/vec3.hpp>
#include <glm/ext/matrix_transform.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <print>
#include <thread>
#include <chrono>

constexpr float PARTICLE_RADIUS = 0.15f;
constexpr size_t PARTICLE_COUNT = 20000;

constexpr glm::vec3 LIGHT_POSITION(0, 10, 0);

std::pair<glm::mat4, glm::mat4> getCameraMatrices(
    float fovDegrees,
    float aspectRatio,
    float nearClip,
    float farClip,
    glm::vec3 camPosition,
    glm::vec3 camLookAt
) {
    float dx = camLookAt.x - camPosition.x;
    float dy = camLookAt.y - camPosition.y;
    float dz = camLookAt.z - camPosition.z;
    float horizontalDist = glm::sqrt(dx * dx + dz * dz);
    float pitch = glm::degrees( glm::atan(dy, horizontalDist) );
    float yaw =   glm::degrees( glm::atan(dz, dx) );

    glm::vec3 localForward = glm::normalize(glm::vec3(
        glm::cos( glm::radians(pitch) ) * glm::cos( glm::radians(yaw) ),
        glm::sin( glm::radians(pitch) ),
        glm::cos( glm::radians(pitch) ) * glm::sin( glm::radians(yaw) )
    ));
    glm::vec3 localRight = glm::normalize( glm::cross(localForward, glm::vec3(0, 1, 0)) );
    glm::vec3 localUp    = glm::normalize( glm::cross(localRight  , localForward) );

    glm::mat4 viewMatrix = glm::lookAt(camPosition, camPosition + localForward, localUp);
    glm::mat4 projectionMatrix = glm::perspective( glm::radians(fovDegrees), aspectRatio, nearClip, farClip );

    return std::pair(viewMatrix, projectionMatrix);
}

int main(void)
{
    auto [cameraViewMatrix, cameraProjectionMatrix] = getCameraMatrices(
        50.0f,
        16.0f / 9.0f,
        0.01f,
        500.0f,
        ParticleSimulator::lowCenter + glm::vec3(-12, 8, -6),
        ParticleSimulator::lowCenter
    );
    ParticleSimulator simulator = ParticleSimulator::create(PARTICLE_COUNT);
    ParticleRenderer renderer = ParticleRenderer::create(
        640,
        480,
        "Gepe3D",
        PARTICLE_COUNT,
        PARTICLE_RADIUS,
        LIGHT_POSITION,
        cameraViewMatrix,
        cameraProjectionMatrix
    );
    while (!renderer.shouldClose()) {
        try{
            simulator.update();
        } catch (const cl::Error& e) {
            std::print(stderr, "OpenCL error: {} ({})\n", e.what(), e.err());
            // return 1;
        }
        renderer.render(simulator.getPosData());
        std::this_thread::sleep_for(std::chrono::milliseconds(10));
    }
    return 0;
}

