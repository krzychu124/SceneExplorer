using System;
using SceneExplorer.Services;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public class ManualEntityInspector : EntityInspector
    {
        public EntityInspector Root { get; set; }

        public override void OnDestroy() {
        base.OnDestroy();
        
        Logging.DebugUI($"Destroying manual {Id}");
        base.OnDestroy();
        OnClosedManual?.Invoke(this);
        OnClosedManual = null;
        Logging.DebugUI($"Destroying manual {Id}");
    }

        public event Action<ManualEntityInspector> OnClosedManual;

        public override void Close() {
        Logging.DebugUI($"Closing manual {Id}");
        base.Close();
        if (this)
        {
            Logging.DebugUI($"Closing manual (with destroy) {Id}");
            Destroy(this.gameObject);
        }
        Logging.DebugUI($"Closing manual completed {Id}");
    }
    }
}
