#version 330

uniform sampler2D screenTexture;

uniform mat4 projectionMatrix; // TODO: calculate inverse in CPU code
uniform mat4 viewMatrix;

uniform int drawStyle;
uniform vec3 lightPos;
uniform vec3 viewPos;
uniform vec3 ambientLight;
uniform vec3 fillColor;

vec3 lightColor = vec3(1.0, 1.0, 1.0);

out vec4 FragColor;

in vec2 texUV;

const float offset = 1.0 / 100.0;  


// only had one texture come out of the normal shader, with 4 values xyzw, hence position needs to be found from a single depth value
// TODO: make it output two textures (pos and normal) so this repetition of the same function in the normal shader is not needed
vec3 sampleViewSpaceCoords(sampler2D screenTexture, vec2 texCoord)
{
    float depth = texture2D(screenTexture, texCoord).w; // different from normal shader's one
    
    float x = texCoord.x * 2.0 - 1.0;
    float y = texCoord.y * 2.0 - 1.0;
    float z = depth * 2.0 - 1.0;
    vec4 clipPos = vec4(x, y, z, 1);
    
    vec4 viewPos = inverse(projectionMatrix) * clipPos;
    return viewPos.xyz / viewPos.w;
}


// TODO: don't copy code from the entity shader, have it in one file and reuse
vec3 diffuseColor(vec3 lightColor, vec3 normal, vec3 lightDirection)
{
    float intensity = max( dot(normal, lightDirection), 0.0 );
    return lightColor * intensity;
}

vec3 specularColor(vec3 lightColor, vec3 reflectDirection, vec3 viewDirection)
{
    float specularStrength = 0.5;
    float intensity = pow( max( dot(viewDirection, reflectDirection), 0.0 ), 32 );
    return specularStrength * intensity * lightColor;  
}

void main()
{
    vec3 pos = sampleViewSpaceCoords(screenTexture, texUV);
    vec3 normal = texture2D(screenTexture, texUV).xyz;
    
    // view space
    vec3 vLightPos = ( viewMatrix * vec4(lightPos, 1.0) ).xyz;
    vec3 lightDirection = normalize(vLightPos - pos);
    vec3 viewDirection = normalize( -pos );
    vec3 reflectDirection = reflect(-lightDirection, normal);
    
    vec3 diffuse  = diffuseColor(lightColor, normal, lightDirection);
    vec3 specular = specularColor(lightColor, reflectDirection, viewDirection);
    vec3 color = (ambientLight + diffuse + specular) * fillColor;
    
    FragColor = vec4(color, 1);
}