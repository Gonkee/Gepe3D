#version 330

uniform sampler2D screenTexture;
uniform float particleRadius;
uniform bool blurXaxis;

uniform float screenWidth;
uniform float screenHeight;

out vec4 FragColor;

in vec2 texUV;

float gaussianDistribution(float input, float standardDeviation)
{
    float inSquared = input * input;
    float sdSquared = standardDeviation * standardDeviation;
    
    // not normalized so that max output is 1, but it doesn't matter
    return exp( -( inSquared / sdSquared / 2 ) );
}

float linearizeDepth(float depth)
{
    float near = 0.01;
    float far = 500;
    
    float zVal = (2 * near * far) / (far + near - (depth * 2 - 1) * (far - near));
    return zVal;
}

void main()
{
    float depth = texture2D(screenTexture, texUV).x;
    float offsetSize = blurXaxis ? 1.0 / screenWidth : 1.0 / screenHeight;
    float filterRadius = 0.2 / depth;
    filterRadius = clamp(filterRadius, 0, 50 * offsetSize);
    int samplesEachSide = int( filterRadius / offsetSize / 2 );
    
    float spaceSD = filterRadius / 2.0;
    float valueSD = 0.0003;
    // valueSD = mix(valueSD, 0.01, 0.999);
    
    float valueSum = 0;
    float weightSum = 0;
    
    for ( int i = -samplesEachSide; i <= samplesEachSide ; i++ )
    {
        float coordOffset = i * offsetSize;
        
        float sample = blurXaxis ?
            texture2D( screenTexture, texUV + vec2(coordOffset, 0) ).x :
            texture2D( screenTexture, texUV + vec2(0, coordOffset) ).x;
        
        if (sample == 0) continue;
        
        float valueDiff = abs(sample - depth);
        float spaceDiff = coordOffset;
        
        float valueWeight = gaussianDistribution(valueDiff, valueSD);
        float spaceWeight = gaussianDistribution(spaceDiff, spaceSD);
        
        valueSum += sample * valueWeight * spaceWeight;
        weightSum += valueWeight * spaceWeight;
    }
    
    float finalDepth = weightSum > 0 ? valueSum / weightSum : depth;
    
    FragColor = vec4( finalDepth, 0, 0, 1 );
}