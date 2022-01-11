#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 instancePosition;
layout(location = 2) in vec3 instanceColour;

uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

out vec3 texCoords;
out vec3 viewSpaceSphereCenter;
out vec4 instanceAlbedo;
out float xPos;

void main()
{
    mat4 model = mat4(1.0, 0.0, 0.0, 0.0,  // 1. column
                      0.0, 1.0, 0.0, 0.0,  // 2. column
                      0.0, 0.0, 1.0, 0.0,  // 3. column
                      instancePosition.x, instancePosition.y, instancePosition.z, 1.0); // 4. column
    
    mat4 viewModelMat = viewMatrix * model;
    viewModelMat[0][0] = 1;   viewModelMat[1][0] = 0;   viewModelMat[2][0] = 0;
    viewModelMat[0][1] = 0;   viewModelMat[1][1] = 1;   viewModelMat[2][1] = 0;
    viewModelMat[0][2] = 0;   viewModelMat[1][2] = 0;   viewModelMat[2][2] = 1;

    gl_Position = projectionMatrix * viewModelMat * vec4(vertexPosition, 1.0);
    
    texCoords = normalize(vertexPosition) * sqrt(2); // square from (-1, -1) to (1, 1)
    viewSpaceSphereCenter = ( viewMatrix * vec4(instancePosition, 1.0) ).xyz;
    
    instanceAlbedo = vec4(instanceColour, 1);
    xPos = instancePosition.x;
}