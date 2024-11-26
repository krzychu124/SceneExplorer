using System;
using System.Collections.Generic;
using SceneExplorer.ToBeReplaced.Helpers;
using SceneExplorer.ToBeReplaced.Helpers.ContentSections;
using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Windows
{
    public class ObjectInfo : FloatingWindowBase
    {
        private List<ISection> _expandableComponents = new List<ISection>();

        private object _lock = new object();

        private string _prefabName;
        private Vector2 _scrollPos = Vector2.zero;

        public ObjectInfo() {
        _minSize = new Vector2(200, 300);
        ForceSize(420, 460);
    }

        protected override string Title => "Object Info";
        public DataBindings Bindings { get; set; }

        protected override void RenderWindowContent() {
        GUILayout.Label("Object Information", options: null);

        if (!BindingsReady())
        {
            GUILayout.Label("Data bindings not ready!");
            return;
        }

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, options: null);
        for (var i = 0; i < _expandableComponents.Count; i++)
        {
            _expandableComponents[i].Render();
        }
        GUILayout.EndScrollView();
    }

        private bool BindingsReady() {
        return Bindings != null;
    }

        public void UpdateContent(List<ISection> sections, string prefabName) {
        lock (_lock)
        {
            _expandableComponents = sections;
            _prefabName = prefabName;
        }
    }

        public class DataBindings
        {
            public ValueBinding<string> Text;


            public void UpdateBindings() {
            Text.Update();
        }
        }

        public class BindingsBuilder
        {
            private Func<string> _text;

            public static BindingsBuilder Create() {
            return new BindingsBuilder();
        }

            public BindingsBuilder SetTextGetter(Func<string> getter) {
            _text = getter;
            return this;
        }

            public DataBindings Build() {
            DataBindings b = new DataBindings
            {
                Text = new ValueBinding<string>(_text ?? (() => "TEXT BINDING NOT SET"))
            };
            return b;
        }
        }

        public class UIBuilder
        {
            private List<ISection> _sections = new List<ISection>();

            public void Reset() {
            _sections.Clear();
        }

            public UIBuilder AddSection(string title, ValueBinding<string> valueBinding) {
            _sections.Add(new ExpandableSection(title, valueBinding));
            return this;
        }

            public List<ISection> Build() {
            foreach (ISection section in _sections)
            {
                section.UpdateBindings();
            }
            return _sections;
        }

            public UIBuilder AddTagSection(string typeName) {
            _sections.Add(new TagSection(typeName));
            return this;
        }

            public UIBuilder AddBufferSection(string typeName, string content) {
            // _sections.Add(new BufferSection(typeName, content));
            return this;
        }
        }
    }
}
