#version 330

uniform sampler2D screenTexture;

uniform mat4 clip2viewMat;

out vec4 FragColor;

in vec2 texUV;

float normalSampleOffset = 0.001;

void main()
{
    // FBO pixel internal format must NOT clamp between [0, 1], i.e. RGB16F or RGB32F etc
    // sample the position at the pixel and the positions around it to figure out the normal at that position
    vec3 pos  = texture2D( screenTexture, texUV ).xyz;
    vec3 rPos = texture2D( screenTexture, texUV + vec2(  normalSampleOffset, 0 ) ).xyz;
    vec3 lPos = texture2D( screenTexture, texUV + vec2( -normalSampleOffset, 0 ) ).xyz;
    vec3 tPos = texture2D( screenTexture, texUV + vec2( 0, -normalSampleOffset ) ).xyz;
    vec3 bPos = texture2D( screenTexture, texUV + vec2( 0,  normalSampleOffset ) ).xyz;
    
    vec3 rDiff = rPos - pos;
    vec3 lDiff = pos - lPos;
    vec3 tDiff = tPos - pos;
    vec3 bDiff = pos - bPos;
    
    // pick the difference vector with smaller Z difference in order to prevent wonky normals at the edges
    vec3 xDiff = abs(rDiff.z) < abs(lDiff.z) ? rDiff : lDiff;
    vec3 yDiff = abs(tDiff.z) < abs(bDiff.z) ? tDiff : bDiff;
    
    vec3 normal = normalize( cross(yDiff, xDiff) );
    FragColor = vec4( mix(normal, texture2D( screenTexture, texUV ).xyz, 0.01), 1);
}