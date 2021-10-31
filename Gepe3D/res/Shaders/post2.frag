#version 330

uniform sampler2D screenTexture;
uniform float particleRadius;
uniform bool blurXaxis;

out vec4 FragColor;

in vec2 texUV;

int sampleResolution = 10;


float gaussianDistribution(float input, float standardDeviation)
{
    float inSquared = input * input;
    float sdSquared = standardDeviation * standardDeviation;
    
    // not normalized so that max output is 1, but it doesn't matter
    return exp( -( inSquared / sdSquared / 2 ) );
}


void main()
{
    
    vec3 viewSpacePos = texture2D(screenTexture, texUV).xyz;
    
    float depth = viewSpacePos.z;
    float filterRadius = 0.2 / depth;
    
    float spaceSD = filterRadius / 2.0;
    float valueSD = particleRadius * 3.0;
    
    float valueSum = 0;
    float weightSum = 0;
    
    for ( int i = -sampleResolution / 2 ; i <= sampleResolution / 2 ; i++ )
    {
        float coordOffset = filterRadius * i / sampleResolution;
        
        float sample = blurXaxis ?
            texture2D( screenTexture, texUV + vec2(coordOffset, 0) ).z :
            texture2D( screenTexture, texUV + vec2(0, coordOffset) ).z;
        
        float valueDiff = abs(sample - depth);
        float spaceDiff = coordOffset;
        
        float valueWeight = gaussianDistribution(valueDiff, valueSD);
        float spaceWeight = gaussianDistribution(spaceDiff, spaceSD);
        
        valueSum += sample * valueWeight * spaceWeight;
        weightSum += valueWeight * spaceWeight;
    }
    
    float finalDepth = valueSum / weightSum;    
    FragColor = vec4(viewSpacePos.x, viewSpacePos.y, finalDepth, 1);
}