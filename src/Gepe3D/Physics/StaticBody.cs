

using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class StaticBody : PhysicsBody
    {

        private float _maxX, _minX, _maxY, _minY, _maxZ, _minZ;

        private readonly Mesh mesh;

        public StaticBody(Geometry geometry, Material material)
        {
            _maxX = float.MinValue;
            _minX = float.MaxValue;
            _maxY = float.MinValue;
            _minY = float.MaxValue;
            _maxZ = float.MinValue;
            _minZ = float.MaxValue;
            
            mesh = new Mesh(geometry, material);

            foreach (Vector3 v in geometry.Vertices)
            {
                _maxX = Math.Max( _maxX, v.X );
                _minX = Math.Min( _minX, v.X );
                _maxY = Math.Max( _maxY, v.Y );
                _minY = Math.Min( _minY, v.Y );
                _maxZ = Math.Max( _maxZ, v.Z );
                _minZ = Math.Min( _minZ, v.Z );
            }
        }

        public override PhysicsData GetState()
        {
            return new PhysicsData(0);
        }

        public override PhysicsData GetDerivative(PhysicsData state)
        {
            return new PhysicsData(0);
        }

        public override void UpdateState(PhysicsData change, List<PhysicsBody> bodies)
        {
        }

        
        public override float MaxX() { return _maxX; }
        public override float MinX() { return _minX; }
        public override float MaxY() { return _maxY; }
        public override float MinY() { return _minY; }
        public override float MaxZ() { return _maxZ; }
        public override float MinZ() { return _minZ; }

        public override Mesh GetMesh()
        {
            return mesh;
        }

        public override void Draw()
        {
            mesh.Draw();
        }
    }
}