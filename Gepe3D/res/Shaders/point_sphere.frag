#version 330

out vec4 FragColor;

in vec3 fragNormal;
in vec3 fragPos;

in vec4 instanceAlbedo;

void main()
{
    float d2 = fragPos.x * fragPos.x + fragPos.y * fragPos.y;
    
    if (d2 > 0.5) discard;
    
    FragColor = instanceAlbedo;
}