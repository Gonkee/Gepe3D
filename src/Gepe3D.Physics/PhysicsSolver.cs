
namespace Gepe3D.Physics
{
    public class PhysicsSolver
    {

        public static void IntegrateExplicitEuler(PhysicsBody body, float delta)
        {
            float[] derivative = body.GetDerivative( body.GetState() );
            float[] change = new float[derivative.Length];
            for (int i = 0; i < change.Length; i++) change[i] = derivative[i] * delta;
            body.UpdateState(change);
        }
        
        public static void IntegrateRungeKutta4(PhysicsBody body, float delta)
        {
            float[] initialState, state1, state2, state3,
                dA, dB, dC, dD, change;

            initialState = body.GetState();

            dA = body.GetDerivative(initialState);
            state1 = SimpleUpdateState(initialState, dA, delta / 2);
            dB = body.GetDerivative(state1);
            state2 = SimpleUpdateState(initialState, dB, delta / 2);
            dC = body.GetDerivative(state2);
            state3 = SimpleUpdateState(initialState, dC, delta);
            dD = body.GetDerivative(state3);

            change = new float[initialState.Length];
            for (int i = 0; i < change.Length; i++)
            {
                change[i] = ( dA[i] + 2 * dB[i] + 2 * dC[i] + dD[i] ) / 6f * delta;
            }


            body.UpdateState(change);
            
        }

        private static float[] SimpleUpdateState (float[] state, float[] derivative, float delta)
        {
            float[] stateNew = new float[state.Length];
            for (int i = 0; i < state.Length; i++)
            {
                stateNew[i] = state[i] + derivative[i] * delta;
            }
            return stateNew;
        }

    }
}