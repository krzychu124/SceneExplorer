using Game.UI.Editor;
using SceneExplorer.System;
using Unity.Entities;

namespace SceneExplorer.Tools
{
    public class InspectObjectTool : EditorTool
    {
        public const string ToolID = "Inspect Object";

        public InspectObjectTool(World world) : base(world)
        {
            base.id = ToolID;
            base.icon = "Media/Editor/DefaultObject.svg";
            base.panel = world.GetOrCreateSystemManaged<InspectorToolPanelSystem>();
            base.tool = world.GetOrCreateSystemManaged<InspectObjectToolSystem>();
        }
    }
}
