

using System;
using System.Collections.Generic;
using Gepe3D.Core;
using OpenTK.Mathematics;

namespace Gepe3D.Physics
{
    public class StaticBody : PhysicsBody
    {

        private float _maxX, _minX, _maxY, _minY, _maxZ, _minZ;


        public StaticBody(Geometry geometry, Material material) : base(geometry, material)
        {
            _maxX = float.MinValue;
            _minX = float.MaxValue;
            _maxY = float.MinValue;
            _minY = float.MaxValue;
            _maxZ = float.MinValue;
            _minZ = float.MaxValue;

            foreach (Vector3 v in geometry.vertices)
            {
                _maxX = Math.Max( _maxX, v.X );
                _minX = Math.Min( _minX, v.X );
                _maxY = Math.Max( _maxY, v.Y );
                _minY = Math.Min( _minY, v.Y );
                _maxZ = Math.Max( _maxZ, v.Z );
                _minZ = Math.Min( _minZ, v.Z );
            }
        }

        public override float[] GetState()
        {
            return new float[0];
        }

        public override float[] GetDerivative(float[] state)
        {
            return new float[0];
        }

        public override void UpdateState(float[] change, List<PhysicsBody> bodies)
        {
        }

        
        public override float MaxX() { return _maxX; }
        public override float MinX() { return _minX; }
        public override float MaxY() { return _maxY; }
        public override float MinY() { return _minY; }
        public override float MaxZ() { return _maxZ; }
        public override float MinZ() { return _minZ; }

    }
}