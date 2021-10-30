#version 330

uniform vec3 lightPos;
uniform float sphereRadius;

out vec4 FragColor;

in vec3 texCoords;
in vec3 viewSpaceSphereCenter;
in vec4 instanceAlbedo;

uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
    float d2 = texCoords.x * texCoords.x + texCoords.y * texCoords.y;
    
    if (d2 > 1) discard;
    
    float nx = texCoords.x;
    float ny = texCoords.y;
    float nz = sqrt(1.0 - d2);
    vec3 normal = vec3(nx, ny, nz);
    
    // view space
    vec3 vLightPos = ( viewMatrix * vec4(lightPos, 1.0) ).xyz;
    vec3 vFragPos = viewSpaceSphereCenter + normal * sphereRadius;
    vec3 lightDir = normalize( vLightPos.xyz - vFragPos.xyz );
    
    float NdL = max( 0.0, dot(normal, lightDir) );
    
    vec3 col = instanceAlbedo.xyz * NdL;
    vec3 posCol = vec3(vFragPos.x, vFragPos.y, -vFragPos.z);
    
    
    FragColor = vec4( mix(col, vFragPos, 0.99) , 1);
}