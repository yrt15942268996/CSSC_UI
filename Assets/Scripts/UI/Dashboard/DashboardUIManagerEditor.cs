using UnityEngine;

namespace CSSC.UI.Dashboard
{
    /// <summary>
    /// 编辑器扩展：在 Inspector 上提供一键生成大屏 UI 的按钮。
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(DashboardUIManager))]
    public class DashboardUIManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            if (GUILayout.Button("Build Dashboard", GUILayout.Height(32)))
            {
                var mgr = (DashboardUIManager)target;
                mgr.Build();
                UnityEditor.EditorUtility.SetDirty(mgr.gameObject);
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Clear Children", GUILayout.Height(32)))
            {
                var mgr = (DashboardUIManager)target;
                var children = mgr.transform.childCount;
                for (int i = children - 1; i >= 0; i--)
                {
                    DestroyImmediate(mgr.transform.GetChild(i).gameObject);
                }
                UnityEditor.EditorUtility.SetDirty(mgr.gameObject);
            }
        }
    }
#endif
}
