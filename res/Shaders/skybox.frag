#version 330

out vec4 FragColor;

in vec3 fragPos;

vec4 skyTop       = vec4(0.65, 0.84, 0.95, 1.0);
vec4 skyBottom    = vec4(0.84, 0.92, 0.98, 1.0);
vec4 groundTop    = vec4(0.42, 0.40, 0.37, 1.0);
vec4 groundBottom = vec4(0.16, 0.18, 0.21, 1.0);


vec4 skyGradient(float height)
{
    if (height < 0.48)
    {
        float fac = 1 - (height / 0.48);
        fac = 1 - exp( -10 * fac );

        return mix(groundTop, groundBottom, fac);
    }
    else if (height < 0.52)
    {
        float fac = (height - 0.48) / (0.52 - 0.48);
        fac = fac * fac * (3.0 - 2.0 * fac);

        return mix(groundTop, skyBottom, fac);
    }
    else
    {
        float fac = (height - 0.52) / (1 - 0.52);
        fac = 1 - exp( -6 * fac );

        return mix(skyBottom, skyTop, fac);
    }
}


void main()
{
    vec3 normPos = normalize(fragPos); // project cube onto unit sphere
    float val = (normPos.y + 1) * 0.5;
    FragColor = skyGradient(val);
}