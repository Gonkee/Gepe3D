#version 330

uniform sampler2D screenTexture;

out vec4 FragColor;

in vec2 texUV;

const float offset = 1.0 / 100.0;  

void main()
{
    
    vec2 offsets[9] = vec2[](
        vec2(-offset,  offset), // top-left
        vec2( 0.0,    offset), // top-center
        vec2( offset,  offset), // top-right
        vec2(-offset,  0.0),   // center-left
        vec2( 0.0,    0.0),   // center-center
        vec2( offset,  0.0),   // center-right
        vec2(-offset, -offset), // bottom-left
        vec2( 0.0,   -offset), // bottom-center
        vec2( offset, -offset)  // bottom-right    
    );

    float kernel[9] = float[](
        -1, -1, -1,
        -1,  9, -1,
        -1, -1, -1
    );
    
    vec3 sampleTex[9];
    for(int i = 0; i < 9; i++)
    {
        sampleTex[i] = vec3(texture2D(screenTexture, texUV + offsets[i]));
    }
    vec3 col = vec3(0.0);
    for(int i = 0; i < 9; i++)
        col += sampleTex[i] * kernel[i];
    
    // vec4 color = texture2D(screenTexture, texUV);
    // vec4 color = vec4(1, 1, 0, 1);
    
    // if ( length(color.xyz) < 0.5) discard;
    
    vec3 ting = 1 - col.xyz;
    vec3 tingProcessed = ting * ting * (3 - 2 * ting);
    
    FragColor = vec4(tingProcessed, 1);
}