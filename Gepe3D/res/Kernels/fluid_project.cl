
#define PI 3.1415926f

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


kernel void calculate_densities(    global float *eposBuffer,   // 0
                                    global float *imasses,      // 1
                                    global float *densities,    // 2
                                    float kernelSize            // 3
) {
    int i = get_global_id(0);
    float3 epos1 = getVec(eposBuffer, i);
    
    float density = 0;
    for (int j = 0; j < get_global_size(0); j++) {
        float3 epos2 = getVec(eposBuffer, j);
        float imass = imasses[j];
        float dist = distance(epos1, epos2);
        density += imass * w_poly6(dist, kernelSize);
    }
}

kernel void calculate_lambdas() {
    
}

kernel void add_lambdas() {
    
}