using UnityEngine;

namespace CSSC.UI.Dashboard
{
    /// <summary>
    /// 启动器：自动挂载 Dashboard 所需核心组件，便于在任意场景一键运行。
    /// </summary>
    public class DashboardBootstrap : MonoBehaviour
    {
        [Tooltip("是否自动创建 Dashboard 系统")]
        public bool autoCreate = true;

        private void Awake()
        {
            if (!autoCreate) return;

            var manager = FindObjectOfType<DashboardUIManager>();
            if (manager == null)
            {
                var go = new GameObject("DashboardRoot");
                manager = go.AddComponent<DashboardUIManager>();
                go.AddComponent<DashboardDataInjector>();

                // 状态栏挂在独立对象上，便于布局
                var statusGo = new GameObject("DashboardStatusBar");
                statusGo.transform.SetParent(go.transform, false);
                var statusBar = statusGo.AddComponent<DashboardStatusBar>();
                statusBar.barRoot = null; // 在 Start 中自动查找
            }
        }
    }
}
