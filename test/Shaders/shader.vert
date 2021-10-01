#version 330

layout(location = 0) in vec3 vertexPosition;

uniform mat4 modelMatrix;
uniform mat4 cameraMatrix;

void main()
{
    gl_Position = cameraMatrix * modelMatrix * vec4(vertexPosition, 1.0);
}