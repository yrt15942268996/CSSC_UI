using UnityEngine;

namespace CSSC_UI
{
    /// <summary>
    /// 世界地图编辑器辅助脚本（可选，仅用于快速测试WorldMapUI）
    /// 
    /// 【注意】此脚本仅通过WorldMapUI的公共API与之交互，
    /// 不直接访问任何内部字段，确保模块间松耦合。
    /// 测试完成后可以从GameObject上移除，不影响WorldMapUI正常工作。
    /// </summary>
    [RequireComponent(typeof(WorldMapUI))]
    public class WorldMapUITester : MonoBehaviour
    {
        private WorldMapUI _worldMapUI;

        [Header("测试用JSON数据")]
        [TextArea(3, 10)]
        public string testJsonData = @"{
    ""countries"": [
        ""China"",
        ""United States"",
        ""Japan"",
        ""Germany"",
        ""United Kingdom"",
        ""France"",
        ""Brazil"",
        ""India"",
        ""Russia"",
        ""Australia""
    ]
}";

        [Header("测试设置")]
        public Color testMapColor = new Color(0.2f, 0.3f, 0.5f, 1f);
        public Color testMarkerColor = Color.red;

        private void Start()
        {
            _worldMapUI = GetComponent<WorldMapUI>();

            if (_worldMapUI != null)
            {
                _worldMapUI.SetMapColor(testMapColor);
                _worldMapUI.SetMarkerColor(testMarkerColor);
                _worldMapUI.LoadCountryDataFromJson(testJsonData);
            }
        }

        /// <summary>
        /// 在编辑器中重新加载测试数据
        /// </summary>
        [ContextMenu("Reload Test Data")]
        public void ReloadTestData()
        {
            if (_worldMapUI == null)
                _worldMapUI = GetComponent<WorldMapUI>();

            if (_worldMapUI != null)
            {
                _worldMapUI.SetMapColor(testMapColor);
                _worldMapUI.SetMarkerColor(testMarkerColor);
                _worldMapUI.LoadCountryDataFromJson(testJsonData);
            }
        }
    }
}
