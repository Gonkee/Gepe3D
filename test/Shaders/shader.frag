#version 330

out vec4 FragColor;

// 0 = fill
// 1 = line
// 2 = point
uniform int drawStyle;
uniform vec3 lightPos;
vec3 lightColor = vec3(1.0, 1.0, 1.0);
uniform vec3 viewPos;
uniform vec3 ambientLight;

vec4 fillColor  = vec4(1.0, 1.0, 1.0, 1.0);
vec4 lineColor  = vec4(0.0, 0.0, 0.0, 1.0);
vec4 pointColor = vec4(1.0, 0.0, 0.0, 1.0);

in vec3 fragNormal;
in vec3 fragPos;


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

        vec3 color = (ambientLight + diffuse + specular) * vec3(fillColor);
        FragColor = vec4(color, 1.0);
    }
}

