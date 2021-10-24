#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec3 instancePosition;


// uniform mat4 cameraMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

out vec3 fragNormal;
out vec3 fragPos;
out vec4 instanceAlbedo;
out mat4 fragViewMatrix;

void main()
{
    
    mat4 model = mat4(1.0, 0.0, 0.0, 0.0,  // 1. column
                      0.0, 1.0, 0.0, 0.0,  // 2. column
                      0.0, 0.0, 1.0, 0.0,  // 3. column
                      instancePosition.x, instancePosition.y, instancePosition.z, 1.0); // 4. column
    
    mat4 viewModelMat = viewMatrix * model;
    
    // viewModelMat[3][0] += instancePosition.x;
    // viewModelMat[3][1] += instancePosition.y;
    // viewModelMat[3][2] += instancePosition.z;
    
    // Column 0:
    viewModelMat[0][0] = 1;
    viewModelMat[0][1] = 0;
    viewModelMat[0][2] = 0;

    // Column 1:
    viewModelMat[1][0] = 0;
    viewModelMat[1][1] = 1;
    viewModelMat[1][2] = 0;

    // Column 2:
    viewModelMat[2][0] = 0;
    viewModelMat[2][1] = 0;
    viewModelMat[2][2] = 1;
    
    vec4 pos = projectionMatrix * viewModelMat * vec4(vertexPosition, 1.0);
    // pos += vec4(vertexPosition, 0);
    
    gl_Position = pos;
    fragNormal = normal;
    fragPos = normalize(vertexPosition) * sqrt(2); // square from (-1, -1) to (1, 1)
    
    if (gl_InstanceID % 3 == 0) instanceAlbedo = vec4(1, 0, 0, 1);
    if (gl_InstanceID % 3 == 1) instanceAlbedo = vec4(0, 1, 0, 1);
    if (gl_InstanceID % 3 == 2) instanceAlbedo = vec4(0, 0, 1, 1);
    
    fragViewMatrix = viewMatrix;
}