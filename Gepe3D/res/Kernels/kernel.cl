

float3 getVec(float *buffer, int i) {
    return (float3) ( buffer[i * 3 + 0], buffer[i * 3 + 1], buffer[i * 3 + 2] );
}

void setVec(float *buffer, int i, float3 val) {
    buffer[i * 3 + 0] = val.x;
    buffer[i * 3 + 1] = val.y;
    buffer[i * 3 + 2] = val.z;
}


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
                                global float *eposBuffer,   // 3
                                       float MAX_X,         // 4
                                       float MAX_Y,         // 5
                                       float MAX_Z          // 6
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



kernel void calculate_densities() {
    
}

kernel void calculate_lambdas() {
    
}

kernel void add_lambdas() {
    
}