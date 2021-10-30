#version 330

uniform sampler2D screenTexture;

out vec4 FragColor;

in vec2 texUV;

float filterRadius = 20;

void main()
{
    
    float depth = texture2D(screenTexture, texUV).x;
    float sum = 0;
    float wsum = 0;
    for (float x = -filterRadius; x <= filterRadius; x += 1.0) {
        
        float sample = texture2D( screenTexture, texUV + x * blurDir ).x;
        
        // spatial domain
        float r = x * blurScale;
        float w = exp( -r * r );
        
        // range domain
        float r2 = (sample - depth) * blurDepthFalloff;
        float g = exp( -r2 * r2 );
        sum += sample * w * g;
        wsum += w * g;
    }
    if (wsum > 0.0) {
        sum /= wsum;
    }
    
    vec3 ting = vec3(sum, sum, sum);
    
    FragColor = vec4(ting, 1);
}