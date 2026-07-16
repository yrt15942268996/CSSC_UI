using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CSSC.UI.Dashboard
{
    /// <summary>
    /// 底部状态栏：展示全局关键指标与系统状态。
    /// </summary>
    public class DashboardStatusBar : MonoBehaviour
    {
        [Header("状态条目")]
        [Tooltip("状态栏根 RectTransform")]
        public RectTransform barRoot;

        [Tooltip("状态条目预制模板（为空时自动生成）")]
        public GameObject statusItemPrefab;

        [Tooltip("正常状态颜色")]
        public Color normalColor = new Color(0.2f, 0.8f, 0.4f, 1f);

        private readonly string[] _defaultKeys = {
            "global_total", "sailing", "building", "orders", "ports", "partners"
        };

        private readonly string[] _defaultIcons = {
            "icon_ship", "icon_sailing", "icon_building", "icon_order", "icon_port", "icon_partner"
        };

        private readonly string[] _defaultLabels = {
            "全球船舶总数", "在航船舶", "在建船舶", "手持订单", "服务港口", "合作伙伴"
        };

        private readonly string[] _defaultValues = {
            "416 艘", "12 艘", "28 艘", "42 艘", "67 个", "150+ 家"
        };

        private void Start()
        {
            if (barRoot == null)
                barRoot = GetComponent<RectTransform>();
            BuildStatusBar();
        }

        private void BuildStatusBar()
        {
            if (barRoot == null) return;

            int count = _defaultKeys.Length;
            float width = barRoot.rect.width / count;
            float height = barRoot.rect.height;

            for (int i = 0; i < count; i++)
            {
                var item = CreateStatusItem(_defaultKeys[i], _defaultIcons[i], _defaultLabels[i], _defaultValues[i]);
                var rt = item.GetComponent<RectTransform>();
                rt.SetParent(barRoot, false);
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(i * width, 0);
                rt.sizeDelta = new Vector2(width, 0);
            }

            // 系统状态
            var systemItem = CreateStatusItem("system_status", "icon_status", "系统状态", "正常运行");
            var sysRt = systemItem.GetComponent<RectTransform>();
            sysRt.SetParent(barRoot, false);
            sysRt.anchorMin = new Vector2(1, 0);
            sysRt.anchorMax = new Vector2(1, 1);
            sysRt.pivot = new Vector2(1, 0.5f);
            sysRt.anchoredPosition = new Vector2(-20, 0);
            sysRt.sizeDelta = new Vector2(180, 0);
            var statusDot = systemItem.GetComponent<Image>();
            if (statusDot != null) statusDot.color = normalColor;
        }

        private GameObject CreateStatusItem(string key, string iconId, string label, string value)
        {
            GameObject go;
            if (statusItemPrefab != null)
            {
                go = Instantiate(statusItemPrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                go = new GameObject($"StatusItem_{key}", typeof(RectTransform), typeof(Image));
                var bg = go.GetComponent<Image>();
                bg.color = new Color(0, 0, 0, 0);
                bg.raycastTarget = false;
            }
            go.name = $"StatusItem_{key}";

            var rt = go.GetComponent<RectTransform>();

            // 图标占位
            var icon = DashboardUIHelper.CreateImagePlaceholder(rt, $"img_{iconId}", new Vector2(10, 0), new Vector2(36, 36), new Color(0.08f, 0.12f, 0.18f, 0.45f));
            icon.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
            icon.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
            icon.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(15, 0);

            // 标签
            var labelText = DashboardUIHelper.CreateText(rt, "Label", label, DashboardUIHelper.TextStyle.Status,
                new Vector2(60, 8), new Vector2(120, 25));
            labelText.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
            labelText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
            labelText.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            labelText.GetComponent<RectTransform>().anchoredPosition = new Vector2(60, 8);

            // 数值
            var valueText = DashboardUIHelper.CreateText(rt, "Value", value, DashboardUIHelper.TextStyle.NumberUnit,
                new Vector2(60, -18), new Vector2(120, 30));
            valueText.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
            valueText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
            valueText.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            valueText.GetComponent<RectTransform>().anchoredPosition = new Vector2(60, -18);

            return go;
        }
    }
}
