#include "particle_renderer.hpp"

constexpr float PARTICLE_RADIUS = 0.15f;

int main(void)
{
    ParticleRenderer renderer = ParticleRenderer::create(640, 480, "Gepe3D", PARTICLE_RADIUS);
    while (!renderer.shouldClose()) {
        renderer.render();
    }
    return 0;
}

