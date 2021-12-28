

float3 getVec(float *buffer, int i) {
    return (float3) ( buffer[i * 3 + 0], buffer[i * 3 + 1], buffer[i * 3 + 2] );
}

void setVec(float *buffer, int i, float3 val) {
    buffer[i * 3 + 0] = val.x;
    buffer[i * 3 + 1] = val.y;
    buffer[i * 3 + 2] = val.z;
}


int3 cell_id_2_coords(int id, int rowsX, int rowsY, int rowsZ) {
    
    int x = id / (rowsY * rowsZ);
    int y = ( id % (rowsY * rowsZ) ) / rowsZ;
    int z = ( id % (rowsY * rowsZ) ) % rowsZ;
    return (int3) (x, y, z);
}

int cell_coords_2_id(int3 coords, int rowsX, int rowsY, int rowsZ) {
    
    return
        coords.x * rowsY * rowsZ +
        coords.y * rowsZ + 
        coords.z;
}

int get_cell_id(float3 pos, int rowsX, int rowsY, int rowsZ, float cell_width) {
    int3 cellCoords = (int3) (
        (int) (pos.x / cell_width),
        (int) (pos.y / cell_width),
        (int) (pos.z / cell_width)
    );
    return cell_coords_2_id(cellCoords, rowsX, rowsY, rowsZ);
}