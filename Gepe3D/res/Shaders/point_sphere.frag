#version 330

uniform vec3 lightPos;

out vec4 FragColor;

in vec3 fragNormal;
in vec3 fragPos;
in vec4 instanceAlbedo;
in mat4 fragViewMatrix;

void main()
{
    float d2 = fragPos.x * fragPos.x + fragPos.y * fragPos.y;
    
    if (d2 > 1) discard;
    
    vec4 lightPosTransformed = fragViewMatrix * vec4(lightPos, 1.0);
    vec4 fragPosTransformed = fragViewMatrix * vec4(fragPos, 1.0);
    vec3 lightDir = normalize( lightPosTransformed.xyz - fragPosTransformed.xyz );
    
    float nx = fragPos.x;
    float ny = fragPos.y;
    float nz = sqrt(1.0 - d2);
    vec3 normal = vec3(nx, ny, nz);
    
    
    FragColor = vec4(instanceAlbedo.xyz * dot(normal, lightDir), 1);
}