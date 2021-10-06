#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 normal;

uniform vec3 cameraPos;
uniform mat4 cameraMatrix;

out vec3 fragPos;

void main()
{
    gl_Position = cameraMatrix * vec4(vertexPosition + cameraPos, 1.0);
    fragPos = vertexPosition;
}