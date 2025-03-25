using Game.Areas;
using Game.Net;
using Game.Objects;
using Unity.Entities;
using Node = Game.Net.Node;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public static class InspectObjectUtils
    {
        public static string GetModeName(int mode) {
            switch (mode)
            {
                case 0:
                    return "Any object";
                case 1:
                    return "Networks only";
                case 2:
                    return "Props and other objects";
                case 3:
                    return "Areas";
                default:
                    return "Other";
            }
        }

        public static bool EvaluateCanJumpTo(EntityManager entityManager, Entity entity)
        {
            return entity.ExistsIn(entityManager) && (entityManager.HasComponent<Transform>(entity) || entityManager.HasComponent<Node>(entity) || entityManager.HasComponent<Curve>(entity) || entityManager.HasComponent<Geometry>(entity));
        }
    }
}
