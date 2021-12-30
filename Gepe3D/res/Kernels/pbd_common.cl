
// #define variables added in the C# code - don't use these as var names
// CELLCOUNT_X, CELLCOUNT_Y, CELLCOUNT_Z, CELL_WIDTH
// MAX_X, MAX_Y, MAX_Z
// KERNEL_SIZE, REST_DENSITY

kernel void predict_positions(         float delta,         // 0
                                global float *posBuffer,    // 1
                                global float *velBuffer,    // 2
                                global float *eposBuffer    // 3
) {
    int i = get_global_id(0);
    float3 pos  = getVec( posBuffer, i);
    float3 vel  = getVec( velBuffer, i);
    
    vel.y += -1 * delta;
    float3 epos = pos + vel * delta;
    
    setVec( velBuffer, i,  vel);
    setVec(eposBuffer, i, epos);
}


kernel void update_velocity(           float delta,         // 0
                                global float *posBuffer,    // 1
                                global float *velBuffer,    // 2
                                global float *eposBuffer    // 3
) {
    int i = get_global_id(0);
    float3 pos  = getVec( posBuffer, i);
    float3 vel  = getVec( velBuffer, i);
    float3 epos = getVec(eposBuffer, i);
    
    vel = (epos - pos) / delta;
    pos = epos;
    
    if      (pos.x <     0) {  pos.x =     0;  vel.x = fmax( (float) 0, (float) vel.x);  }
    else if (pos.x > MAX_X) {  pos.x = MAX_X;  vel.x = fmin( (float) 0, (float) vel.x);  }
    
    if      (pos.y <     0) {  pos.y =     0;  vel.y = fmax( (float) 0, (float) vel.y);  }
    else if (pos.y > MAX_Y) {  pos.y = MAX_Y;  vel.y = fmin( (float) 0, (float) vel.y);  }
    
    if      (pos.z <     0) {  pos.z =     0;  vel.z = fmax( (float) 0, (float) vel.z);  }
    else if (pos.z > MAX_Z) {  pos.z = MAX_Z;  vel.z = fmin( (float) 0, (float) vel.z);  }
    
    setVec( posBuffer, i,  pos);
    setVec( velBuffer, i,  vel);
    setVec(eposBuffer, i, epos);
}



kernel void assign_particle_cells (global float *eposBuffer,
                                    global int *numParticlesPerCell,
                                    global int *cellIDsOfParticles,
                                    global int *particleIDinCell,
                                    global float *debugOut
) {
    int i = get_global_id(0);
    float3 epos = getVec(eposBuffer, i);
    
    int cellID = get_cell_id(epos);
    
    cellIDsOfParticles[i] = cellID;
    
    
    
    
    particleIDinCell[i] = atomic_inc( &numParticlesPerCell[cellID] );
    
    // if (i == 400) {
    //     // for (int k = 0; k < 27; k++) {
    //     //     debugOut[k] = neighbourCellIDs[k];
    //     // }
    //     debugOut[0] = cellID;
    //     debugOut[1] = cellIDsOfParticles[i];
    //     debugOut[2] = particleIDinCell[i];
    // }
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


kernel void sort_particle_ids_by_cell ( global int *particleIDinCell,
                                    global int *cellStartAndEndIDs,
                                    global int *cellIDsOfParticles,
                                    global int *sortedParticleIDs,
                                     global float *debugOut
) {
    
    int i = get_global_id(0);
    int cellID = cellIDsOfParticles[i];
    int cellStartPos = cellStartAndEndIDs[cellID * 2 + 0];
    int idInCell = particleIDinCell[i];
    int sortedID = cellStartPos + idInCell;
    sortedParticleIDs[sortedID] = i;
    
    debugOut[i] = sortedID;
    
    // if (sortedID > 3000) printf("holup sortedID %d \n", sortedID);
    // if (cellID > 124) printf("holup cellID %d \n", cellID);
    
    
    // if (i == 400) {
    //     // for (int k = 0; k < 27; k++) {
    //     //     debugOut[k] = neighbourCellIDs[k];
    //     // }
    //     debugOut[3] = cellID;
    // }
}