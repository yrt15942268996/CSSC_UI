using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CSSC.UI.Dashboard
{
    /// <summary>
    /// 数据可视化大屏主管理器：负责 Canvas、安全区域、整体布局以及六大模块的组装。
    /// 所有模块通过代码动态构建，仅搭建骨架，不填充贴图与数据。
    /// </summary>
    public class DashboardUIManager : MonoBehaviour
    {
        [Header("全局配置")]
        [Tooltip("设计参考分辨率宽度")] public float referenceWidth = 3840f;
        [Tooltip("设计参考分辨率高度")] public float referenceHeight = 2160f;
        [Tooltip("是否自动构建整个界面")] public bool buildOnAwake = true;

        [Header("模块颜色")]
        public Color panelBackground = DashboardUIHelper.PanelBgColor;
        public Color chartBackground = DashboardUIHelper.ChartBgColor;
        public Color imagePlaceholder = new Color(0.08f, 0.12f, 0.18f, 0.45f);

        // 根节点
        private Canvas _canvas;
        private RectTransform _safeArea;
        private RectTransform _topHeader;
        private RectTransform _navTabs;
        private RectTransform _mainContent;
        private RectTransform _leftColumn;
        private RectTransform _centerArea;
        private RectTransform _rightColumn;
        private RectTransform _bottomSection;
        private RectTransform _bottomStatusBar;

        // 各模块根节点
        private RectTransform _globalOverviewPanel;
        private RectTransform _orderMarketPanel;
        private RectTransform _shipDynamicsPanel;
        private RectTransform _serviceNetworkPanel;
        private RectTransform _industryStatusPanel;
        private RectTransform _customerCasePanel;

        // 文本缓存（用于外部数据注入）
        private readonly Dictionary<string, TextMeshProUGUI> _textMap = new Dictionary<string, TextMeshProUGUI>();
        private readonly Dictionary<string, DashboardChartPlaceholder> _chartMap = new Dictionary<string, DashboardChartPlaceholder>();
        private readonly Dictionary<string, DashboardImagePlaceholder> _imageMap = new Dictionary<string, DashboardImagePlaceholder>();

        private void Awake()
        {
            if (buildOnAwake)
                Build();
        }

        [ContextMenu("Build Dashboard")]
        public void Build()
        {
            ClearChildren();
            EnsureCanvas();
            EnsureSafeArea();

            BuildTopHeader();
            BuildNavTabs();
            BuildMainContent();
            BuildBottomSection();
            BuildBottomStatusBar();

            BuildGlobalOverviewModule();
            BuildOrderMarketModule();
            BuildShipDynamicsModule();
            BuildServiceNetworkModule();
            BuildIndustryStatusModule();
            BuildCustomerCaseModule();

            Debug.Log("[DashboardUIManager] 大屏 UI 框架已搭建完成。");
        }

        private void ClearChildren()
        {
            _textMap.Clear();
            _chartMap.Clear();
            _imageMap.Clear();

            if (transform.childCount == 0) return;
            var children = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
                children.Add(transform.GetChild(i));
            foreach (var child in children)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void EnsureCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
                _canvas = gameObject.AddComponent<Canvas>();

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 0;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        private void EnsureSafeArea()
        {
            if (_safeArea != null) return;
            var go = new GameObject("SafeArea", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _safeArea = go.GetComponent<RectTransform>();
            _safeArea.anchorMin = Vector2.zero;
            _safeArea.anchorMax = Vector2.one;
            _safeArea.offsetMin = new Vector2(40, 30);
            _safeArea.offsetMax = new Vector2(-40, -30);
        }

        #region 顶层布局
        private void BuildTopHeader()
        {
            _topHeader = DashboardUIHelper.CreatePanel(_safeArea, "TopHeader", Color.clear,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), new Vector2(0, 0));

            // 左侧 Logo 区域
            var logoArea = DashboardUIHelper.CreateHorizontalGroup(_topHeader, "LogoArea", new Vector2(400, 80));
            logoArea.anchoredPosition = new Vector2(0, 0);
            logoArea.pivot = new Vector2(0, 1f);
            var logoIcon = DashboardUIHelper.CreateImagePlaceholder(logoArea, "LogoIcon", new Vector2(0, 0), new Vector2(60, 60), imagePlaceholder);
            RegisterImage(logoIcon);
            var logoText = DashboardUIHelper.CreateText(logoArea, "LogoText", "未来造船集团", DashboardUIHelper.TextStyle.ModuleTitle, new Vector2(70, -10), new Vector2(300, 50));
            RegisterText("logoText", logoText);

            // 中央标题
            var title = DashboardUIHelper.CreateText(_topHeader, "MainTitle", "全球业务概览与运营指挥平台", DashboardUIHelper.TextStyle.MainTitle,
                new Vector2(0, -10), new Vector2(800, 70));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0, -10);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            RegisterText("mainTitle", title);

            // 右侧时间/天气
            var rightInfo = DashboardUIHelper.CreateHorizontalGroup(_topHeader, "RightInfo", new Vector2(400, 80));
            rightInfo.anchoredPosition = new Vector2(0, 0);
            rightInfo.anchorMin = new Vector2(1, 1);
            rightInfo.anchorMax = new Vector2(1, 1);
            rightInfo.pivot = new Vector2(1, 1);
            var timeText = DashboardUIHelper.CreateText(rightInfo, "TimeText", "2025-07-12 10:30:45", DashboardUIHelper.TextStyle.Label, new Vector2(-280, -10), new Vector2(180, 30));
            var weatherText = DashboardUIHelper.CreateText(rightInfo, "WeatherText", "上海  26°C  晴", DashboardUIHelper.TextStyle.Label, new Vector2(-90, -10), new Vector2(160, 30));
            RegisterText("timeText", timeText);
            RegisterText("weatherText", weatherText);
        }

        private void BuildNavTabs()
        {
            _navTabs = DashboardUIHelper.CreatePanel(_safeArea, "NavTabs", new Color(0.06f, 0.12f, 0.22f, 0.55f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(420, -95), new Vector2(-420, -140));

            string[] tabs = { "全球概览", "订单分析", "运营监控", "服务网络", "质量安全", "客户案例" };
            float tabWidth = 180;
            float gap = 40;
            float totalWidth = tabs.Length * tabWidth + (tabs.Length - 1) * gap;
            float startX = -(totalWidth * 0.5f);
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = DashboardUIHelper.CreateText(_navTabs, $"Tab_{tabs[i]}", tabs[i], DashboardUIHelper.TextStyle.Nav,
                    new Vector2(startX + i * (tabWidth + gap), -5), new Vector2(tabWidth, 40));
                tab.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                tab.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                tab.rectTransform.pivot = new Vector2(0f, 1f);
                RegisterText($"navTab_{i}", tab);
            }
        }

        private void BuildMainContent()
        {
            _mainContent = DashboardUIHelper.CreatePanel(_safeArea, "MainContent", Color.clear,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 180), new Vector2(0, -240));

            // 左列 30%
            _leftColumn = DashboardUIHelper.CreatePanel(_mainContent, "LeftColumn", Color.clear,
                new Vector2(0, 0), new Vector2(0.3f, 1), new Vector2(0, 0), new Vector2(-15, 0));

            // 中间 40%
            _centerArea = DashboardUIHelper.CreatePanel(_mainContent, "CenterArea", Color.clear,
                new Vector2(0.3f, 0), new Vector2(0.7f, 1), new Vector2(15, 0), new Vector2(-15, 0));

            // 右列 30%
            _rightColumn = DashboardUIHelper.CreatePanel(_mainContent, "RightColumn", Color.clear,
                new Vector2(0.7f, 0), new Vector2(1, 1), new Vector2(15, 0), new Vector2(0, 0));
        }

        private void BuildBottomSection()
        {
            _bottomSection = DashboardUIHelper.CreatePanel(_safeArea, "BottomSection", Color.clear,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 110), new Vector2(0, 170));
        }

        private void BuildBottomStatusBar()
        {
            _bottomStatusBar = DashboardUIHelper.CreatePanel(_safeArea, "BottomStatusBar", new Color(0.04f, 0.08f, 0.14f, 0.85f),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 100));
        }
        #endregion

        #region 模块一：全球业务概览
        private void BuildGlobalOverviewModule()
        {
            _globalOverviewPanel = DashboardUIHelper.CreatePanel(_leftColumn, "GlobalOverviewPanel", panelBackground,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(0, -(_leftColumn.rect.height * 0.5f + 5)));
            // 重新使用相对高度
            _globalOverviewPanel.anchorMin = new Vector2(0, 0.5f);
            _globalOverviewPanel.anchorMax = new Vector2(1, 1);
            _globalOverviewPanel.offsetMin = new Vector2(0, 5);
            _globalOverviewPanel.offsetMax = new Vector2(0, -10);

            AddModuleTitle(_globalOverviewPanel, "一、全球业务概览（企业外向型实力）");

            // 2x2 指标卡片
            float cardW = _globalOverviewPanel.rect.width * 0.5f - 10;
            float cardH = 160;
            AddMetricCard(_globalOverviewPanel, "metric_delivered", "累计交付船舶总数", "416", "艘", new Vector2(10, -45), new Vector2(cardW, cardH));
            AddMetricCard(_globalOverviewPanel, "metric_regions", "覆盖国家/地区数量", "23", "个", new Vector2(cardW + 20, -45), new Vector2(cardW, cardH));
            AddMetricCard(_globalOverviewPanel, "metric_orders", "手持订单量", "42", "艘", new Vector2(10, -45 - cardH - 10), new Vector2(cardW, cardH));
            AddMetricCard(_globalOverviewPanel, "metric_building", "在建船舶数量", "28", "艘", new Vector2(cardW + 20, -45 - cardH - 10), new Vector2(cardW, cardH));
        }
        #endregion

        #region 模块二：全球订单与市场分布
        private void BuildOrderMarketModule()
        {
            _orderMarketPanel = DashboardUIHelper.CreatePanel(_leftColumn, "OrderMarketPanel", panelBackground,
                new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(0, -5));

            AddModuleTitle(_orderMarketPanel, "二、全球订单与市场分布");

            // 订单区域占比 - 饼图占位
            var pieChart = DashboardUIHelper.CreateChartPlaceholder(_orderMarketPanel, "chart_orderRegion",
                new Vector2(20, -50), new Vector2(220, 220), DashboardChartPlaceholder.ChartType.Pie, chartBackground);
            RegisterChart(pieChart);
            AddLegend(_orderMarketPanel, "legend_orderRegion", new Vector2(260, -60), new string[] { "亚洲 41%", "欧洲 37%", "美洲 22%" });

            // 前5大船东国家 - 条形图占位
            var barChart = DashboardUIHelper.CreateChartPlaceholder(_orderMarketPanel, "chart_topShipOwners",
                new Vector2(20, -300), new Vector2(_orderMarketPanel.rect.width - 60, 200), DashboardChartPlaceholder.ChartType.Bar, chartBackground);
            RegisterChart(barChart);

            // 主要船型全球需求分布 - 图标占位
            AddShipTypeDemand(_orderMarketPanel);
        }
        #endregion

        #region 模块三：实时船舶动态
        private void BuildShipDynamicsModule()
        {
            _shipDynamicsPanel = DashboardUIHelper.CreatePanel(_rightColumn, "ShipDynamicsPanel", panelBackground,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(0, -(_rightColumn.rect.height * 0.5f + 5)));
            _shipDynamicsPanel.anchorMin = new Vector2(0, 0.5f);
            _shipDynamicsPanel.anchorMax = new Vector2(1, 1);
            _shipDynamicsPanel.offsetMin = new Vector2(0, 5);
            _shipDynamicsPanel.offsetMax = new Vector2(0, -10);

            AddModuleTitle(_shipDynamicsPanel, "三、实时船舶动态（AIS/定位数据）");

            // 实时在航船舶数量
            AddMetricCard(_shipDynamicsPanel, "metric_sailing", "实时在航船舶数量", "12", "艘", new Vector2(20, -45), new Vector2(220, 110));

            // 各海域船舶分布
            var seaChart = DashboardUIHelper.CreateChartPlaceholder(_shipDynamicsPanel, "chart_seaDistribution",
                new Vector2(260, -50), new Vector2(_shipDynamicsPanel.rect.width - 300, 160), DashboardChartPlaceholder.ChartType.Bar, chartBackground);
            RegisterChart(seaChart);

            // 船舶实时位置与航行轨迹 - 地图占位
            var mapChart = DashboardUIHelper.CreateChartPlaceholder(_shipDynamicsPanel, "chart_shipTrackMap",
                new Vector2(20, -220), new Vector2(_shipDynamicsPanel.rect.width - 60, 220), DashboardChartPlaceholder.ChartType.Map, chartBackground);
            RegisterChart(mapChart);
        }
        #endregion

        #region 模块四：全球服务网络与合作伙伴
        private void BuildServiceNetworkModule()
        {
            _serviceNetworkPanel = DashboardUIHelper.CreatePanel(_rightColumn, "ServiceNetworkPanel", panelBackground,
                new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(0, -5));

            AddModuleTitle(_serviceNetworkPanel, "四、全球服务网络与合作伙伴");

            // 海外办事处/分支机构数量
            AddMetricCard(_serviceNetworkPanel, "metric_offices", "海外办事处/分支机构数量", "12", "个", new Vector2(20, -45), new Vector2(200, 100));
            // 覆盖服务港口数量
            AddMetricCard(_serviceNetworkPanel, "metric_ports", "覆盖服务港口数量", "67", "个", new Vector2(240, -45), new Vector2(200, 100));

            // 国际主要船级社认证 - 图标占位
            var certTitle = DashboardUIHelper.CreateText(_serviceNetworkPanel, "CertTitle", "国际主要船级社认证", DashboardUIHelper.TextStyle.Label,
                new Vector2(20, -160), new Vector2(250, 30));
            string[] certs = { "CCS", "LR", "ABS", "DNV" };
            for (int i = 0; i < certs.Length; i++)
            {
                var certIcon = DashboardUIHelper.CreateImagePlaceholder(_serviceNetworkPanel, $"img_cert_{certs[i]}",
                    new Vector2(20 + i * 90, -200), new Vector2(70, 70), imagePlaceholder);
                RegisterImage(certIcon);
                var certLabel = DashboardUIHelper.CreateText(_serviceNetworkPanel, $"txt_cert_{certs[i]}", certs[i], DashboardUIHelper.TextStyle.Label,
                    new Vector2(20 + i * 90, -275), new Vector2(70, 25));
                RegisterText($"cert_{certs[i]}", certLabel);
            }

            // 全球核心供应商/合作伙伴数量
            AddMetricCard(_serviceNetworkPanel, "metric_partners", "全球核心供应商/合作伙伴数量", "150+", "家", new Vector2(20, -320), new Vector2(300, 100));
        }
        #endregion

        #region 模块五：行业地位与国际荣誉
        private void BuildIndustryStatusModule()
        {
            _industryStatusPanel = DashboardUIHelper.CreatePanel(_bottomSection, "IndustryStatusPanel", panelBackground,
                new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(-5, 0));

            AddModuleTitle(_industryStatusPanel, "五、行业地位与国际荣誉");

            // 全球造船企业排名
            AddMetricCard(_industryStatusPanel, "metric_rank", "全球造船企业排名", "4", "位", new Vector2(20, -45), new Vector2(200, 130));
            // 细分船型市场占有率
            AddMetricCard(_industryStatusPanel, "metric_share", "细分船型市场占有率", "15", "%", new Vector2(240, -45), new Vector2(200, 130));

            // 国际认证 - 占位
            var isoTitle = DashboardUIHelper.CreateText(_industryStatusPanel, "IsoTitle", "国际质量/环境/安全标准认证", DashboardUIHelper.TextStyle.Label,
                new Vector2(470, -50), new Vector2(280, 30));
            string[] isos = { "ISO 9001", "ISO 14001", "ISO 45001" };
            for (int i = 0; i < isos.Length; i++)
            {
                var isoIcon = DashboardUIHelper.CreateImagePlaceholder(_industryStatusPanel, $"img_iso_{isos[i]}",
                    new Vector2(470 + i * 100, -90), new Vector2(80, 80), imagePlaceholder);
                RegisterImage(isoIcon);
                var isoLabel = DashboardUIHelper.CreateText(_industryStatusPanel, $"txt_iso_{isos[i]}", isos[i], DashboardUIHelper.TextStyle.Label,
                    new Vector2(470 + i * 100, -180), new Vector2(80, 45));
                RegisterText($"iso_{isos[i]}", isoLabel);
            }
        }
        #endregion

        #region 模块六：全球客户与项目案例
        private void BuildCustomerCaseModule()
        {
            _customerCasePanel = DashboardUIHelper.CreatePanel(_bottomSection, "CustomerCasePanel", panelBackground,
                new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(5, 0), new Vector2(0, 0));

            AddModuleTitle(_customerCasePanel, "六、全球客户与项目案例（可选）");

            // 代表性国际客户 LOGO
            var clientTitle = DashboardUIHelper.CreateText(_customerCasePanel, "ClientTitle", "代表性国际客户 LOGO", DashboardUIHelper.TextStyle.Label,
                new Vector2(20, -45), new Vector2(220, 30));
            string[] clients = { "MAERSK", "MSC", "COSCO", "Hapag-Lloyd" };
            for (int i = 0; i < clients.Length; i++)
            {
                var clientLogo = DashboardUIHelper.CreateImagePlaceholder(_customerCasePanel, $"img_client_{clients[i]}",
                    new Vector2(20 + i * 110, -85), new Vector2(90, 50), imagePlaceholder);
                RegisterImage(clientLogo);
            }

            // 标志性出口船型项目
            var projectTitle = DashboardUIHelper.CreateText(_customerCasePanel, "ProjectTitle", "标志性出口船型项目", DashboardUIHelper.TextStyle.Label,
                new Vector2(20, -160), new Vector2(220, 30));
            var projectImage = DashboardUIHelper.CreateImagePlaceholder(_customerCasePanel, "img_project",
                new Vector2(20, -200), new Vector2(220, 100), imagePlaceholder);
            RegisterImage(projectImage);
            var projectText = DashboardUIHelper.CreateText(_customerCasePanel, "ProjectText", "\"沪东型\"超大型集装箱船系列\n已交付 32 艘 / 在建 8 艘", DashboardUIHelper.TextStyle.Label,
                new Vector2(260, -200), new Vector2(280, 80));
            RegisterText("project_info", projectText);
        }
        #endregion

        #region 辅助构建方法
        private void AddModuleTitle(RectTransform parent, string title)
        {
            var titleText = DashboardUIHelper.CreateText(parent, "Title", title, DashboardUIHelper.TextStyle.ModuleTitle,
                new Vector2(15, -12), new Vector2(parent.rect.width - 40, 30));
            RegisterText($"title_{parent.name}", titleText);
        }

        private void AddMetricCard(RectTransform parent, string id, string label, string value, string unit, Vector2 pos, Vector2 size)
        {
            var card = DashboardUIHelper.CreatePanel(parent, $"Card_{id}", new Color(0.08f, 0.14f, 0.24f, 0.35f), pos, size);
            var labelText = DashboardUIHelper.CreateText(card, "Label", label, DashboardUIHelper.TextStyle.Label, new Vector2(10, -10), new Vector2(size.x - 20, 25));
            var valueText = DashboardUIHelper.CreateText(card, "Value", value, DashboardUIHelper.TextStyle.BigNumber, new Vector2(10, -40), new Vector2(120, 55));
            var unitText = DashboardUIHelper.CreateText(card, "Unit", unit, DashboardUIHelper.TextStyle.NumberUnit, new Vector2(130, -60), new Vector2(60, 30));
            RegisterText($"{id}_label", labelText);
            RegisterText($"{id}_value", valueText);
            RegisterText($"{id}_unit", unitText);
        }

        private void AddLegend(RectTransform parent, string id, Vector2 pos, string[] items)
        {
            var legend = DashboardUIHelper.CreateHorizontalGroup(parent, $"Legend_{id}", new Vector2(200, 120));
            legend.anchoredPosition = pos;
            for (int i = 0; i < items.Length; i++)
            {
                var item = DashboardUIHelper.CreateText(legend, $"Legend_{i}", items[i], DashboardUIHelper.TextStyle.Label,
                    new Vector2(0, -i * 28), new Vector2(180, 25));
                RegisterText($"{id}_{i}", item);
            }
        }

        private void AddShipTypeDemand(RectTransform parent)
        {
            var title = DashboardUIHelper.CreateText(parent, "ShipTypeTitle", "主要船型全球需求分布", DashboardUIHelper.TextStyle.Label,
                new Vector2(20, -520), new Vector2(220, 25));
            string[] types = { "集装箱船", "LNG船", "邮轮", "散货船" };
            string[] ratios = { "40%", "30%", "20%", "10%" };
            for (int i = 0; i < types.Length; i++)
            {
                var icon = DashboardUIHelper.CreateImagePlaceholder(parent, $"img_shipType_{types[i]}",
                    new Vector2(20 + i * 110, -560), new Vector2(70, 50), imagePlaceholder);
                RegisterImage(icon);
                var nameText = DashboardUIHelper.CreateText(parent, $"txt_shipType_{types[i]}", types[i], DashboardUIHelper.TextStyle.Label,
                    new Vector2(20 + i * 110, -620), new Vector2(70, 25));
                var ratioText = DashboardUIHelper.CreateText(parent, $"txt_shipTypeRatio_{types[i]}", ratios[i], DashboardUIHelper.TextStyle.NumberUnit,
                    new Vector2(20 + i * 110, -645), new Vector2(70, 25));
                RegisterText($"shipType_{types[i]}", nameText);
                RegisterText($"shipTypeRatio_{types[i]}", ratioText);
            }
        }
        #endregion

        #region 注册/数据注入
        private void RegisterText(string id, TextMeshProUGUI text)
        {
            if (!_textMap.ContainsKey(id))
                _textMap.Add(id, text);
        }

        private void RegisterChart(DashboardChartPlaceholder chart)
        {
            if (!_chartMap.ContainsKey(chart.chartId))
                _chartMap.Add(chart.chartId, chart);
        }

        private void RegisterImage(DashboardImagePlaceholder image)
        {
            if (!_imageMap.ContainsKey(image.imageId))
                _imageMap.Add(image.imageId, image);
        }

        /// <summary>
        /// 运行时注入文本数据。
        /// </summary>
        public void SetText(string id, string value)
        {
            if (_textMap.TryGetValue(id, out var text))
                text.text = value;
        }

        /// <summary>
        /// 运行时注入图表数据（需挂载 XCharts 组件后调用）。
        /// </summary>
        public void SetChartData(string chartId, object data)
        {
            if (_chartMap.TryGetValue(chartId, out var chart))
                chart.InitChart(data);
        }

        /// <summary>
        /// 运行时注入图片资源。
        /// </summary>
        public void SetImageSprite(string imageId, Sprite sprite)
        {
            if (_imageMap.TryGetValue(imageId, out var image))
                image.SetSprite(sprite);
        }
        #endregion
    }
}
