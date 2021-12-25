

__kernel void move_particles(__global float *posData) {
 
    // Get the index of the current element to be processed
    int i = get_global_id(0);
 
    // Do the operation
    posData[i * 3 + 0] = posData[i * 3 + 0] + 0.1f;
    posData[i * 3 + 1] = posData[i * 3 + 1] + 0.1f;
    posData[i * 3 + 2] = posData[i * 3 + 2] + 0.1f;
}