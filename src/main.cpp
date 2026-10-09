#include "particle_renderer.hpp"
#include "particle_simulator.hpp"
#include <glm/glm.hpp>
#include <glm/vec3.hpp>
#include <glm/ext/matrix_transform.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <print>
#include <random>

constexpr float PARTICLE_RADIUS = 0.2f;
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

void createBall(
    ParticleSimulator& simulator,
    ParticleRenderer& renderer,
    float x, float y, float z,
    float radius, float particleGap
) {
    static const std::pair<glm::uvec3, glm::uvec3> connections[] = {
        // 1 axis
        std::pair(glm::uvec3(0, 0, 0), glm::uvec3(1, 0, 0)),
        std::pair(glm::uvec3(0, 0, 0), glm::uvec3(0, 1, 0)),
        std::pair(glm::uvec3(0, 0, 0), glm::uvec3(0, 0, 1)),

        // 2 axes
        std::pair(glm::uvec3(0, 0, 0), glm::uvec3(1, 1, 0)),
        std::pair(glm::uvec3(0, 0, 0), glm::uvec3(0, 1, 1)),
        std::pair(glm::uvec3(0, 0, 0), glm::uvec3(1, 0, 1)),

        // 2 axes other
        std::pair(glm::uvec3(1, 0, 0), glm::uvec3(0, 1, 0)),
        std::pair(glm::uvec3(1, 0, 0), glm::uvec3(0, 0, 1)),
        std::pair(glm::uvec3(0, 1, 0), glm::uvec3(0, 0, 1)),

        // 3 axes
        std::pair(glm::uvec3(0, 0, 0), glm::uvec3(1, 1, 1)),
        std::pair(glm::uvec3(1, 0, 0), glm::uvec3(0, 1, 1)),
        std::pair(glm::uvec3(0, 1, 0), glm::uvec3(1, 0, 1)),
        std::pair(glm::uvec3(0, 0, 1), glm::uvec3(1, 1, 0)),
    };

    size_t resolution = radius / particleGap * 2;
    std::vector<std::optional<size_t>> gridCoordsToParticleIndex(resolution * resolution * resolution, std::nullopt);
    auto flattenCoords = [resolution](size_t x, size_t y, size_t z) {
        if (x >= resolution || y >= resolution || z >= resolution)
            throw std::runtime_error("coordinates out of bounds");
        return (x * resolution * resolution) + (y * resolution) + (z);
    };

    size_t currentParticleIndex = 0;
    for (size_t gridX = 0; gridX < resolution; ++gridX) {
        for (size_t gridY = 0; gridY < resolution; ++gridY) {
            for (size_t gridZ = 0; gridZ < resolution; ++gridZ) {
                float offsetX = std::lerp(-radius, radius, gridX / (resolution - 1.0f));
                float offsetY = std::lerp(-radius, radius, gridY / (resolution - 1.0f));
                float offsetZ = std::lerp(-radius, radius, gridZ / (resolution - 1.0f));
                float dist = std::sqrt(offsetX * offsetX + offsetY * offsetY + offsetZ * offsetZ);
                if (dist <= radius) {
                    renderer.setColour(currentParticleIndex, 1.0f, 0.6f, 0.0f);
                    simulator.setPhase(currentParticleIndex, ParticleSimulator::PHASE_SOLID);
                    simulator.setPos(currentParticleIndex, x + offsetX, y + offsetY, z + offsetZ);
                    gridCoordsToParticleIndex[flattenCoords(gridX, gridY, gridZ)] = currentParticleIndex;
                    currentParticleIndex++;
                }
            }
        }
    }

    for (size_t gridX = 0; gridX < resolution; ++gridX) {
        for (size_t gridY = 0; gridY < resolution; ++gridY) {
            for (size_t gridZ = 0; gridZ < resolution; ++gridZ) {
                for (const std::pair<glm::uvec3, glm::uvec3>& connection : connections) {
                    glm::uvec3 currentCoords(gridX, gridY, gridZ);
                    glm::uvec3 coords1 = currentCoords + connection.first;
                    glm::uvec3 coords2 = currentCoords + connection.second;

                    if (coords1.x >= resolution || coords1.y >= resolution || coords1.z >= resolution ||
                        coords2.x >= resolution || coords2.y >= resolution || coords2.z >= resolution) {
                        continue; // out of bounds
                    }

                    auto& particleEntry1 = gridCoordsToParticleIndex[
                        flattenCoords(coords1.x, coords1.y, coords1.z)];
                    auto& particleEntry2 = gridCoordsToParticleIndex[
                        flattenCoords(coords2.x, coords2.y, coords2.z)];
                    if (!particleEntry1.has_value() || !particleEntry2.has_value()) continue;

                    size_t particleIndex1 = particleEntry1.value();
                    size_t particleIndex2 = particleEntry2.value();
                    float distance = glm::length(simulator.getPos(particleIndex1) - simulator.getPos(particleIndex2));
                    simulator.addDistConstraint(particleIndex1, particleIndex2, distance);
                }
            }
        }
    }
}

