#include "particle_renderer.hpp"

int main(void)
{
    ParticleRenderer renderer(640, 480, "Gepe3D");
    while (!renderer.shouldClose()) {
        renderer.render();
    }
    return 0;
}

