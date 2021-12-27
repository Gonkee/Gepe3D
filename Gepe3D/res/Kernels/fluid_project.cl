
#define PI 3.1415926f

// Epsilon in gamma correction denominator
#define RELAXATION 0.01f

// Pressure terms
#define K_P  0.1f
#define E_P  4.0f
#define DQ_P 0.2f

float w_poly6(float dist, float h) {
    dist = clamp(dist, (float) 0, (float) h);
    float tmp = h * h - dist * dist;
    return ( 315.0f / (64.0f * PI * pow(h, 9)) ) * tmp * tmp * tmp;
}


float3 w_spikygrad(float dist, float h) {
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
                        float restDensity           // 4
) {
    int i = get_global_id(0);
    float3 epos1 = getVec(eposBuffer, i);
    
    float3 correction = (float3) (0, 0, 0);
    
    for (int j = 0; j < get_global_size(0); j++) {
        
        if (i == j) continue;
        
        float3 epos2 = getVec(eposBuffer, j);
        float3 diff = epos1 - epos2;
        float dist = length(diff);
        
        float3 grad = w_spikygrad(dist, kernelSize) * normalize(diff);
        
        float artificialPressure = -K_P * pow( w_poly6(dist, kernelSize) / w_poly6(DQ_P * kernelSize, kernelSize), E_P );
        
        correction += (lambdas[i] + lambdas[i] + artificialPressure) * grad;
    }
    
    correction /= restDensity;
    
    
}