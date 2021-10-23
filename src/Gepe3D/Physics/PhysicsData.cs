


namespace Gepe3D
{
    public class PhysicsData
    {
        private readonly float[] _data;
        public readonly int DataLength;

        public PhysicsData(int dataLength)
        {
            this._data = new float[dataLength];
            DataLength = dataLength;
        }
        
        public PhysicsData(float[] data)
        {
            this._data = data;
            DataLength = data.Length;
        }

        public float Get(int id)
        {
            return _data[id];
        }

        public void Set(int id, float value)
        {
            _data[id] = value;
        }

        public float[] GetData()
        {
            return _data;
        }

    }
}