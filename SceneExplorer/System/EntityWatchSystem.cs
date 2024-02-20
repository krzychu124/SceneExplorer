using System.Collections.Generic;
using Game;
using Unity.Entities;

namespace SceneExplorer.System
{
    public partial class EntityWatchSystem : GameSystemBase
    {
        private HashSet<Entity> _watchingEntities;

        protected override void OnCreate() {
            base.OnCreate();
            _watchingEntities = new HashSet<Entity>();
        }

        protected override void OnUpdate() {
        
        }
        
        
    }
}
