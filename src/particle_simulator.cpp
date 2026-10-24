#include "particle_simulator.hpp"
#include "kernels.cl.h"
#include <vector>
#include <stdexcept>
#include <iostream>
#include <memory>
#include <algorithm>
#include <print>
#include <format>
#include <cmath>
#include <glm/geometric.hpp>

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
    defines += std::format("#define MAX_VEL {:.4f}f\n", PARTICLE_MAX_VELOCITY);
    defines += std::format("#define RELAXATION {:.4f}f\n", RELAXATION);
    defines += std::format("#define K_P {:.4f}f\n", K_P);
    defines += std::format("#define E_P {:.4f}f\n", E_P);
    defines += std::format("#define DQ_P {:.4f}f\n", DQ_P);
    defines += std::format("#define VISCOSITY_COEFF {:.4f}f\n", VISCOSITY_COEFF);
    defines += std::format("#define CONSTRAINT_SOLVER_STIFFNESS {:.4f}f\n", CONSTRAINT_SOLVER_STIFFNESS);
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
      phaseData  (std::vector<int>(particleCount)),

      clQueue(cl::CommandQueue(cl::Context::getDefault(), cl::Device::getDefault())),
      // TODO: probably change to kernel functors for convenience
      k01_predict_positions         (cl::Kernel(kernels, "predict_positions"         )),
      k02_assign_particle_cells     (cl::Kernel(kernels, "assign_particle_cells"     )),
      k03_find_cells_start_and_end  (cl::Kernel(kernels, "find_cells_start_and_end"  )),
      k04_sort_particle_ids_by_cell (cl::Kernel(kernels, "sort_particle_ids_by_cell" )),
      k05_compute_lambdas           (cl::Kernel(kernels, "compute_lambdas"           )),
      k06_compute_fluid_corrections (cl::Kernel(kernels, "compute_fluid_corrections" )),
      k07_compute_solid_corrections (cl::Kernel(kernels, "compute_solid_corrections" )),
      k08_apply_corrections         (cl::Kernel(kernels, "apply_corrections"         )),
      k09_solve_distance_constraints(cl::Kernel(kernels, "solve_distance_constraints")),
      k10_update_velocity           (cl::Kernel(kernels, "update_velocity"           )),
      k11_compute_vorticity         (cl::Kernel(kernels, "compute_vorticity"         )),
      k12_apply_vorticity_viscosity (cl::Kernel(kernels, "apply_vorticity_viscosity" )),
      k13_correct_fluid_velocity    (cl::Kernel(kernels, "correct_fluid_velocity"    )),

      // TODO: must use fixed width types like int32_t to match opencl
      b_pos           (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_vel           (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_ePos          (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_posCorrection (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_vorticities   (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_velCorrection (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount * 3)),
      b_imass         (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount)),
      b_lambdas       (cl::Buffer(CL_MEM_READ_WRITE, sizeof(float) * particleCount)),

      b_phase               (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * particleCount)),
      b_sortedParticleIDs   (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * particleCount)),
      b_cellIDsOfParticles  (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * particleCount)),
      b_particleIDinCell    (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * particleCount)),
      b_cellStartAndEndIDs  (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * cellCount * 2)),
      b_numParticlesPerCell (cl::Buffer(CL_MEM_READ_WRITE, sizeof(int32_t) * cellCount))

