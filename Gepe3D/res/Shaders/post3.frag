#version 330

uniform sampler2D screenTexture;

out vec4 FragColor;

in vec2 texUV;

const float offset = 1.0 / 100.0;  

void main()
{
    
    vec3 ting = texture2D(screenTexture, texUV).xyz;
    
    FragColor = vec4( 1 - ting.x, ting.y, 1 - ting.z , 1);
}