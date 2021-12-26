

float3 getVec(float *buffer, int i) {
    return (float3) ( buffer[i * 3 + 0], buffer[i * 3 + 1], buffer[i * 3 + 2] );
}

void setVec(float *buffer, int i, float3 val) {
    buffer[i * 3 + 0] = val.x;
    buffer[i * 3 + 1] = val.y;
    buffer[i * 3 + 2] = val.z;
}