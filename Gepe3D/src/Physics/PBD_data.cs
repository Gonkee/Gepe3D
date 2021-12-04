

using OpenTK.Mathematics;

namespace Gepe3D
{
    public class PBD_data
    {
        
        private readonly float[] _positions;
        private readonly float[] _velocities;
        private readonly float[] _inverseMass;
        private int[] _phases;
        
        public readonly int ParticleCount;

        public PBD_data(int particleCount)
        {
            this.ParticleCount = particleCount;
            _positions = new float[particleCount * 3];
            _velocities = new float[particleCount * 3];
            _inverseMass = new float[particleCount];
            _phases = new int[particleCount];
        }
        
        public float[] GetPositionBuffer()
        {
            return _positions;
        }

        public Vector3 GetPos(int id)
        {
            return new Vector3(
                _positions[id * 3 + 0],
                _positions[id * 3 + 1],
                _positions[id * 3 + 2]
            );
        }

        public void SetPos(int id, float x, float y, float z)
        {
            _positions[id * 3 + 0] = x;
            _positions[id * 3 + 1] = y;
            _positions[id * 3 + 2] = z;
        }
        
        public void SetPos(int id, Vector3 pos)
        {
            _positions[id * 3 + 0] = pos.X;
            _positions[id * 3 + 1] = pos.Y;
            _positions[id * 3 + 2] = pos.Z;
        }

        public void AddPos(int id, float x, float y, float z)
        {
            _positions[id * 3 + 0] += x;
            _positions[id * 3 + 1] += y;
            _positions[id * 3 + 2] += z;
        }
        
        public void AddPos(int id, Vector3 pos)
        {
            _positions[id * 3 + 0] += pos.X;
            _positions[id * 3 + 1] += pos.Y;
            _positions[id * 3 + 2] += pos.Z;
        }
        
        public Vector3 GetVel(int id)
        {
            return new Vector3(
                _velocities[id * 3 + 0],
                _velocities[id * 3 + 1],
                _velocities[id * 3 + 2]
            );
        }

        public void SetVel(int id, float x, float y, float z)
        {
            _velocities[id * 3 + 0] = x;
            _velocities[id * 3 + 1] = y;
            _velocities[id * 3 + 2] = z;
        }
        
        public void SetVel(int id, Vector3 vel)
        {
            _velocities[id * 3 + 0] = vel.X;
            _velocities[id * 3 + 1] = vel.Y;
            _velocities[id * 3 + 2] = vel.Z;
        }

        public void AddVel(int id, float x, float y, float z)
        {
            _velocities[id * 3 + 0] += x;
            _velocities[id * 3 + 1] += y;
            _velocities[id * 3 + 2] += z;
        }
        
        public void AddVel(int id, Vector3 vel)
        {
            _velocities[id * 3 + 0] += vel.X;
            _velocities[id * 3 + 1] += vel.Y;
            _velocities[id * 3 + 2] += vel.Z;
        }
    }
}