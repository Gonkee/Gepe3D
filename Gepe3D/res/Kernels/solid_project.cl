
// #define variables added in the C# code - don't use these as var names
// CELLCOUNT_X, CELLCOUNT_Y, CELLCOUNT_Z, CELL_WIDTH
// MAX_X, MAX_Y, MAX_Z
// KERNEL_SIZE, REST_DENSITY
// PHASE_LIQUID, PHASE_SOLID

kernel void calc_solid_corrections (global float *eposBuffer,   // 0
                        global float *imasses,      // 1
                        global float *corrections,  // 3
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
    
    int numNeighbours = 1; // start at 1 to prevent divide by zero
    
    float3 correction = (float3) (0, 0, 0);
    
    FOREACH_NEIGHBOUR_j
        
        if (i == j) continue;
        
        float3 epos2 = getVec(eposBuffer, j);
        float3 diff = epos1 - epos2;
        float dist = length(diff);
        
        float imass2 = imasses[j];
        if (imass2 == 0) continue;
    
        
        if (dist < 0.2f) {
            float displacement = dist - 0.2f;
            float w = imass1 / (imass1 + imass2);
            correction -= w * displacement * normalize(diff);
        }
        
    END_FOREACH_NEIGHBOUR_j
    
    
    // correction /= numNeighbours;
    
    setVec(corrections, i, correction);
}


kernel void solve_dist_constraint( 
    global float *eposBuffer,
    global float *imasses,
    global float *corrections,
    global int *constraints,
    global float *distances,
    int numberOfConstraints
 ) {
    int cID = get_global_id(0);
    
    
    // for (int cID = 0; cID < numberOfConstraints; cID++ ) {

        int p1 = constraints[cID * 2 + 0];
        int p2 = constraints[cID * 2 + 1];
        float restDist = distances[cID];
        float imass1 = imasses[p1];
        float imass2 = imasses[p2];
        if (imass1 == 0 && imass2 == 0) return;

        float3 epos1 = getVec(eposBuffer, p1);
        float3 epos2 = getVec(eposBuffer, p2);
        float3 dir = epos1 - epos2;
        float displacement = length(dir) - restDist;
        dir = normalize(dir);

        float w1 = imass1 / (imass1 + imass2);
        float w2 = imass2 / (imass1 + imass2);

        // float3 correction1 = getVec(corrections, p1);
        // float3 correction2 = getVec(corrections, p2);

        float3 correction1 = -w1 * displacement * dir;
        float3 correction2 = +w2 * displacement * dir;
        
        // p1.posEstimate += correction1 * stiffnessFac;
        // p2.posEstimate += correction2 * stiffnessFac;
        
        // setVec(corrections, p1, correction1);
        // setVec(corrections, p2, correction2);
        atomic_add_global_float( &corrections[p1 * 3 + 0], correction1.x );
        atomic_add_global_float( &corrections[p1 * 3 + 1], correction1.y );
        atomic_add_global_float( &corrections[p1 * 3 + 2], correction1.z );
        
        atomic_add_global_float( &corrections[p2 * 3 + 0], correction2.x );
        atomic_add_global_float( &corrections[p2 * 3 + 1], correction2.y );
        atomic_add_global_float( &corrections[p2 * 3 + 2], correction2.z );
        
        
    // }
 }
