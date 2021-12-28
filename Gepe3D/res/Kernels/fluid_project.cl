
#define PI 3.1415926f

// Epsilon in gamma correction denominator
#define RELAXATION 0.01f

// Pressure terms
#define K_P  0.1f
#define E_P  4.0f
#define DQ_P 0.2f

#define VISCOSITY_COEFF 0.001f

float w_poly6(float dist, float h) {
    dist = clamp(dist, (float) 0, (float) h);
    float tmp = h * h - dist * dist;
    return ( 315.0f / (64.0f * PI * pow(h, 9)) ) * tmp * tmp * tmp;
}


// TODO: neighbour finding so it only loops through ~50 particles instead of 3000
// TODO: make spiky grad func return a float3 instead (more convenient)

float w_spikygrad(float dist, float h) {
    dist = clamp(dist, (float) 0, (float) h);
    if (dist < FLT_EPSILON) return 0; // too close = same particle = can't use this scalar kernel
    return ( -45 / (PI * pow(h, 6)) ) * (h - dist) * (h - dist);
}



kernel void calculate_lambdas(    global float *eposBuffer,   // 0
                                  global float *imasses,      // 1
                                  global float *lambdas,      // 2
                                  float kernelSize,           // 3
                                  float restDensity           // 4
) {
    int i = get_global_id(0);
    float3 epos1 = getVec(eposBuffer, i);
    
    float density = 0;
    float  gradN = 0; // gradient sum when other particle is neighbour
    float3 gradS = 0; // gradient sum when other particle is self
    
    for (int j = 0; j < get_global_size(0); j++) {
        
        float3 epos2 = getVec(eposBuffer, j);
        float3 diff = epos1 - epos2;
        float dist = length(diff);
        if (dist > kernelSize) continue;
        
        // the added bit should be multiplied by an extra scalar if its a solid
        if (imasses[j] > 0) density += (1.0 / imasses[j]) * w_poly6(dist, kernelSize);
        
        if (i != j) {
            
            float kgrad = w_spikygrad(dist, kernelSize);
            float tmp = kgrad / restDensity;
            // the added bit should be multiplied by an extra scalar if its a solid
            gradN += tmp * tmp;
            // the added bit should be multiplied by an extra scalar if its a solid
            gradS += normalize(diff) * kgrad;
        }
    }
    gradS /= restDensity;
    float denominator = gradN + dot(gradS, gradS);
    
    lambdas[i] = -(density / restDensity - 1.0) / (denominator + RELAXATION);
}


kernel void add_lambdas(global float *eposBuffer,   // 0
                        global float *imasses,      // 1
                        global float *lambdas,      // 2
                        float kernelSize,           // 3
                        float restDensity,          // 4
                        global float *corrections   //5
) {
    int i = get_global_id(0);
    float3 epos1 = getVec(eposBuffer, i);
    
    float3 correction = (float3) (0, 0, 0);
    
    int numNeighbours = 1; // start at 1 to prevent divide by zero
    
    for (int j = 0; j < get_global_size(0); j++) {
        
        if (i == j) continue;
        
        float3 epos2 = getVec(eposBuffer, j);
        float3 diff = epos1 - epos2;
        float dist = length(diff);
        
        if (dist > kernelSize) continue;
        numNeighbours++;
        
        float3 grad = w_spikygrad(dist, kernelSize) * normalize(diff);
        
        float artificialPressure = -K_P * pow( w_poly6(dist, kernelSize) / w_poly6(DQ_P * kernelSize, kernelSize), E_P );
        
        correction += (lambdas[i] + lambdas[j] + artificialPressure) * grad;
    }
    
    correction /= restDensity;
    correction /= numNeighbours;
    
    setVec(corrections, i, correction);
    
}

kernel void correct_fluid_positions(global float *eposBuffer, global float *corrections) {
    
    int i = get_global_id(0);
    float3 epos = getVec(eposBuffer, i);
    float3 correction = getVec(corrections, i);
    epos += correction;
    setVec(eposBuffer, i, epos);
}

kernel void calculate_vorticities (global float *posBuffer, global float *velBuffer, global float *vorticities, float kernelSize) {
    
    int i = get_global_id(0);
    float3 pos = getVec(posBuffer, i);
    float3 vel = getVec(velBuffer, i);
    
    float3 vorticity = (float3) (0, 0, 0);
        
    for (int j = 0; j < get_global_size(0); j++) {
        
        float3 velDiff = getVec(velBuffer, j) - vel;
        float3 posDiff = pos - getVec(posBuffer, j);
        float3 grad = w_spikygrad( length(posDiff), kernelSize ) * normalize(posDiff);
        
        vorticity += cross(velDiff, grad);
    }
    
    setVec(vorticities, i, vorticity);
}


kernel void apply_vorticity_viscosity (global float *posBuffer, global float *velBuffer, global float *vorticities, global float *velCorrect, global float *imasses,
                                        float kernelSize, float restDensity, float delta) {
    
    int i = get_global_id(0);
    float3 pos = getVec(posBuffer, i);
    float3 vel = getVec(velBuffer, i);
    float3 vort_i = getVec(vorticities, i);
    
    // gradient direction of the magnitude of vorticities around this point (scalar field)
    // gradient of a function is calculated by summing function values multiplied by spikygrad
    float3 vortMagGrad = (float3) (0, 0, 0);
    
    float3 avgNeighbourVelDiff = (float3) (0, 0, 0);
    
    for (int j = 0; j < get_global_size(0); j++) {
        
        
        float3 velDiff = getVec(velBuffer, j) - vel;
        float3 posDiff = pos - getVec(posBuffer, j);
        float3 vort_j = getVec(vorticities, j);
        vortMagGrad += length(vort_j) * w_spikygrad( length(posDiff), kernelSize ) * normalize(posDiff);
        
        avgNeighbourVelDiff += velDiff * w_poly6( length(posDiff), kernelSize );
        
    }
    
    float3 vorticity_force = RELAXATION * cross( normalize(vortMagGrad), vort_i );
    
    float3 correction = 
                        (vorticity_force * delta * imasses[i]) +
                        (avgNeighbourVelDiff * VISCOSITY_COEFF);
    
    setVec(velCorrect, i, correction);
}


kernel void correct_fluid_vel(global float *velBuffer, global float *velCorrect) {
    
    int i = get_global_id(0);
    float3 vel = getVec(velBuffer, i);
    float3 correction = getVec(velCorrect, i);
    vel += correction;
    setVec(velBuffer, i, vel);
}