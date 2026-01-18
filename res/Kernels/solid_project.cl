
// #define variables added in the C# code - don't use these as var names
// CELLCOUNT_X, CELLCOUNT_Y, CELLCOUNT_Z, CELL_WIDTH
// MAX_X, MAX_Y, MAX_Z
// KERNEL_SIZE, REST_DENSITY
// PHASE_LIQUID, PHASE_SOLID, PHASE_STATIC

kernel void calc_solid_corrections (
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


