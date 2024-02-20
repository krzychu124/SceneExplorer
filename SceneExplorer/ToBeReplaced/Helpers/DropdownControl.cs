using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public class DropdownControl : MonoBehaviour
    {
        private static GUIContent _temp = new GUIContent("");
        private static CommonUI.LocalState _tempState = new CommonUI.LocalState();
        private static DropdownControl _instance;

        internal static DropdownControl Instance
        {
            get
        {
            if (!_instance)
            {
                _instance = new GameObject("SceneExplorer Popup").AddComponent<DropdownControl>();
            }
            return _instance;
        }
        }

        public bool IsOpen { get; private set; }

        private int _id;
        private int _ownerId;
        private Rect _position = new Rect(0, 0, 200, 250);
        private Func<int, CommonUI.LocalState, bool> _callback;
        private CommonUI.LocalState _value = null;
        private Vector2 _scrollPos;
        private float _lastCheck;

        private void Awake() {
        _id = GUIUtility.GetControlID(FocusType.Passive);
        enabled = false;
    }

        private void OnGUI() {
        if (_callback != null)
        {
            GUISkin temp = GUI.skin;
            GUI.skin = UIStyle.Instance.Skin;
            _position = GUILayout.Window(_id, _position, RenderWindow, "", options: null);
            GUI.skin = temp;
        }
    }

        private void RenderWindow(int id) {
        GUI.BringWindowToFront(id);

        var c = _callback;
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, options: null);
        bool close = _callback(id, _value);
        GUILayout.EndScrollView();

        if (close && c == _callback)
        {
            enabled = false;
        }
        else
        {
            DrawBorder();
        }
            
        if (c == _callback && Time.time - _lastCheck > 1f && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            Vector2 v = Mouse.current.position.value;
            if (!_position.Contains(new Vector2(v.x, Screen.height - v.y)))
            {
                enabled = false;
            }
            _lastCheck = Time.time;
        }
    }
        
        private void DrawBorder() {
        Texture2D texture = UIStyle.Instance.CommonHoverTexture;
        float width = 1f;
        GUI.DrawTexture(new Rect(0.0f, 0.0f, width, _position.height), texture);
        GUI.DrawTexture(new Rect(_position.width - width, 0.0f, width, _position.height), texture);
        GUI.DrawTexture(new Rect(0.0f, _position.height - width, _position.width, width), texture);
    }

        protected void OnEnable() {
        IsOpen = true;
    }

        protected void OnDisable() {
        IsOpen = false;
    }

        private void Open() {
        _scrollPos = Vector2.zero;
        enabled = true;
        _lastCheck = Time.time;
    }

        public static void DrawButton(string text, GUIStyle style, CommonUI.LocalState state, Func<int, CommonUI.LocalState, bool> callback, Rect position, int ownerWindowId, Action<string> onOpened = null) {
        GUIContent content = Temp(text);
        Rect rect = GUILayoutUtility.GetRect(content, style);
        if (GUI.Button(rect, text, style))
        {
            Instance.Open();
            Instance._ownerId = ownerWindowId;
            Instance._callback = callback;
            Instance._value = state;
            Instance._position = new Rect(position.x+4f, position.y + rect.y + 24f, Mathf.Max(180, position.width -8f), Instance._position.height);
            onOpened?.Invoke(text);
        }
    }

        public static void DrawTextField(CommonUI.LocalState text, Func<int, CommonUI.LocalState, bool> callback, Action<string> onOpened, Action<string> onChanged, Rect position) {
        int lastFocused = GUIUtility.keyboardControl;
        GUIContent content = Temp(text.value);
        Rect rect = GUILayoutUtility.GetRect(content, GUI.skin.textField);
        int active = GUIUtility.GetControlID(FocusType.Keyboard) + 1;
        string prev = text.value;
        text.value = GUI.TextField(rect, text.value);
        int focused = GUIUtility.keyboardControl;
        if (lastFocused != focused && focused == active)
        {
            Instance.Open();
            Instance._callback = callback;
            Instance._value = text;
            Instance._position = new Rect(position.x+4f, position.y + rect.y + 24f, Mathf.Max(180, position.width-8f), Instance._position.height);
            onOpened(text.value);
        }
    }

        private static GUIContent Temp(string t) {
        _temp.text = t;
        _temp.tooltip = string.Empty;
        return _temp;
    }

        private static CommonUI.LocalState TempState(string value) {
        _tempState.value = value;
        return _tempState;
    }

        public void Close(int windowId) {
        if (IsOpen && windowId == _ownerId)
        {
            enabled = false;
        }
    }
    }
}
