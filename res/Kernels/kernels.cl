
// #define variables added in the C# code - don't use these as var names
// CELLCOUNT_X, CELLCOUNT_Y, CELLCOUNT_Z, CELL_WIDTH
// MAX_X, MAX_Y, MAX_Z
// KERNEL_SIZE, REST_DENSITY
// PHASE_LIQUID, PHASE_SOLID, PHASE_STATIC


// code to gather neighbours, must match input buffer names for particle ids, try not to use var names that might overlap
#define FOREACH_NEIGHBOUR_j                                                                                 \
    int cellID = cellIDsOfParticles[i];                                                                     \
    int neighbourCellIDs[3 * 3 * 3];                                                                        \
    int neighbourCellCount = 0;                                                                             \
    int3 cellCoords = cell_id_2_coords(cellID);                                                             \
    for ( int cx = max( cellCoords.x - 1, 0 ); cx <= min( cellCoords.x + 1, CELLCOUNT_X - 1 ); cx++ ) {     \
    for ( int cy = max( cellCoords.y - 1, 0 ); cy <= min( cellCoords.y + 1, CELLCOUNT_Y - 1 ); cy++ ) {     \
    for ( int cz = max( cellCoords.z - 1, 0 ); cz <= min( cellCoords.z + 1, CELLCOUNT_Z - 1 ); cz++ ) {     \
        neighbourCellIDs[ neighbourCellCount++ ] = cell_coords_2_id( (int3) (cx, cy, cz));                  \
    }}}                                                                                                     \
    for (int nc = 0; nc < neighbourCellCount; nc++) {                                                       \
        int nCellID = neighbourCellIDs[nc];                                                                 \
        for (int g = cellStartAndEndIDs[nCellID * 2 + 0]; g < cellStartAndEndIDs[nCellID * 2 + 1]; g++) {   \
            int j = sortedParticleIDs[g];

#define END_FOREACH_NEIGHBOUR_j }}

#define PI 3.1415926f

// Epsilon in gamma correction denominator
#define RELAXATION 0.01f

// Pressure terms
#define K_P  0.1f
#define E_P  4.0f
#define DQ_P 0.2f

#define VISCOSITY_COEFF 0.001f


float3 getVec(global float *buffer, int i) {
    return (float3) ( buffer[i * 3 + 0], buffer[i * 3 + 1], buffer[i * 3 + 2] );
}

void setVec(global float *buffer, int i, float3 val) {
    buffer[i * 3 + 0] = val.x;
    buffer[i * 3 + 1] = val.y;
    buffer[i * 3 + 2] = val.z;
}


int3 cell_id_2_coords(int id) {
    
    int x =   id / (CELLCOUNT_Y * CELLCOUNT_Z);
    int y = ( id % (CELLCOUNT_Y * CELLCOUNT_Z) ) / CELLCOUNT_Z;
    int z = ( id % (CELLCOUNT_Y * CELLCOUNT_Z) ) % CELLCOUNT_Z;
    return (int3) (x, y, z);
}

int cell_coords_2_id(int3 coords) {
    
    return
        coords.x * CELLCOUNT_Y * CELLCOUNT_Z +
        coords.y * CELLCOUNT_Z + 
        coords.z;
}

int get_cell_id(float3 pos) {
    int3 cellCoords = (int3) (
        (int) (pos.x / CELL_WIDTH),
        (int) (pos.y / CELL_WIDTH),
        (int) (pos.z / CELL_WIDTH)
    );
    // MUST CLAMP or else a bunch of bugs occur (id out of bounds, reading random areas of memory, etc)
    // often particles end up right on the boundary, 1 over the max allowed coord
    cellCoords.x = clamp( cellCoords.x, 0, (int) CELLCOUNT_X - 1 );
    cellCoords.y = clamp( cellCoords.y, 0, (int) CELLCOUNT_Y - 1 );
    cellCoords.z = clamp( cellCoords.z, 0, (int) CELLCOUNT_Z - 1 );
    return cell_coords_2_id(cellCoords);
}

void atomic_add_global_float(volatile global float *source, const float operand) {
    union {
        unsigned int intVal;
        float floatVal;
    } newVal;
    union {
        unsigned int intVal;
        float floatVal;
    } prevVal;
 
    do {
        prevVal.floatVal = *source;
        newVal.floatVal = prevVal.floatVal + operand;
    } while (atomic_cmpxchg((volatile global unsigned int *)source, prevVal.intVal, newVal.intVal) != prevVal.intVal);
}



#define MAX_VEL 5



kernel void predict_positions(
    float delta,
    global float *posBuffer,
    global float *velBuffer,
    global float *eposBuffer,
    global int *phase,
    float gravityX,
    float gravityY,
    float gravityZ
) {
    int i = get_global_id(0);
    // if (phase[i] == PHASE_STATIC) return;
    
    float3 pos  = getVec( posBuffer, i);
    float3 vel  = getVec( velBuffer, i);
    
    vel.x += gravityX * delta;
    vel.y += gravityY * delta;
    vel.z += gravityZ * delta;
    float3 epos = pos + vel * delta;
    
    setVec( velBuffer, i,  vel);
    setVec(eposBuffer, i, epos);
}


kernel void assign_particle_cells (
    global float *eposBuffer,
    global int *numParticlesPerCell,
    global int *cellIDsOfParticles,
    global int *particleIDinCell
) {
    int i = get_global_id(0);
    float3 epos = getVec(eposBuffer, i);
    int cellID = get_cell_id(epos);
    cellIDsOfParticles[i] = cellID;
    particleIDinCell[i] = atomic_inc( &numParticlesPerCell[cellID] );
}


kernel void find_cells_start_and_end (
    global int *numParticlesPerCell,
    global int *cellStartAndEndIDs
) {
    int cellID = get_global_id(0);
    
    int startPos = 0;
    for (int i = 0; i < cellID; i++) {
        startPos += numParticlesPerCell[i];
    }
    int endPos = startPos + numParticlesPerCell[cellID];

    cellStartAndEndIDs[cellID * 2 + 0] = startPos;
    cellStartAndEndIDs[cellID * 2 + 1] = endPos;
}

kernel void sort_particle_ids_by_cell (
    global int *particleIDinCell,
    global int *cellStartAndEndIDs,
    global int *cellIDsOfParticles,
    global int *sortedParticleIDs
) {
    
    int i = get_global_id(0);
    int cellID = cellIDsOfParticles[i];
    int cellStartPos = cellStartAndEndIDs[cellID * 2 + 0];
    int idInCell = particleIDinCell[i];
    int sortedID = cellStartPos + idInCell;
    sortedParticleIDs[sortedID] = i;
}


float w_poly6(float dist, float h) {
    dist = clamp(dist, (float) 0, (float) h);
    float tmp = h * h - dist * dist;
    return ( 315.0f / (64.0f * PI * pow(h, 9)) ) * tmp * tmp * tmp;
}


float w_spikygrad(float dist, float h) {
    dist = clamp(dist, (float) 0, (float) h);
    if (dist < FLT_EPSILON) return 0; // too close = same particle = can't use this scalar kernel
    return ( -45 / (PI * pow(h, 6)) ) * (h - dist) * (h - dist);
}

kernel void compute_lambdas(
    global float *eposBuffer,
    global float *imasses,
    global float *lambdas,
    global int *cellIDsOfParticles,
    global int *cellStartAndEndIDs,
    global int *sortedParticleIDs,
    global int *phase
) {
    
    int i = get_global_id(0);
    
    if (phase[i] != PHASE_LIQUID) {
        lambdas[i] = 0;
        return;
    }
    
    float3 epos1 = getVec(eposBuffer, i);
    float density = 0;
    float  gradN = 0; // gradient sum when other particle is neighbour
    float3 gradS = 0; // gradient sum when other particle is self
    
    
    FOREACH_NEIGHBOUR_j
    
        float3 epos2 = getVec(eposBuffer, j);
        float3 diff = epos1 - epos2;
        float dist = length(diff);
        if (dist > KERNEL_SIZE) continue;
        
        // the added bit should be multiplied by an extra scalar if its a solid
        if (imasses[j] > 0) density += (1.0 / imasses[j]) * w_poly6(dist, KERNEL_SIZE);
        
        if (i != j) {
            
            float kgrad = w_spikygrad(dist, KERNEL_SIZE);
            float tmp = kgrad / REST_DENSITY;
            // the added bit should be multiplied by an extra scalar if its a solid
            gradN += tmp * tmp;
            // the added bit should be multiplied by an extra scalar if its a solid
            gradS += normalize(diff) * kgrad;
        }
        
    END_FOREACH_NEIGHBOUR_j
    
    
    gradS /= REST_DENSITY;
    float denominator = gradN + dot(gradS, gradS);
    
    lambdas[i] = -(density / REST_DENSITY - 1.0) / (denominator + RELAXATION);
    
}

kernel void compute_fluid_corrections(
    global float *eposBuffer,
    global float *imasses,
    global float *lambdas,
    global float *corrections,
    global int *cellIDsOfParticles,
    global int *cellStartAndEndIDs,
    global int *sortedParticleIDs,
    global int *phase
) {
    int i = get_global_id(0);
    
    if (phase[i] != PHASE_LIQUID) return;
    
    float3 epos1 = getVec(eposBuffer, i);
    
    float3 correction = (float3) (0, 0, 0);
    
    int numNeighbours = 1; // start at 1 to prevent divide by zero
    
    FOREACH_NEIGHBOUR_j
        
        if (i == j) continue;
        
        float3 epos2 = getVec(eposBuffer, j);
        float3 diff = epos1 - epos2;
        float dist = length(diff);
        
        if (dist > KERNEL_SIZE) continue;
        numNeighbours++;
        
        float3 grad = w_spikygrad(dist, KERNEL_SIZE) * normalize(diff);
        
        float artificialPressure = -K_P * pow( w_poly6(dist, KERNEL_SIZE) / w_poly6(DQ_P * KERNEL_SIZE, KERNEL_SIZE), E_P );
        
        correction += (lambdas[i] + lambdas[j] + artificialPressure) * grad;
        
    END_FOREACH_NEIGHBOUR_j
    
    
    correction /= REST_DENSITY;
    correction /= numNeighbours;
    
    setVec(corrections, i, correction);
    
}


kernel void compute_solid_corrections (
    global float *eposBuffer,
    global float *imasses,
    global float *corrections,
    global int *cellIDsOfParticles,
    global int *cellStartAndEndIDs,
    global int *sortedParticleIDs,
    global int *phase
) {
    int i = get_global_id(0);
    
    if (phase[i] != PHASE_SOLID) return;
    
    float3 epos1 = getVec(eposBuffer, i);
    
    float imass1 = imasses[i];
    if (imass1 == 0) return;
    
    float3 correction = (float3) (0, 0, 0);
    
    FOREACH_NEIGHBOUR_j
        
        if (i == j) continue;
        
        float3 epos2 = getVec(eposBuffer, j);
        float3 diff = epos1 - epos2;
        float dist = length(diff);
        
        float imass2 = imasses[j];
        
        if (dist < 0.2f) {
            float displacement = dist - 0.2f;
            float w = imass1 / (imass1 + imass2);
            correction -= w * displacement * normalize(diff);
        }
        
    END_FOREACH_NEIGHBOUR_j
    
    
    setVec(corrections, i, correction);
}


kernel void apply_corrections(
    global float *posBuffer,
    global float *eposBuffer,
    global float *corrections,
    global int *phase
) {
    int i = get_global_id(0);
    
    float3 correction = getVec(corrections, i);
    float3 epos = getVec(eposBuffer, i);
    epos += correction;
    setVec(eposBuffer, i, epos);
}



kernel void update_velocity(
    float delta,
    global float *posBuffer,
    global float *velBuffer,
    global float *eposBuffer,
    global int *phase,
    float shiftX
) {
    int i = get_global_id(0);
    
    float3 pos  = getVec( posBuffer, i);
    float3 vel  = getVec( velBuffer, i);
    float3 epos = getVec(eposBuffer, i);
    
    if (phase[i] == PHASE_STATIC) epos = pos;
    
    vel = (epos - pos) / delta;
    pos = epos;
    pos.x += shiftX;
    
    if (phase[i] == PHASE_LIQUID) {
        if (pos.x <     0) pos.x = MAX_X - 0.01f;
        if (pos.x > MAX_X) pos.x =     0 + 0.01f;
    }
    
    if      (pos.y <     0) {  pos.y =     0;  vel.y = fmax( (float) 0, (float) vel.y);  }
    else if (pos.y > MAX_Y) {  pos.y = MAX_Y;  vel.y = fmin( (float) 0, (float) vel.y);  }
    
    if      (pos.z <     0) {  pos.z =     0;  vel.z = fmax( (float) 0, (float) vel.z);  }
    else if (pos.z > MAX_Z) {  pos.z = MAX_Z;  vel.z = fmin( (float) 0, (float) vel.z);  }
    
    if (length(vel) > MAX_VEL) vel = normalize(vel) * MAX_VEL;
    
    setVec( posBuffer, i,  pos);
    setVec( velBuffer, i,  vel);
    setVec(eposBuffer, i, epos);
}


kernel void compute_vorticity (
    global float *posBuffer,
    global float *velBuffer,
    global float *vorticities,
    global int *cellIDsOfParticles,
    global int *cellStartAndEndIDs,
    global int *sortedParticleIDs,
    global int *phase
) {
    
    int i = get_global_id(0);
    
    if (phase[i] != PHASE_LIQUID) {
        setVec(vorticities, i, (float3) (0, 0, 0) );
        return;
    }
    
    float3 pos = getVec(posBuffer, i);
    float3 vel = getVec(velBuffer, i);
    
    float3 vorticity = (float3) (0, 0, 0);
        
    FOREACH_NEIGHBOUR_j
        float3 velDiff = getVec(velBuffer, j) - vel;
        float3 posDiff = pos - getVec(posBuffer, j);
        float3 grad = w_spikygrad( length(posDiff), KERNEL_SIZE ) * normalize(posDiff);
        
        vorticity += cross(velDiff, grad);
        
    END_FOREACH_NEIGHBOUR_j
    
    setVec(vorticities, i, vorticity);
}


kernel void apply_vorticity_viscosity (
    global float *posBuffer,
    global float *velBuffer,
    global float *vorticities,
    global float *velCorrect,
    global float *imasses,
    float delta,
    global int *cellIDsOfParticles,
    global int *cellStartAndEndIDs,
    global int *sortedParticleIDs,
    global int *phase
) {
    
    int i = get_global_id(0);
    
    if (phase[i] != PHASE_LIQUID) {
        setVec(velCorrect, i, (float3) (0, 0, 0) );
        return;
    }
    
    float3 pos = getVec(posBuffer, i);
    float3 vel = getVec(velBuffer, i);
    float3 vort_i = getVec(vorticities, i);
    
    // gradient direction of the magnitude of vorticities around this point (scalar field)
    // gradient of a function is calculated by summing function values multiplied by spikygrad
    float3 vortMagGrad = (float3) (0, 0, 0);
    
    float3 avgNeighbourVelDiff = (float3) (0, 0, 0);
    
    FOREACH_NEIGHBOUR_j
        
        float3 velDiff = getVec(velBuffer, j) - vel;
        float3 posDiff = pos - getVec(posBuffer, j);
        float3 vort_j = getVec(vorticities, j);
        vortMagGrad += length(vort_j) * w_spikygrad( length(posDiff), KERNEL_SIZE ) * normalize(posDiff);
        
        avgNeighbourVelDiff += velDiff * w_poly6( length(posDiff), KERNEL_SIZE );
        
    END_FOREACH_NEIGHBOUR_j
    
    float3 vorticity_force = RELAXATION * cross( normalize(vortMagGrad), vort_i );
    
    float3 correction = 
                        (vorticity_force * delta * imasses[i]) +
                        (avgNeighbourVelDiff * VISCOSITY_COEFF);
    
    setVec(velCorrect, i, correction);
}


kernel void correct_fluid_velocity(
    global float *velBuffer,
    global float *velCorrect
) {
    
    int i = get_global_id(0);
    float3 vel = getVec(velBuffer, i);
    float3 correction = getVec(velCorrect, i);
    vel += correction;
    setVec(velBuffer, i, vel);
}




