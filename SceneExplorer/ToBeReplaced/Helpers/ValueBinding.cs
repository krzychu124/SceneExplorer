using System;
using Colossal.UI.Binding;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public class ValueBinding<T>
    {
        private readonly Func<T> _read;
        private T _value;

        public T Value => _value;
    
        public ValueBinding(Func<T> readValue) {
        _read = readValue;
    }

        public void Update() {
        _value = _read();
    }
    }
}
