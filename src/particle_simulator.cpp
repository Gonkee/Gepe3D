#include "particle_simulator.hpp"
#include "kernels.cl.h"
#include <vector>
#include <stdexcept>
#include <iostream>
#include <memory>
#include <algorithm>
#include <print>
#include <format>

std::string ParticleSimulator::generateKernelDefines() {
    std::string defines = "";
    defines += std::format("#define MAX_X {:.4f}f\n"       , MAX_X);
    defines += std::format("#define MAX_Y {:.4f}f\n"       , MAX_Y);
    defines += std::format("#define MAX_Z {:.4f}f\n"       , MAX_Z);
    defines += std::format("#define CELLCOUNT_X {}\n"      , gridRowsX);
    defines += std::format("#define CELLCOUNT_Y {}\n"      , gridRowsY);
    defines += std::format("#define CELLCOUNT_Z {}\n"      , gridRowsZ);
    defines += std::format("#define CELL_WIDTH {:.4f}f\n"  , GRID_CELL_WIDTH);
    defines += std::format("#define KERNEL_SIZE {:.4f}f\n" , KERNEL_SIZE);
    defines += std::format("#define REST_DENSITY {:.4f}f\n", REST_DENSITY);
    defines += std::format("#define PHASE_LIQUID {}\n"     , PHASE_LIQUID);
    defines += std::format("#define PHASE_SOLID {}\n"      , PHASE_SOLID);
    defines += std::format("#define PHASE_STATIC {}\n"     , PHASE_STATIC);
    return defines;
}

ParticleSimulator ParticleSimulator::create(size_t particleCount) {
    std::vector<cl::Platform> platforms;
    cl::Platform::get(&platforms);
    if (platforms.empty()) throw std::runtime_error("Could not find OpenCL platform");

    cl::Platform platform = platforms[0];
    if (cl::Platform::setDefault(platform) != platform)
        throw std::runtime_error("Error setting default platform");

    std::string sourceWithoutDefines(reinterpret_cast<const char*>(kernels_cl), sizeof(kernels_cl));
    std::string sourceDefines = generateKernelDefines();
    std::string fullSource = sourceDefines + sourceWithoutDefines;
    cl::Program kernels(fullSource);
    try {
        kernels.build("-cl-std=CL1.2");
    }
    catch (const cl::Error& e) {
        std::println(std::cerr, "OpenCL build failed: {}", e.what());
        cl_int buildErr = CL_SUCCESS;
        auto buildInfo = kernels.getBuildInfo<CL_PROGRAM_BUILD_LOG>(&buildErr);
        for (const auto& [device, log] : buildInfo) {
            std::println(
                std::cerr,
                "Build log for {}:\n\t{}",
                device.getInfo<CL_DEVICE_NAME>(),
                log
            );
        }
        throw;
    }

    return ParticleSimulator(particleCount, kernels);
}

ParticleSimulator::ParticleSimulator(size_t particleCount, cl::Program kernels)
    : particleCount(particleCount),
      k01_predict_positions         (cl::Kernel(kernels, "predict_positions"        )),
      k02_assign_particle_cells     (cl::Kernel(kernels, "assign_particle_cells"    )),
      k03_find_cells_start_and_end  (cl::Kernel(kernels, "find_cells_start_and_end" )),
      k04_sort_particle_ids_by_cell (cl::Kernel(kernels, "sort_particle_ids_by_cell")),
      k05_compute_lambdas           (cl::Kernel(kernels, "compute_lambdas"          )),
      k07_compute_solid_corrections (cl::Kernel(kernels, "compute_fluid_corrections")),
      k06_compute_fluid_corrections (cl::Kernel(kernels, "compute_solid_corrections")),
      k08_apply_corrections         (cl::Kernel(kernels, "apply_corrections"        )),
      k09_update_velocity           (cl::Kernel(kernels, "update_velocity"          )),
      k10_compute_vorticity         (cl::Kernel(kernels, "compute_vorticity"        )),
      k11_apply_vorticity_viscosity (cl::Kernel(kernels, "apply_vorticity_viscosity")),
      k12_correct_fluid_velocity    (cl::Kernel(kernels, "correct_fluid_velocity"   ))
{

}

int testCL() {
    std::vector<cl::Platform> platforms;
    cl::Platform::get(&platforms);
    if (platforms.empty()) throw std::runtime_error("Could not find OpenCL platform");

    cl::Platform platform = platforms[0];
    if (cl::Platform::setDefault(platform) != platform)
        throw std::runtime_error("Error setting default platform");

    std::string kernelSource = R"CLC(
        __kernel void vectorAdd(
            __global const int* a,
            __global const int* b,
            __global int* output)
        {
            int i = get_global_id(0);
            output[i] = a[i] + b[i];
        }
    )CLC";

    cl::Program vectorAddProgram(kernelSource);
    try {
        vectorAddProgram.build("-cl-std=CL1.2");
    }
    catch (...) {
        // Print build info for all devices
        cl_int buildErr = CL_SUCCESS;
        auto buildInfo = vectorAddProgram.getBuildInfo<CL_PROGRAM_BUILD_LOG>(&buildErr);
        for (auto &pair : buildInfo) {
            std::cerr << pair.second << std::endl << std::endl;
        }
        return 1;
    }

    cl::CommandQueue queue(cl::Context::getDefault(), cl::Device::getDefault());

    std::vector<int> inputA = {1, 2, 3, 4};
    std::vector<int> inputB = {10, 20, 30, 40};
    cl::Buffer bufferA(
        CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR,
        sizeof(int) * inputA.size(),
        inputA.data()
    );
    cl::Buffer bufferB(
        CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR,
        sizeof(int) * inputB.size(),
        inputB.data()
    );
    cl::Buffer bufferOutput(
        CL_MEM_WRITE_ONLY,
        sizeof(int) * inputA.size()
    );

    cl::Kernel kernel(vectorAddProgram, "vectorAdd");
    kernel.setArg(0, bufferA);
    kernel.setArg(1, bufferB);
    kernel.setArg(2, bufferOutput);
    queue.enqueueNDRangeKernel(
        kernel,
        cl::NullRange,
        cl::NDRange(inputA.size()),
        cl::NullRange
    );
    queue.finish();

    std::vector<int> output(inputA.size(), 0xdeadbeef);
    cl::copy(bufferOutput, begin(output), end(output));

    std::cout << "Output:\n";
    for (int o : output) {
        std::cout << "\t" << o << "\n";
    }
    return 0;
}
