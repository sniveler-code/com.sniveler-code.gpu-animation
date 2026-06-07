using System;
using System.Text;

namespace SnivelerCode.GpuAnimation.Editor.Utils
{
    public sealed class AnimatorCodeBuilder
    {
        private readonly StringBuilder _sb = new();
        private int _indentLevel;
        private const string _indentString = "    ";

        public void Line(string text = "")
        {
            if (string.IsNullOrEmpty(text))
            {
                _sb.AppendLine();
                return;
            }

            for (int i = 0; i < _indentLevel; i++) _sb.Append(_indentString);
            _sb.AppendLine(text);
        }

        public IDisposable Block(string declaration, string suffix = "\n")
        {
            Line(declaration);
            Line("{");
            _indentLevel++;
            return new Scope(this, suffix);
        }

        public override string ToString() => _sb.ToString();

        private readonly struct Scope : IDisposable
        {
            private readonly AnimatorCodeBuilder _cb;
            private readonly string _suffix;

            public Scope(AnimatorCodeBuilder cb, string suffix = "\n")
            {
                _cb = cb;
                _suffix = suffix;
            }

            public void Dispose()
            {
                _cb._indentLevel--;
                _cb.Line("}" + _suffix);
            }
        }
    }
}
