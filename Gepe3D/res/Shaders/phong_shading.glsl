#version 330

struct Light
{
    vec3 position;
    vec3 color;
}


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

vec3 computeShading(vec3 albedoColor, vec3 fragPos, vec3 cameraPos, )
{
    
}


void main()
{
    if (drawStyle == 1) { FragColor = lineColor; }
    if (drawStyle == 2) { FragColor = pointColor; }
    if (drawStyle == 0)
    {
        vec3 normal = normalize(fragNormal);
        vec3 lightDirection = normalize(lightPos - fragPos);
        vec3 viewDirection = normalize(viewPos - fragPos);
        vec3 reflectDirection = reflect(-lightDirection, normal);

        vec3 diffuse  = diffuseColor(lightColor, normal, lightDirection);
        vec3 specular = specularColor(lightColor, reflectDirection, viewDirection);

        vec3 color = (ambientLight + diffuse + specular) * fillColor;
        FragColor = vec4(color, 1.0);
    }
}

