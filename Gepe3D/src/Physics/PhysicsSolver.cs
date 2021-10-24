
using System.Collections.Generic;

namespace Gepe3D
{
    public class PhysicsSolver
    {

        public static void IntegrateExplicitEuler(PhysicsBody body, float delta, List<PhysicsBody> bodies)
        {
            PhysicsData derivative = body.GetDerivative( body.GetState() );
            PhysicsData change = new PhysicsData(derivative.DataLength);
            for (int i = 0; i < change.DataLength; i++) change.Set(i, derivative.Get(i) * delta);
            body.UpdateState(change, bodies);
        }
        
        public static void IntegrateRungeKutta4(PhysicsBody body, float delta, List<PhysicsBody> bodies)
        {
            PhysicsData initialState, state1, state2, state3,
                dA, dB, dC, dD, change;

            initialState = body.GetState();

            dA = body.GetDerivative(initialState);
            state1 = SimpleUpdateState(initialState, dA, delta / 2);
            dB = body.GetDerivative(state1);
            state2 = SimpleUpdateState(initialState, dB, delta / 2);
            dC = body.GetDerivative(state2);
            state3 = SimpleUpdateState(initialState, dC, delta);
            dD = body.GetDerivative(state3);

            change = new PhysicsData(initialState.DataLength);
            for (int i = 0; i < change.DataLength; i++)
            {
                change.Set( i, ( dA.Get(i) + 2 * dB.Get(i) + 2 * dC.Get(i) + dD.Get(i) ) / 6f * delta );
            }

            body.UpdateState(change, bodies);
            
        }

        private static PhysicsData SimpleUpdateState (PhysicsData state, PhysicsData derivative, float delta)
        {
            PhysicsData stateNew = new PhysicsData(state.DataLength);
            for (int i = 0; i < state.DataLength; i++)
            {
                stateNew.Set( i, state.Get(i) + derivative.Get(i) * delta );
            }
            return stateNew;
        }

    }
}