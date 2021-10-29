#version 330

uniform vec3 lightPos;
uniform float sphereRadius;

out vec4 FragColor;

in vec3 fragNormal;
in vec3 localSpacePos;
in vec3 viewSpacePos;
in vec4 instanceAlbedo;
in vec4 tempPos;

uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
    float d2 = localSpacePos.x * localSpacePos.x + localSpacePos.y * localSpacePos.y;
    
    if (d2 > 1) discard;
    
    // view space
    vec4 vLightPos = viewMatrix * vec4(lightPos, 1.0);
    vec4 vFragPos = viewMatrix * vec4(localSpacePos, 1.0);
    vec3 lightDir = normalize( vLightPos.xyz - vFragPos.xyz );
    
    float nx = localSpacePos.x;
    float ny = localSpacePos.y;
    float nz = sqrt(1.0 - d2);
    vec3 normal = vec3(nx, ny, nz);
    
    float NdL = max( 0.0, dot(normal, lightDir) );
    
    // clip space
    vec4 cFragPos = projectionMatrix * vec4( viewSpacePos + normal * sphereRadius, 1 );
    float depth = cFragPos.z / cFragPos.w;
    // float depth = tempPos.z / tempPos.w;
    
    
    FragColor = vec4(depth, depth, depth, 1);
}