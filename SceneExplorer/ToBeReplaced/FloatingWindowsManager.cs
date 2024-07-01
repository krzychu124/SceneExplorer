using System.Collections.Generic;
using System.Linq;
using SceneExplorer.ToBeReplaced.Helpers;
using SceneExplorer.ToBeReplaced.Windows;

namespace SceneExplorer.ToBeReplaced
{
    public class FloatingWindowsManager
    {
        public int FocusedWindowId { get; private set; } = -1;
        public List<FloatingWindowBase> VisiblePopups { get; } = new List<FloatingWindowBase>();
        private Dictionary<int, List<FloatingWindowBase>> _openedWindows = new Dictionary<int, List<FloatingWindowBase>>();
        private Dictionary<int, FloatingWindowBase> _parentWindows = new Dictionary<int, FloatingWindowBase>();

        public bool CursorOverUI
        {
            get { return _parentWindows.Any(p => p.Value.IsOpen && p.Value.CursorOverUI || _openedWindows.TryGetValue(p.Key, out List<FloatingWindowBase> children) && children.Any(c => c.IsOpen && c.CursorOverUI)); }
        }

        public void FocusWindow(int id)
        {
            int lastWindow = FocusedWindowId;
            FocusedWindowId = id;
            
            if (_parentWindows.TryGetValue(lastWindow, out var window))
            {
                window.OnFocusLost();
            }
            
            if (_openedWindows.TryGetValue(lastWindow, out var windows))
            {
                windows.ForEach(w => w.OnFocusLost());
            }
        }

        public void Unfocus()
        {
            if (_parentWindows.TryGetValue(FocusedWindowId, out var window))
            {
                window.OnFocusLost();
            } 
            else if (_openedWindows.TryGetValue(FocusedWindowId, out var windows))
            {
                windows.ForEach(w => w.OnFocusLost());
            }
            FocusedWindowId = -1;
        }

        public void HideAll()
        {
            for (var i = 0; i < VisiblePopups.Count; i++)
            {
                VisiblePopups[i].enabled = false;
            }
        }

        public void ShowAll()
        {
            for (var i = 0; i < VisiblePopups.Count; i++)
            {
                VisiblePopups[i].enabled = true;
            }
        }

        public void CloseAll(bool force = true)
        {
            List<int> removed = new List<int>(_openedWindows.Count);
            foreach (KeyValuePair<int, FloatingWindowBase> floatingWindowBase in _parentWindows)
            {
                if (force || floatingWindowBase.Value.CanRemove)
                {
                    UnityEngine.Object.Destroy(floatingWindowBase.Value.gameObject);
                    removed.Add(floatingWindowBase.Key);
                }
            }
            foreach (int id in removed)
            {
                _parentWindows.Remove(id);
            }
        }

        public void RegisterWindow(FloatingWindowBase window, int parentId = -1)
        {
            Logging.Debug($"Registering {window.Id} with parent: {parentId}");
            if (parentId > -1)
            {
                if (_openedWindows.TryGetValue(parentId, out List<FloatingWindowBase> windows))
                {
                    windows.Add(window);
                    _openedWindows.Add(window.Id, new List<FloatingWindowBase>());
                }
                else
                {
                    Logging.Debug($"Parent id ({parentId}) not found while registering window: {window.Id}");
                }
            }
            else
            {
                if (!_parentWindows.ContainsKey(window.Id))
                {
                    _parentWindows.Add(window.Id, window);
                    _openedWindows.Add(window.Id, new List<FloatingWindowBase>());
                }
                else
                {
                    Logging.Debug($"Window ({window.Id}) already registered!");
                }
            }
        }

        public void DisposeOpenedWindows(int parentWindowId)
        {
            Logging.Debug($"Disposing {parentWindowId}");
            DropdownControl.Instance.Close(parentWindowId);
            if (_openedWindows.TryGetValue(parentWindowId, out List<FloatingWindowBase> children))
            {
                foreach (FloatingWindowBase floatingWindowBase in children)
                {
                    UnityEngine.Object.Destroy(floatingWindowBase);
                }

                _openedWindows.Remove(parentWindowId);
            }
            _parentWindows.Remove(parentWindowId);
            Logging.Debug($"Disposed {parentWindowId}!");
        }

        public void ClosingChild(int parentWindowId, int id)
        {
            Logging.Debug($"Closing child ({id}) of {parentWindowId}!");
            DropdownControl.Instance.Close(parentWindowId);
            if (_openedWindows.TryGetValue(parentWindowId, out List<FloatingWindowBase> children))
            {
                int index = children.FindIndex(c => c.Id == id);
                if (index > -1)
                {
                    children.RemoveAt(index);
                    Logging.Debug($"Detached opened window child {id} from {parentWindowId}!");
                }
            }
        }
    }
}
