using System;
using SceneExplorer.Services;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public class ManualEntityInspector : EntityInspector
    {
        public EntityInspector Root { get; set; }

        public override void OnDestroy() {
        base.OnDestroy();
        
        Logging.Info($"Destroying manual {Id}");
        base.OnDestroy();
        OnClosedManual?.Invoke(this);
        OnClosedManual = null;
        Logging.Info($"Destroying manual {Id}");
    }

        public event Action<ManualEntityInspector> OnClosedManual;

        public override void Close() {
        Logging.Info($"Closing manual {Id}");
        base.Close();
        if (this)
        {
            Logging.Info($"Closing manual (with destroy) {Id}");
            Destroy(this.gameObject);
        }
        Logging.Info($"Closing manual completed {Id}");
    }
    }
}
