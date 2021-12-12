

namespace Gepe3D
{
    public class ContactConstraint : PBD_data.Constraint
    {
        
        private int id1, id2;
        
        public ContactConstraint(int id1, int id2)
        {
            this.id1 = id1;
            this.id2 = id2;
        }
        
        public override void Project()
        {
            throw new System.NotImplementedException();
        }
        
        public override float Evaluate()
        {
            return 0;
        }
    }
}