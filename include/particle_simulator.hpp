#pragma once

#define CL_HPP_ENABLE_EXCEPTIONS
#define CL_HPP_MINIMUM_OPENCL_VERSION 120
#define CL_HPP_TARGET_OPENCL_VERSION 120
#include <CL/opencl.hpp>
#include <glm/vec3.hpp>

class ParticleSimulator {
public:
    static ParticleSimulator create(size_t particleCount);

    ~ParticleSimulator();
    // no copy or move constructors/assignment
    ParticleSimulator(const ParticleSimulator&) = delete;
    ParticleSimulator& operator=(const ParticleSimulator&) = delete;
    ParticleSimulator(const ParticleSimulator&&) = delete;
    ParticleSimulator& operator=(const ParticleSimulator&&) = delete;

    static constexpr int
        PHASE_LIQUID = 0,
        PHASE_SOLID = 1,
        PHASE_STATIC = 2,

        gridRowsX = 16,
        gridRowsY = 10,
        gridRowsZ = 16,

        cellCount = gridRowsX * gridRowsY * gridRowsZ,

        barCellsX = 12,
        barCellsY = 1,
        barCellsZ = 1;

    static constexpr float
        PARTICLE_RADIUS = 0.2f,
        GRID_CELL_WIDTH = 0.6f,
        KERNEL_SIZE     = 0.6f,
        REST_DENSITY    = 80.0f,

        MAX_X = GRID_CELL_WIDTH * gridRowsX,
        MAX_Y = GRID_CELL_WIDTH * gridRowsY,
        MAX_Z = GRID_CELL_WIDTH * gridRowsZ;

    static constexpr glm::vec3
        GRAVITY = glm::vec3(0, -6, 0),
        lowCenter = glm::vec3(MAX_X, MAX_Y / 3, MAX_Z) / 2.0f;

private:
    static std::string generateKernelDefines();

    ParticleSimulator(size_t particleCount, cl::Program kernels);

    const size_t particleCount;

    const cl::Platform clPlatform;
    const cl::Device clDevice;
    const cl::Context clContext;
    const cl::CommandQueue clQueue;

    const cl::Kernel
        k01_predict_positions,
        k02_assign_particle_cells,
        k03_find_cells_start_and_end,
        k04_sort_particle_ids_by_cell,
        k05_compute_lambdas,
        k07_compute_solid_corrections,
        k06_compute_fluid_corrections,
        k08_apply_corrections,
        k09_update_velocity,
        k10_compute_vorticity,
        k11_apply_vorticity_viscosity,
        k12_correct_fluid_velocity;

    const cl::Buffer
        b_pos,              // positions
        b_vel,              // velocities
        b_ePos,             // estimated positions
        b_imass,            // inverse masses
        b_lambdas,          // fluid correction scalar
        b_phase,            // particle phase
        b_vorticities,      // fluid vorticities
        b_posCorrection,    // position correction
        b_velCorrection,    // velocity correction

        // buffers for neighbour search
        b_sortedParticleIDs,
        b_cellStartAndEndIDs,
        b_cellIDsOfParticles,
        b_numParticlesPerCell,
        b_particleIDinCell;

    bool
        posDirty = false,
        velDirty = false,
        phaseDirty = false,
        colourDirty = false;

    struct Constraint {
        size_t p1;
        size_t p2;
        float distance;
    };

    std::vector<Constraint> constraints;
};

void setupOpenCL();
int testCL();
