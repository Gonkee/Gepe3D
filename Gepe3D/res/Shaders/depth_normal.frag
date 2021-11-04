#version 330

uniform sampler2D screenTexture;

uniform mat4 projectionMatrix; // TODO: calculate inverse in CPU code

out vec4 FragColor;

in vec2 texUV;

float normalSampleOffset = 0.001;


vec3 sampleViewSpaceCoords(sampler2D screenTexture, vec2 texCoord)
{
    float depth = texture2D(screenTexture, texCoord).x;
    
    float x = texCoord.x * 2.0 - 1.0;
    float y = texCoord.y * 2.0 - 1.0;
    float z = depth * 2.0 - 1.0;
    vec4 clipPos = vec4(x, y, z, 1);
    
    vec4 viewPos = inverse(projectionMatrix) * clipPos;
    return viewPos.xyz / viewPos.w;
}

void main()
{
    // FBO pixel internal format must NOT clamp between [0, 1], i.e. RGBA16F or RGBA32F etc
    // sample the position at the pixel and the positions around it to figure out the normal at that position
    vec3 pos  = sampleViewSpaceCoords( screenTexture, texUV );
    vec3 rPos = sampleViewSpaceCoords( screenTexture, texUV + vec2(  normalSampleOffset, 0 ) );
    vec3 lPos = sampleViewSpaceCoords( screenTexture, texUV + vec2( -normalSampleOffset, 0 ) );
    vec3 tPos = sampleViewSpaceCoords( screenTexture, texUV + vec2( 0, -normalSampleOffset ) );
    vec3 bPos = sampleViewSpaceCoords( screenTexture, texUV + vec2( 0,  normalSampleOffset ) );
    
    vec3 rDiff = rPos - pos;
    vec3 lDiff = pos - lPos;
    vec3 tDiff = tPos - pos;
    vec3 bDiff = pos - bPos;
    
    // pick the difference vector with smaller Z difference in order to prevent wonky normals at the edges
    vec3 xDiff = abs(rDiff.z) < abs(lDiff.z) ? rDiff : lDiff;
    vec3 yDiff = abs(tDiff.z) < abs(bDiff.z) ? tDiff : bDiff;
    xDiff = rPos - lPos;
    yDiff = tPos - bPos;
    
    vec3 normal = normalize( cross(yDiff, xDiff) );
    
    FragColor = vec4(normal, texture2D(screenTexture, texUV).x); // depth value stored in the w component
}