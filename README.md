# Gepe3D
 Gonkee's Epic Physics Engine 3D (Gepe3D)
 
 Computational fluid dynamics + rigid bodies + soft bodies, made in C# + OpenGL + OpenCL

![fluid sim](https://github.com/Gonkee/Gepe3D/assets/41216697/e4c93a39-be19-41bf-9f24-3dfac0cfa6e3)
 
 [Click here to download ZIP file containing EXE](https://github.com/Gonkee/Gepe3D/releases/download/breh/Gepe3D_win-x64.zip)
 
 (or click the releases section on the right)

### Code snippet (update function for player controller in C#)
```c#
public void Update(float delta, KeyboardState keyboardState) {
    Vector3 camOffset = new Vector3(7, 0, 0);
    camOffset = Vector3.TransformColumn( Matrix3.CreateRotationZ( MathHelper.DegreesToRadians(pitch) ), camOffset );
    camOffset = Vector3.TransformColumn( Matrix3.CreateRotationY( MathHelper.DegreesToRadians(yaw) ), camOffset );
    
    centrePos = particleSystem.GetPos(centreParticleID);
    activeCam.SetPos(centrePos + camOffset);
    activeCam.LookAt(centrePos);
    activeCam.UpdateLocalVectors();
    
    Vector3 movement = new Vector3();
    if ( keyboardState.IsKeyDown(Keys.W)         ) { movement.X += 1; }
    if ( keyboardState.IsKeyDown(Keys.S)         ) { movement.X -= 1; }
    if ( keyboardState.IsKeyDown(Keys.A)         ) { movement.Z -= 1; }
    if ( keyboardState.IsKeyDown(Keys.D)         ) { movement.Z += 1; }
    if ( keyboardState.IsKeyDown(Keys.Space)     ) { movement.Y += 1; }
    if ( keyboardState.IsKeyDown(Keys.LeftShift) ) { movement.Y -= 1; }
    if (movement.Length != 0) movement.Normalize();

    Matrix4 rotation = Matrix4.CreateRotationY( MathHelper.DegreesToRadians(-yaw) );
    rotation.Transpose(); // OpenTK matrices are transposed by default for some reason

    Vector4 rotatedMovement = rotation * new Vector4(movement, 1);
    movement.X = -rotatedMovement.X;
    movement.Y =  rotatedMovement.Y;
    movement.Z = -rotatedMovement.Z;
    
    movement *= 0.1f;

    foreach (int pID in particleIDs) {
        particleSystem.AddVel(pID, movement.X, movement.Y, movement.Z);
    }
}
```

### Code snippet (calculation functions in OpenCL)
```opencl
float w_poly6(float dist, float h) {
    dist = clamp(dist, (float) 0, (float) h);
    float tmp = h * h - dist * dist;
    return ( 315.0f / (64.0f * PI * pow(h, 9)) ) * tmp * tmp * tmp;
}

float w_spikygrad(float dist, float h) {
    dist = clamp(dist, (float) 0, (float) h);
    if (dist < FLT_EPSILON) return 0; // too close = same particle = can't use this scalar kernel
    return ( -45 / (PI * pow(h, 6)) ) * (h - dist) * (h - dist);
}

kernel void calculate_lambdas(
    global float *eposBuffer,
    global float *imasses,
    global float *lambdas,
    global int *cellIDsOfParticles,
    global int *cellStartAndEndIDs,
    global int *sortedParticleIDs,
    global int *phase
) {
    int i = get_global_id(0);
    
    if (phase[i] != PHASE_LIQUID) {
        lambdas[i] = 0;
        return;
    }
    
    float3 epos1 = getVec(eposBuffer, i);
    float density = 0;
    float  gradN = 0; // gradient sum when other particle is neighbour
    float3 gradS = 0; // gradient sum when other particle is self
    
    FOREACH_NEIGHBOUR_j
    
        float3 epos2 = getVec(eposBuffer, j);
        float3 diff = epos1 - epos2;
        float dist = length(diff);
        if (dist > KERNEL_SIZE) continue;
        
        // the added bit should be multiplied by an extra scalar if its a solid
        if (imasses[j] > 0) density += (1.0 / imasses[j]) * w_poly6(dist, KERNEL_SIZE);
        
        if (i != j) {
            
            float kgrad = w_spikygrad(dist, KERNEL_SIZE);
            float tmp = kgrad / REST_DENSITY;
            // the added bit should be multiplied by an extra scalar if its a solid
            gradN += tmp * tmp;
            // the added bit should be multiplied by an extra scalar if its a solid
            gradS += normalize(diff) * kgrad;
        }
        
    END_FOREACH_NEIGHBOUR_j
    
    gradS /= REST_DENSITY;
    float denominator = gradN + dot(gradS, gradS);
    
    lambdas[i] = -(density / REST_DENSITY - 1.0) / (denominator + RELAXATION);
}
```
 
Keybinds:
 - W: move forward
 - S: move backward
 - A: move left
 - D: move right
 - Space: move up
 - Left Shift: move down
 - Escape: close program

References:

- [Matthias Müller et al - "Position Based Dynamics"](https://matthias-research.github.io/pages/publications/posBasedDyn.pdf)
- [Miles Macklin and Matthias Müller - "Position based fluids"](http://mmacklin.com/pbf_sig_preprint.pdf)
- [Miles Macklin et al - "Unified Particle Physics for Real-Time Applications"](https://mmacklin.com/uppfrta_preprint.pdf)
- [Simon Green - "Particle Simulation using CUDA"](http://developer.download.nvidia.com/assets/cuda/files/particles.pdf)
