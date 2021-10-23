

using OpenTK.Mathematics;

namespace Gepe3D
{
    public class ParticleData : PhysicsData
    {

        public readonly int ParticleCount;

        private int  x (int id) { return id * 6 + 0; }
        private int  y (int id) { return id * 6 + 1; }
        private int  z (int id) { return id * 6 + 2; }
        private int vx (int id) { return id * 6 + 3; }
        private int vy (int id) { return id * 6 + 4; }
        private int vz (int id) { return id * 6 + 5; }

        public ParticleData(int particleCount) : base(particleCount * 6)
        {
            this.ParticleCount = particleCount;
        }

        public ParticleData(PhysicsData data) : base( data.GetData() )
        {
            this.ParticleCount = data.DataLength / 6;
        }

        public Vector3 GetPos(int v_id)
        {
            return new Vector3( Get(v_id * 6 + 0), Get(v_id * 6 + 1), Get(v_id * 6 + 2) );
        }

        public void SetPos(int v_id, float x, float y, float z)
        {
            Set(v_id * 6 + 0, x);
            Set(v_id * 6 + 1, y);
            Set(v_id * 6 + 2, z);
        }
        
        public void SetPos(int v_id, Vector3 pos)
        {
            Set(v_id * 6 + 0, pos.X);
            Set(v_id * 6 + 1, pos.Y);
            Set(v_id * 6 + 2, pos.Z);
        }

        public void AddPos(int v_id, float x, float y, float z)
        {
            Set( v_id * 6 + 0, Get(v_id * 6 + 0) + x );
            Set( v_id * 6 + 1, Get(v_id * 6 + 1) + y );
            Set( v_id * 6 + 2, Get(v_id * 6 + 2) + z );
        }
        
        public void AddPos(int v_id, Vector3 pos)
        {
            Set(v_id * 6 + 0, Get(v_id * 6 + 0) + pos.X);
            Set(v_id * 6 + 1, Get(v_id * 6 + 1) + pos.Y);
            Set(v_id * 6 + 2, Get(v_id * 6 + 2) + pos.Z);
        }
        
        public Vector3 GetVel(int v_id)
        {
            return new Vector3( Get(v_id * 6 + 3), Get(v_id * 6 + 4), Get(v_id * 6 + 5) );
        }

        public void SetVel(int v_id, float x, float y, float z)
        {
            Set(v_id * 6 + 3, x);
            Set(v_id * 6 + 4, y);
            Set(v_id * 6 + 5, z);
        }
        
        public void SetVel(int v_id, Vector3 vel)
        {
            Set(v_id * 6 + 3, vel.X);
            Set(v_id * 6 + 4, vel.Y);
            Set(v_id * 6 + 5, vel.Z);
        }

        public void AddVel(int v_id, float x, float y, float z)
        {
            Set(v_id * 6 + 3, Get(v_id * 6 + 3) + x);
            Set(v_id * 6 + 4, Get(v_id * 6 + 4) + y);
            Set(v_id * 6 + 5, Get(v_id * 6 + 5) + z);
        }
        
        public void AddVel(int v_id, Vector3 vel)
        {
            Set(v_id * 6 + 3, Get(v_id * 6 + 3) + vel.X);
            Set(v_id * 6 + 4, Get(v_id * 6 + 4) + vel.Y);
            Set(v_id * 6 + 5, Get(v_id * 6 + 5) + vel.Z);
        }

    }
}