void createBlock(
    ParticleSimulator& simulator,
    ParticleRenderer& renderer,
    std::vector<std::pair<size_t, glm::vec3>>& particlesList,
    float x, float y, float z,
    float dimX, float dimY, float dimZ,
    float particleGap, int startParticleIndex
) {
    size_t resX = (dimX / particleGap) + 1;
    size_t resY = (dimY / particleGap) + 1;
    size_t resZ = (dimZ / particleGap) + 1;
    size_t currentParticleIndex = startParticleIndex;
    for (size_t i = 0; i < resX; i++) {
        for (size_t j = 0; j < resY; j++) {
            for (size_t k = 0; k < resZ; k++) {
                simulator.setPhase(currentParticleIndex, ParticleSimulator::PHASE_STATIC);
                renderer.setColour(currentParticleIndex, 0.4f, 0.4f, 0.4f);
                float px = x + i * particleGap;
                float py = y + j * particleGap;
                float pz = z + k * particleGap;
                simulator.setPos(currentParticleIndex, px, py, pz);
                particlesList.push_back(
                    std::pair(currentParticleIndex, glm::vec3(px, py, pz))
                );
                currentParticleIndex++;
            }
        }
    }
}

int main() {
    ParticleSimulator simulator = ParticleSimulator::create(PARTICLE_COUNT);

    auto [cameraViewMatrix, cameraProjectionMatrix] = getCameraMatrices(
        50.0f,
        16.0f / 9.0f,
        0.01f,
        500.0f,
        ParticleSimulator::lowCenter + glm::vec3(-12, 8, -6),
        ParticleSimulator::lowCenter
    );

    ParticleRenderer renderer = ParticleRenderer::create(
        1280,
        720,
        "Gepe3D",
        PARTICLE_COUNT,
        PARTICLE_RADIUS,
        LIGHT_POSITION,
        cameraViewMatrix,
        cameraProjectionMatrix
    );

    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_real_distribution<float> distr(0.0f, 1.0f);
    for (size_t i = 0; i < PARTICLE_COUNT; ++i) {
        float x = distr(gen) * ParticleSimulator::MAX_X;
        float y = distr(gen) * ParticleSimulator::MAX_Y * 0.5f;
        float z = distr(gen) * ParticleSimulator::MAX_Z;
        simulator.setPos(i, x, y, z);
        simulator.setPhase(i, ParticleSimulator::PHASE_LIQUID);
        renderer.setColour( i, 0, 0.5f, 1 );
    }

    createBall(
        simulator, renderer,
        ParticleSimulator::MAX_X * 0.5f,
        ParticleSimulator::MAX_Y * 0.5f,
        ParticleSimulator::MAX_Z * 0.5f,
        1.2f, 0.15f
    );

    std::vector<std::pair<size_t, glm::vec3>> particlesList;
    createBlock(
        simulator, renderer, particlesList,
        ParticleSimulator::MAX_X * 0.4f, ParticleSimulator::MAX_Y * 0.1f, ParticleSimulator::MAX_Z * 0.4f,
        ParticleSimulator::MAX_X * 0.2f, ParticleSimulator::MAX_Y * 0.3f, ParticleSimulator::MAX_Z * 0.2f,
        0.15f, 3000
    );

    float totalTime = 0;
    while (!renderer.shouldClose()) {
        // move the block around
        for (auto& blockParticle : particlesList) {
            size_t& blockParticleIndex = blockParticle.first;
            glm::vec3& blockParticlePosition = blockParticle.second;
            simulator.setPos(blockParticleIndex,
                blockParticlePosition.x + cos(totalTime * 1.5f) * ParticleSimulator::MAX_X * 0.3f,
                blockParticlePosition.y,
                blockParticlePosition.z + sin(totalTime * 1.5f) * ParticleSimulator::MAX_Z * 0.3f
            );
        }
        simulator.update();
        renderer.render(simulator.getPosData());
        totalTime += ParticleSimulator::DELTA_TIME;
    }
    return 0;
}

