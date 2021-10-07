

using Gepe3D.Core;
using Microsoft.VisualBasic;

namespace Gepe3D.Physics
{
    public class PointMass
    {
        public float x, y, z;
        public float velX, velY, velZ;
        private float forceX, forceY, forceZ;

        public readonly float mass;

        public PointMass(float mass, float x, float y, float z)
        {
            this.mass = mass;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void ClearForces()
        {
            forceX = 0;
            forceY = 0;
            forceZ = 0;
        }

        public void ApplyForce(float fx, float fy, float fz)
        {
            forceX += fx;
            forceY += fy;
            forceZ += fz;
        }

        public void Update()
        {
            velX += forceX / mass * Global.Delta;
            velY += forceY / mass * Global.Delta;
            velZ += forceZ / mass * Global.Delta;

            x += velX * Global.Delta;
            y += velY * Global.Delta;
            z += velZ * Global.Delta;
        }
    }
}