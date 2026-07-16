using TMPro;
using UnityEngine;

namespace CSSC.UI.Dashboard
{
    /// <summary>
    /// 数据注入示例：演示如何在运行时向 Dashboard 文本、图表、图片占位组件中填充数据。
    /// 实际项目中可替换为网络请求、AIS 数据解析、数据库读取等真实数据源。
    /// </summary>
    public class DashboardDataInjector : MonoBehaviour
    {
        [Header("目标 Dashboard")]
        [Tooltip("为空时自动查找场景中的 DashboardUIManager")]
        public DashboardUIManager dashboard;

        [Header("示例数据")]
        public string timeFormat = "yyyy-MM-dd HH:mm:ss";

        private void Start()
        {
            if (dashboard == null)
                dashboard = FindObjectOfType<DashboardUIManager>();

            if (dashboard == null)
            {
                Debug.LogWarning("[DashboardDataInjector] 未找到 DashboardUIManager，跳过示例注入。");
                return;
            }

            InjectStaticText();
            InjectChartData();
            InjectSprites();
        }

        private void Update()
        {
            if (dashboard == null) return;
            // 实时更新时间
            dashboard.SetText("timeText", System.DateTime.Now.ToString(timeFormat));
        }

        private void InjectStaticText()
        {
            dashboard.SetText("mainTitle", "全球业务概览与运营指挥平台");
            dashboard.SetText("weatherText", "上海  26°C  晴");
            dashboard.SetText("metric_delivered_value", "416");
            dashboard.SetText("metric_regions_value", "23");
            dashboard.SetText("metric_orders_value", "42");
            dashboard.SetText("metric_building_value", "28");
            dashboard.SetText("metric_sailing_value", "12");
            dashboard.SetText("metric_offices_value", "12");
            dashboard.SetText("metric_ports_value", "67");
            dashboard.SetText("metric_partners_value", "150+");
            dashboard.SetText("metric_rank_value", "4");
            dashboard.SetText("metric_share_value", "15");
        }

        private void InjectChartData()
        {
            // 这里仅演示接口调用，实际使用 XCharts 时传入 XCharts 配置对象即可。
            dashboard.SetChartData("chart_orderRegion", new { type = "pie", legend = new[] { "亚洲", "欧洲", "美洲" }, data = new[] { 41, 37, 22 } });
            dashboard.SetChartData("chart_topShipOwners", new { type = "bar", categories = new[] { "中国", "希腊", "日本", "德国", "挪威" }, data = new[] { 18, 14, 11, 9, 7 } });
            dashboard.SetChartData("chart_seaDistribution", new { type = "bar", categories = new[] { "太平洋", "大西洋", "印度洋", "中国近海" }, data = new[] { 4, 3, 2, 3 } });
            dashboard.SetChartData("chart_shipTrackMap", new { type = "map" });
        }

        private void InjectSprites()
        {
            // 实际项目中从 Addressables、Resources 或网络加载 Sprite 后调用：
            // dashboard.SetImageSprite("img_client_MAERSK", sprite);
        }
    }
}
