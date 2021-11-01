#version 330

uniform sampler2D screenTexture;
uniform float particleRadius;
uniform bool blurXaxis;

out vec4 FragColor;

in vec2 texUV;

int sampleResolution = 40;

float gaussianDistribution(float input, float standardDeviation)
{
    float inSquared = input * input;
    float sdSquared = standardDeviation * standardDeviation;
    
    // not normalized so that max output is 1, but it doesn't matter
    return exp( -( inSquared / sdSquared / 2 ) );
}

float sampleFiltered(sampler2D screenTexture, vec2 texUV, float unfilteredValue, int channel, float filterRadius, float valueSD, float spaceSD)
{
    float valueSum = 0;
    float weightSum = 0;
    
    for ( int i = -sampleResolution / 2 ; i <= sampleResolution / 2 ; i++ )
    {
        float coordOffset = filterRadius * i / sampleResolution;
        
        vec4 sampleVec = blurXaxis ?
            texture2D( screenTexture, texUV + vec2(coordOffset, 0) ) :
            texture2D( screenTexture, texUV + vec2(0, coordOffset) );
        
        float sample;
        if (channel == 0) sample = sampleVec.x;
        if (channel == 1) sample = sampleVec.y;
        if (channel == 2) sample = sampleVec.z;
        if (channel == 3) sample = sampleVec.w;
        
        float valueDiff = abs(sample - unfilteredValue);
        float spaceDiff = coordOffset;
        
        float valueWeight = gaussianDistribution(valueDiff, valueSD);
        float spaceWeight = gaussianDistribution(spaceDiff, spaceSD);
        
        valueSum += sample * valueWeight * spaceWeight;
        weightSum += valueWeight * spaceWeight;
    }
    
    float finalValue = weightSum > 0 ? valueSum / weightSum : valueSum;
    return finalValue;
}


void main()
{
    vec3 unfilteredSample = texture2D(screenTexture, texUV).xyz;
    
    float depth = unfilteredSample.x;
    float filterRadius = 0.2 / depth;
    filterRadius = 0.050625;
    // filterRadius = 0;
    
    float spaceSD = filterRadius / 2.0;
    float valueSD = particleRadius * 3.0;
    
    vec3 col = vec3(
        sampleFiltered(screenTexture, texUV, unfilteredSample.x, 0, filterRadius, valueSD, spaceSD),
        sampleFiltered(screenTexture, texUV, unfilteredSample.y, 1, filterRadius, valueSD, spaceSD),
        sampleFiltered(screenTexture, texUV, unfilteredSample.z, 2, filterRadius, valueSD, spaceSD)
    );
    
    FragColor = vec4( col, 1 );
}