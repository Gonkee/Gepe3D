#version 330

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 vertexColor;

uniform mat4 modelMatrix;
uniform mat4 cameraMatrix;

out vec3 aColor;

void main()
{
    gl_Position = cameraMatrix * modelMatrix * vec4(vertexPosition, 1.0);
    aColor = vertexColor;
}