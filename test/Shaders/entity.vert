#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 normal;

uniform mat4 modelMatrix;
uniform mat4 cameraMatrix;
uniform mat3 normalMatrix;

out vec3 fragNormal;
out vec3 fragPos;

void main()
{
    gl_Position = cameraMatrix * modelMatrix * vec4(vertexPosition, 1.0);
    fragNormal = normalMatrix * normal;
    fragPos = vec3( modelMatrix * vec4(vertexPosition, 1.0) );
}