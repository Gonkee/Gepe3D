#version 330

uniform sampler2D screenTexture;

out vec4 FragColor;

in vec2 texUV;

void main()
{
    
    vec4 color = texture2D(screenTexture, texUV);
    
    if ( length(color.xyz) < 0.5) discard;
    
    FragColor = vec4(color.xyz, 1);
}