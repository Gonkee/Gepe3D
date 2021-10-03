#version 330

out vec4 FragColor;

// 0 = fill
// 1 = line
// 2 = point
uniform int drawStyle;

vec4 fillColor  = vec4(0.0, 1.0, 0.0, 1.0);
vec4 lineColor  = vec4(0.0, 0.0, 0.0, 1.0);
vec4 pointColor = vec4(1.0, 0.0, 0.0, 1.0);

in vec3 fragNormal;

void main()
{
    if (drawStyle == 0) { FragColor = vec4(fragNormal, 1.0); }
    if (drawStyle == 1) { FragColor = lineColor; }
    if (drawStyle == 2) { FragColor = pointColor; }
}