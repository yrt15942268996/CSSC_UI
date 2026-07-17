using UnityEngine;

namespace CSSC_UI
{
    /// <summary>
    /// LineChartUI 测试脚本，用于在编辑器中快速预览
    /// 挂载到同一GameObject上即可
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LineChartUITester : MonoBehaviour
    {
        [Header("测试数据（按 T 键应用）")]
        [SerializeField] private Vector2[] testPositions = new Vector2[11]
        {
            new Vector2(5f, 10f),
            new Vector2(24f, 50f),
            new Vector2(43f, 30f),
            new Vector2(62f, 60f),
            new Vector2(81f, 15f),
            new Vector2(100f, 45f),
            new Vector2(119f, 35f),
            new Vector2(138f, 55f),
            new Vector2(157f, 20f),
            new Vector2(176f, 40f),
            new Vector2(190f, 50f),
        };

        private LineChartUI _lineChart;

        private void Awake()
        {
            _lineChart = GetComponent<LineChartUI>();
        }

        private void Update()
        {
            if (_lineChart == null) return;

            // 按 R 键重新绘制
            if (Input.GetKeyDown(KeyCode.R))
            {
                _lineChart.Redraw();
                Debug.Log("[LineChartUITester] 重新绘制");
            }

            // 按 T 键应用测试数据
            if (Input.GetKeyDown(KeyCode.T))
            {
                _lineChart.SetPointPositions(testPositions);
                Debug.Log("[LineChartUITester] 已应用测试坐标");
            }
        }
    }
}
