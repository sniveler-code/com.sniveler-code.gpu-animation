#if UNITY_EDITOR

using UnityEditor;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    [InitializeOnLoad]
    public static class EditorProcessor
    {
        static EditorProcessor()
        {
            EditorApplication.playModeStateChanged += OnStateChanged;
        }

        private static void OnStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Selection.activeGameObject = null;
            }
        }
    }
}

#endif