{
    enqueueFillBufferHelper<float>(b_imass, 1, particleCount);
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


void ParticleSimulator::setPos(size_t index, float x, float y, float z) {
    if (setupFinalised) {
        throw std::runtime_error("attempted to set position after setup has been finalised");
    }
    posData[index * 3 + 0] = x;
    posData[index * 3 + 1] = y;
    posData[index * 3 + 2] = z;
}

glm::vec3 ParticleSimulator::getPos(size_t index) {
    if (setupFinalised) {
        throw std::runtime_error("attempted to get position after setup has been finalised");
    }
    return glm::vec3( posData[index * 3 + 0], posData[index * 3 + 1], posData[index * 3 + 2] );
}

void ParticleSimulator::setPhase(size_t index, int phase) {
    if (setupFinalised) {
        throw std::runtime_error("attempted to set phase after setup has been finalised");
    }
    phaseData[index] = phase;
}

void ParticleSimulator::addDistConstraint(size_t p1, size_t p2, float distance) {
    if (setupFinalised) {
        throw std::runtime_error("attempted to add constraint after setup has been finalised");
    }
    constraints.emplace_back(p1, p2, distance);
}

void ParticleSimulator::finaliseSetup() {
    enqueueWriteBufferHelper(b_pos, posData);
    enqueueWriteBufferHelper(b_vel, velData);
    enqueueWriteBufferHelper(b_phase, phaseData);

    // convert constraints to particle centric
    std::vector<uint32_t> particleConstraintsOffsets(particleCount + 1, 0);
    for (Constraint& c : constraints) {
        // first calculate the constraint counts for each particle
        // at the slot immediately after it
        // (p0's constraints start at 0, p1's start at p0's count)
        particleConstraintsOffsets[c.p1 + 1]++;
        particleConstraintsOffsets[c.p2 + 1]++;
    }
    for (size_t i = 0; i < particleCount; ++i) {
        // then prefix sum the counts so that they turn into offsets
        particleConstraintsOffsets[i + 1] += particleConstraintsOffsets[i];
    }
    // cursors = which position to put the next constraint (separately incremented for each particle)
    std::vector<uint32_t> particleConstraintsCursors(particleConstraintsOffsets);
    std::vector<ParticleConstraint> particleConstraints(constraints.size() * 2);
    for (Constraint& c : constraints) {
        particleConstraints[particleConstraintsCursors[c.p1]++] = { c.p2, c.distance };
        particleConstraints[particleConstraintsCursors[c.p2]++] = { c.p1, c.distance };
    }
    // send the calculated constraint data to the device
    b_particleConstraints = cl::Buffer(CL_MEM_READ_ONLY, sizeof(ParticleConstraint) * particleConstraints.size());
    b_particleConstraintsOffsets = cl::Buffer(CL_MEM_READ_ONLY, sizeof(uint32_t) * particleConstraintsOffsets.size());
    enqueueWriteBufferHelper(b_particleConstraints, particleConstraints);
    enqueueWriteBufferHelper(b_particleConstraintsOffsets, particleConstraintsOffsets);
    clQueue.finish();

    setupFinalised = true;
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

void ParticleSimulator::update(float staticParticleVelX, float staticParticleVelY, float staticParticleVelZ) {
    if (!setupFinalised) {
        throw std::runtime_error("attempted to update simulator state before setup has been finalised; call finaliseSetup()");
    }

    enqueueFillBufferHelper<int32_t>(b_sortedParticleIDs, 0, particleCount);
    enqueueFillBufferHelper<int32_t>(b_numParticlesPerCell, 0, cellCount);
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
    enqueueKernelHelper(k09_solve_distance_constraints, particleCount, b_particleConstraints, b_particleConstraintsOffsets, b_imass, b_ePos, b_posCorrection);
    enqueueKernelHelper(k08_apply_corrections        , particleCount  , b_pos, b_ePos, b_posCorrection, b_phase); // repeated, unaesthetic

    // update particle velocities using corrected predictions, then correct fluid velocities for vorticity & viscosity
    enqueueKernelHelper(k10_update_velocity          , particleCount  , DELTA_TIME, b_pos, b_vel, b_ePos, b_phase, staticParticleVelX, staticParticleVelY, staticParticleVelZ);
    enqueueKernelHelper(k11_compute_vorticity        , particleCount  , b_pos, b_vel, b_vorticities, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
    enqueueKernelHelper(k12_apply_vorticity_viscosity, particleCount  , b_pos, b_vel, b_vorticities, b_velCorrection, b_imass, DELTA_TIME, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
    enqueueKernelHelper(k13_correct_fluid_velocity   , particleCount  , b_vel, b_velCorrection);

    // read position and velocity data from GPU
    enqueueReadBufferHelper(b_pos, posData);
    enqueueReadBufferHelper(b_vel, velData);
    clQueue.finish();
}

const float *ParticleSimulator::getPosData() {
    return posData.data();
}
