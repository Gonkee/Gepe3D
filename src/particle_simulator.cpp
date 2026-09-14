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
      posData    (std::vector<float>(particleCount * 3)),
      ePosData   (std::vector<float>(particleCount * 3)),
      velData    (std::vector<float>(particleCount * 3)),
      colourData (std::vector<float>(particleCount * 3)),
      phaseData  (std::vector<int>(particleCount)),

      clQueue(cl::CommandQueue(cl::Context::getDefault(), cl::Device::getDefault())),
      // TODO: probably change to kernel functors for convenience
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
      k12_correct_fluid_velocity    (cl::Kernel(kernels, "correct_fluid_velocity"   )),

      // TODO: must use fixed width types like int32_t to match opencl
      b_pos           (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_vel           (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_ePos          (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_vorticities   (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_posCorrection (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_velCorrection (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_imass         (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount)),
      b_lambdas       (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount)),

      b_phase               (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * particleCount)),
      b_sortedParticleIDs   (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * particleCount)),
      b_cellStartAndEndIDs  (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * particleCount)),
      b_cellIDsOfParticles  (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * particleCount)),
      b_numParticlesPerCell (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * cellCount * 2)),
      b_particleIDinCell    (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * cellCount))

{
    // TODO: fill imass with 1's
    enqueueFillFloatBufferHelper(b_imass, 0, particleCount);
    clQueue.finish();
}

template <typename... Args>
void ParticleSimulator::enqueueKernelHelper(cl::Kernel& kernel, size_t numWorkUnits, Args&&... args) {
    size_t index = 0;
    (kernel.setArg(index++, std::forward<Args>(args)), ...);
    clQueue.enqueueNDRangeKernel(
        kernel,
        cl::NullRange,             // offset
        cl::NDRange(numWorkUnits), // global
        cl::NullRange              // local
    );
}

template <typename T>
void ParticleSimulator::enqueueWriteBufferHelper(const cl::Buffer& buffer, const std::vector<T>& vector) {
    clQueue.enqueueWriteBuffer(buffer, false, 0, sizeof(T) * vector.size(), vector.data());
}

template <typename T>
void ParticleSimulator::enqueueReadBufferHelper(const cl::Buffer& buffer, std::vector<T>& vector) {
    clQueue.enqueueReadBuffer(buffer, false, 0, sizeof(T) * vector.size(), vector.data());
}

template <typename T>
void ParticleSimulator::enqueueFillBufferHelper(const cl::Buffer& buffer, std::type_identity_t<T> value, size_t count) {
    clQueue.enqueueFillBuffer(buffer, value, 0, sizeof(T) * count);
}

void ParticleSimulator::enqueueFillIntBufferHelper(const cl::Buffer& buffer, int32_t value, size_t count) {
    clQueue.enqueueFillBuffer(buffer, value, 0, sizeof(int32_t) * count);
}

void ParticleSimulator::enqueueFillFloatBufferHelper(const cl::Buffer& buffer, float value, size_t count) {
    clQueue.enqueueFillBuffer(buffer, value, 0, sizeof(float) * count);
}

void ParticleSimulator::update() {
    if (posDirty) {
        enqueueWriteBufferHelper(b_pos, posData);
        posDirty = false;
    }
    if (velDirty) {
        enqueueWriteBufferHelper(b_vel, velData);
        velDirty = false;
    }
    if (phaseDirty) {
        enqueueWriteBufferHelper(b_phase, phaseData);
        phaseDirty = false;
    }
    // enqueueFillIntBufferHelper(b_sortedParticleIDs, 0, particleCount);
    // enqueueFillIntBufferHelper(b_numParticlesPerCell, 0, particleCount);
    // enqueueFillFloatBufferHelper(b_posCorrection, 0, particleCount * 3);

    enqueueFillBufferHelper<int32_t>(b_sortedParticleIDs, 0, particleCount);
    enqueueFillBufferHelper<int32_t>(b_numParticlesPerCell, 0, particleCount);
    enqueueFillBufferHelper<float>  (b_posCorrection, 0, particleCount * 3);

    // predict particle positions, then sort particle IDs for neighbour finding accordingly
    enqueueKernelHelper(k01_predict_positions        , particleCount  , DELTA_TIME, b_pos, b_vel, b_ePos, b_phase, GRAVITY.x, GRAVITY.y, GRAVITY.z);
    enqueueKernelHelper(k02_assign_particle_cells    , particleCount  , b_ePos, b_numParticlesPerCell, b_cellIDsOfParticles, b_particleIDinCell);
    enqueueKernelHelper(k03_find_cells_start_and_end , cellCount      , b_numParticlesPerCell, b_cellStartAndEndIDs);
    enqueueKernelHelper(k04_sort_particle_ids_by_cell, particleCount  , b_particleIDinCell, b_cellStartAndEndIDs, b_cellIDsOfParticles, b_sortedParticleIDs);

    // correct the predicted positions to satisfy fluid and contact constraints
    enqueueKernelHelper(k05_compute_lambdas          , particleCount  , b_ePos, b_imass, b_lambdas, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
    enqueueKernelHelper(k06_compute_fluid_corrections, particleCount  , b_ePos, b_imass, b_lambdas, b_posCorrection, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
    enqueueKernelHelper(k07_compute_solid_corrections, particleCount  , b_ePos, b_imass, b_posCorrection, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
    enqueueKernelHelper(k08_apply_corrections        , particleCount  , b_pos, b_ePos, b_posCorrection, b_phase);

    cpuSolveDistConstraints(0.2f, 2); // parameters: stiffness, iterations

    // update particle velocities using corrected predictions, then correct fluid velocities for vorticity & viscosity
    enqueueKernelHelper(k09_update_velocity          , particleCount  , DELTA_TIME, b_pos, b_vel, b_ePos, b_phase);
    enqueueKernelHelper(k10_compute_vorticity        , particleCount  , b_pos, b_vel, b_vorticities, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
    enqueueKernelHelper(k11_apply_vorticity_viscosity, particleCount  , b_pos, b_vel, b_vorticities, b_velCorrection, b_imass, DELTA_TIME, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
    enqueueKernelHelper(k12_correct_fluid_velocity   , particleCount  , b_vel, b_velCorrection);

    // read position and velocity data from GPU
    enqueueReadBufferHelper(b_pos, posData);
    enqueueReadBufferHelper(b_vel, velData);

    clQueue.finish();
}

void ParticleSimulator::cpuSolveDistConstraints(float stiffness, size_t iterations) {
    // TODO
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
