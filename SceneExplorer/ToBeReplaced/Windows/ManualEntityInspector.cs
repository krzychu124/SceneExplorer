using System;
using SceneExplorer.Services;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public class ManualEntityInspector : EntityInspector
    {
        public EntityInspector Root { get; set; }

        public override void OnDestroy() {
        base.OnDestroy();
        
        Logging.Debug($"Destroying manual {Id}");
        base.OnDestroy();
        OnClosedManual?.Invoke(this);
        OnClosedManual = null;
        Logging.Debug($"Destroying manual {Id}");
    }

        public event Action<ManualEntityInspector> OnClosedManual;

        public override void Close() {
        Logging.Debug($"Closing manual {Id}");
        base.Close();
        if (this)
        {
            Logging.Debug($"Closing manual (with destroy) {Id}");
            Destroy(this.gameObject);
        }
        Logging.Debug($"Closing manual completed {Id}");
    }
    }
}
