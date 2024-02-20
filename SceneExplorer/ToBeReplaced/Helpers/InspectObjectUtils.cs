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
                default:
                    return "Props and other objects";
            }
        }
    }
}
