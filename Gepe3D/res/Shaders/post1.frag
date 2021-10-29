#version 330

uniform sampler2D screenTexture;

uniform mat4 clip2viewMat;

out vec4 FragColor;

in vec2 texUV;


vec3 toViewSpace(vec2 texCoord, float depth)
{
    return ( clip2viewMat * vec4(texCoord.x, texCoord.y, depth, 1) ).xyz;
}

void main()
{
    
    float depth = texture2D(screenTexture, texUV).x;
    
    vec3 pos = toViewSpace(texUV, depth);
    
    FragColor = vec4(pos.z, pos.z, pos.z, 1);
}