namespace SceneExplorer.Services
{
    public class WatcherService
    {


        public interface IWatchable
        {
            string Preview();
            bool IsSnapshot { get; }
            void Inspect();
        }
    }
}
