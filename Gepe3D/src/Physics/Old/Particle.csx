

using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class Particle
    {
        public int id;
        public Vector3 pos = new Vector3();
        public Vector3 vel = new Vector3();
        public Vector3 posEstimate = new Vector3();
        public float inverseMass = 1;
        public int phase = 0;
        public int constraintCount = 0;
        public bool active = false;
        public bool immovable = false;
        
        public int gridX, gridY, gridZ;
        
        public Particle(int id)
        {
            this.id = id;
        }
    }
}