
// #define variables added in the C# code - don't use these as var names
// CELLCOUNT_X, CELLCOUNT_Y, CELLCOUNT_Z, CELL_WIDTH
// MAX_X, MAX_Y, MAX_Z
// KERNEL_SIZE, REST_DENSITY

float3 getVec(float *buffer, int i) {
    return (float3) ( buffer[i * 3 + 0], buffer[i * 3 + 1], buffer[i * 3 + 2] );
}

void setVec(float *buffer, int i, float3 val) {
    buffer[i * 3 + 0] = val.x;
    buffer[i * 3 + 1] = val.y;
    buffer[i * 3 + 2] = val.z;
}


int3 cell_id_2_coords(int id) {
    
    int x =   id / (CELLCOUNT_Y * CELLCOUNT_Z);
    int y = ( id % (CELLCOUNT_Y * CELLCOUNT_Z) ) / CELLCOUNT_Z;
    int z = ( id % (CELLCOUNT_Y * CELLCOUNT_Z) ) % CELLCOUNT_Z;
    return (int3) (x, y, z);
}

int cell_coords_2_id(int3 coords) {
    
    return
        coords.x * CELLCOUNT_Y * CELLCOUNT_Z +
        coords.y * CELLCOUNT_Z + 
        coords.z;
}

int get_cell_id(float3 pos) {
    int3 cellCoords = (int3) (
        (int) (pos.x / CELL_WIDTH),
        (int) (pos.y / CELL_WIDTH),
        (int) (pos.z / CELL_WIDTH)
    );
    // MUST CLAMP or else a bunch of bugs occur (id out of bounds, reading random areas of memory, etc)
    // often particles end up right on the boundary, 1 over the max allowed coord
    cellCoords.x = clamp( cellCoords.x, 0, (int) CELLCOUNT_X - 1 );
    cellCoords.y = clamp( cellCoords.y, 0, (int) CELLCOUNT_Y - 1 );
    cellCoords.z = clamp( cellCoords.z, 0, (int) CELLCOUNT_Z - 1 );
    return cell_coords_2_id(cellCoords);
}

int getNeighbourCells (int cellID, int *neighbourCellIDs) {
    int3 cellCoords = cell_id_2_coords(cellID);
    int neighbourCellCount = 0;
    for ( int cx = max( cellCoords.x - 1, 0 ); cx <= min( cellCoords.x + 1, CELLCOUNT_X ); cx++ ) {
    for ( int cy = max( cellCoords.y - 1, 0 ); cy <= min( cellCoords.y + 1, CELLCOUNT_Y ); cy++ ) {
    for ( int cz = max( cellCoords.z - 1, 0 ); cz <= min( cellCoords.z + 1, CELLCOUNT_Z ); cz++ ) {
        neighbourCellIDs[ neighbourCellCount++ ] = cell_coords_2_id( (int3) (cx, cy, cz));
    }}}
    return neighbourCellCount;
}