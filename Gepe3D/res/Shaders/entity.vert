#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec3 instancePosition;


// uniform mat4 modelMatrix;
uniform mat4 cameraMatrix;
// uniform mat3 normalMatrix;

out vec3 fragNormal;
out vec3 fragPos;

void main()
{
    gl_Position = cameraMatrix * vec4(vertexPosition + instancePosition, 1.0);
    // fragNormal = normalMatrix * normal;
    fragNormal = normal;
    // fragPos = vec3( modelMatrix * vec4(vertexPosition, 1.0) );
    fragPos = vertexPosition;
}