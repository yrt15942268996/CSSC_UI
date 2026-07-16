using System;
using UnityEngine;
using UnityEngine.UI;

namespace CSSC.UI.Dashboard
{
    /// <summary>
    /// 数据可视化大屏中各类图表的占位组件。
    /// 不直接依赖 XCharts，运行时可在该 GameObject 上挂载具体 XCharts 图表组件，
    /// 并调用 <see cref="InitChart"/> 传入配置/数据完成实际图表初始化。
    /// </summary>
    public class DashboardChartPlaceholder : MonoBehaviour
    {
        [Header("占位配置")]
        [Tooltip("该图表的唯一标识，供数据注入时匹配")]
        public string chartId = "";

        [Tooltip("图表类型：Pie/Bar/Line/Radar/Gauge/Map 等")]
        public ChartType chartType = ChartType.Other;

        [Tooltip("背景占位色，便于在 Scene 视图中区分图表区域")]
        public Color placeholderColor = new Color(0.1f, 0.15f, 0.25f, 0.35f);

        private Image _bg;

        public Image Background => _bg;

        public enum ChartType
        {
            Other,
            Pie,
            Bar,
            Line,
            Radar,
            Gauge,
            Map
        }

        /// <summary>
        /// 数据注入接口：实际图表组件挂载后，通过该方法传入配置与数据。
        /// </summary>
        /// <param name="config">序列化配置对象</param>
        public void InitChart(object config)
        {
            // 预留给 XCharts 数据注入，当前版本为空实现。
            Debug.Log($"[DashboardChartPlaceholder] {chartId} 准备接收配置：{config?.GetType().Name}");
        }

        private void Awake()
        {
            EnsureBackground();
        }

        private void EnsureBackground()
        {
            _bg = GetComponent<Image>();
            if (_bg == null)
            {
                _bg = gameObject.AddComponent<Image>();
            }
            _bg.color = placeholderColor;
            _bg.raycastTarget = false;
        }

        public void SetColor(Color color)
        {
            placeholderColor = color;
            if (_bg != null)
                _bg.color = color;
        }
    }
}
