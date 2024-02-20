using System.Collections.Generic;
using Game.Objects;
using Game.Vehicles;

namespace SceneExplorer.ToBeReplaced.Helpers.ComponentComparer
{
    public static class VehicleComparers
    {
    
        internal class TrainBogieFrameEqualityComparer : IEqualityComparer<TrainBogieFrame>
        {
            public bool Equals(TrainBogieFrame x, TrainBogieFrame y) {
            return x.m_FrontLane.Equals(y.m_FrontLane) && x.m_RearLane.Equals(y.m_RearLane);
        }

            public int GetHashCode(TrainBogieFrame obj) {
            unchecked
            {
                return (obj.m_FrontLane.GetHashCode() * 397) ^ obj.m_RearLane.GetHashCode();
            }
        }
        }
    
    
        internal class TransformFrameEqualityComparer : IEqualityComparer<TransformFrame>
        {
            public bool Equals(TransformFrame x, TransformFrame y) {
            return x.m_Position.Equals(y.m_Position) && x.m_Velocity.Equals(y.m_Velocity) && x.m_Rotation.Equals(y.m_Rotation) && x.m_Flags == y.m_Flags && x.m_StateTimer == y.m_StateTimer && x.m_State == y.m_State && x.m_Activity == y.m_Activity;
        }

            public int GetHashCode(TransformFrame obj) {
            unchecked
            {
                int hashCode = obj.m_Position.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.m_Velocity.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.m_Rotation.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)obj.m_Flags;
                hashCode = (hashCode * 397) ^ obj.m_StateTimer.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)obj.m_State;
                hashCode = (hashCode * 397) ^ obj.m_Activity.GetHashCode();
                return hashCode;
            }
        }
        }

        public static IEqualityComparer<TransformFrame> TransformFrameComparer { get; } = new TransformFrameEqualityComparer();
    }
}
