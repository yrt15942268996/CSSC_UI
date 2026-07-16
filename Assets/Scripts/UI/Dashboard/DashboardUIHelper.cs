using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CSSC.UI.Dashboard
{
    /// <summary>
    /// Dashboard UI 通用构建工具：统一封装 TextMeshPro、Image、RectTransform 的创建与布局。
    /// </summary>
    public static class DashboardUIHelper
    {
        // ---------- 颜色常量 ----------
        public static readonly Color PanelBgColor = new Color(0.05f, 0.09f, 0.16f, 0.72f);
        public static readonly Color TitleColor = new Color(0.85f, 0.92f, 1.00f, 1.00f);
        public static readonly Color AccentColor = new Color(0.20f, 0.65f, 1.00f, 1.00f);
        public static readonly Color NumberColor = new Color(0.30f, 0.80f, 1.00f, 1.00f);
        public static readonly Color LabelColor = new Color(0.60f, 0.72f, 0.85f, 0.95f);
        public static readonly Color ChartBgColor = new Color(0.07f, 0.12f, 0.20f, 0.40f);

        // ---------- 文本创建 ----------
        public static TextMeshProUGUI CreateText(Transform parent, string name, string content,
            TextStyle style, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            rt.anchorMin = Vector2.up;
            rt.anchorMax = Vector2.up;
            rt.pivot = new Vector2(0f, 1f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.raycastTarget = false;
            tmp.overflow = TextOverflowModes.Overflow;

            ApplyTextStyle(tmp, style);
            return tmp;
        }

        public static void ApplyTextStyle(TextMeshProUGUI tmp, TextStyle style)
        {
            switch (style)
            {
                case TextStyle.MainTitle:
                    tmp.fontSize = 32;
                    tmp.fontWeight = FontWeight.Bold;
                    tmp.color = TitleColor;
                    tmp.alignment = TextAlignmentOptions.Center;
                    break;
                case TextStyle.ModuleTitle:
                    tmp.fontSize = 18;
                    tmp.fontWeight = FontWeight.Bold;
                    tmp.color = TitleColor;
                    tmp.alignment = TextAlignmentOptions.Left;
                    break;
                case TextStyle.BigNumber:
                    tmp.fontSize = 42;
                    tmp.fontWeight = FontWeight.Bold;
                    tmp.color = NumberColor;
                    tmp.alignment = TextAlignmentOptions.Left;
                    break;
                case TextStyle.NumberUnit:
                    tmp.fontSize = 16;
                    tmp.fontWeight = FontWeight.Regular;
                    tmp.color = LabelColor;
                    tmp.alignment = TextAlignmentOptions.Left;
                    break;
                case TextStyle.Label:
                    tmp.fontSize = 14;
                    tmp.fontWeight = FontWeight.Regular;
                    tmp.color = LabelColor;
                    tmp.alignment = TextAlignmentOptions.Left;
                    break;
                case TextStyle.Nav:
                    tmp.fontSize = 15;
                    tmp.fontWeight = FontWeight.Medium;
                    tmp.color = LabelColor;
                    tmp.alignment = TextAlignmentOptions.Center;
                    break;
                case TextStyle.Status:
                    tmp.fontSize = 13;
                    tmp.fontWeight = FontWeight.Regular;
                    tmp.color = LabelColor;
                    tmp.alignment = TextAlignmentOptions.Left;
                    break;
            }
        }

        public enum TextStyle
        {
            MainTitle,
            ModuleTitle,
            BigNumber,
            NumberUnit,
            Label,
            Nav,
            Status
        }

        // ---------- 面板/容器创建 ----------
        public static RectTransform CreatePanel(Transform parent, string name, Color bgColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;

            return rt;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color bgColor,
            Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            rt.anchorMin = Vector2.up;
            rt.anchorMax = Vector2.up;
            rt.pivot = new Vector2(0f, 1f);

            var img = go.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;

            return rt;
        }

        // ---------- 图片占位创建 ----------
        public static DashboardImagePlaceholder CreateImagePlaceholder(Transform parent, string name,
            Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var rt = CreatePanel(parent, name, color, anchoredPosition, size);
            var ph = rt.gameObject.AddComponent<DashboardImagePlaceholder>();
            ph.imageId = name;
            ph.SetColor(color);
            return ph;
        }

        // ---------- 图表占位创建 ----------
        public static DashboardChartPlaceholder CreateChartPlaceholder(Transform parent, string name,
            Vector2 anchoredPosition, Vector2 size, DashboardChartPlaceholder.ChartType type, Color color)
        {
            var rt = CreatePanel(parent, name, color, anchoredPosition, size);
            var ph = rt.gameObject.AddComponent<DashboardChartPlaceholder>();
            ph.chartId = name;
            ph.chartType = type;
            ph.SetColor(color);
            return ph;
        }

        // ---------- 水平/垂直布局辅助 ----------
        public static RectTransform CreateHorizontalGroup(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = Vector2.up;
            rt.anchorMax = Vector2.up;
            rt.pivot = new Vector2(0f, 1f);
            return rt;
        }

        public static RectTransform CreateDivider(Transform parent, string name, Vector2 size, Color color)
        {
            return CreatePanel(parent, name, color, Vector2.zero, size);
        }
    }
}
