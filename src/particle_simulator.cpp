#include "particle_simulator.hpp"
#include "kernels.cl.h"
#include <vector>
#include <stdexcept>

void setupOpenCL() {
    std::vector<cl::Platform> platforms;
    cl::Platform::get(&platforms);
    if (platforms.empty()) throw std::runtime_error("Could not find OpenCL platform");

    cl::Platform platform = platforms[0];
    if (cl::Platform::setDefault(platform) != platform)
        throw std::runtime_error("Error setting default platform");

    std::string source(reinterpret_cast<const char*>(kernels_cl), sizeof(kernels_cl));
    cl::Program kernels(source);
}
