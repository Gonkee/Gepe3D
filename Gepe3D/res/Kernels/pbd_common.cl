
// #define variables added in the C# code - don't use these as var names
// CELLCOUNT_X, CELLCOUNT_Y, CELLCOUNT_Z, CELL_WIDTH
// MAX_X, MAX_Y, MAX_Z
// KERNEL_SIZE, REST_DENSITY
// PHASE_LIQUID, PHASE_SOLID, PHASE_STATIC

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



kernel void correct_predictions(
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