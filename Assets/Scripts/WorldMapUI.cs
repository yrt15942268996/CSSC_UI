using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CSSC_UI
{
    /// <summary>
    /// 世界地图UI组件（独立模块，不依赖其他脚本，也不被其他脚本修改）
    /// 
    /// 【重要】本脚本设计为完全自包含模块：
    /// - 不依赖项目中任何其他脚本
    /// - 所有内部状态均为 private，外部只能通过设计好的公共API交互
    /// - 使用 sealed 关键字防止被继承修改行为
    /// - 使用 DisallowMultipleComponent 防止在同一GameObject上重复挂载
    /// - 使用独立命名空间 CSSC_UI 避免与其他脚本类名冲突
    /// 
    /// 挂载在Canvas下的Panel上，大小为196x90像素
    /// 功能：
    /// 1. 显示一张可更改颜色的世界地图背景
    /// 2. 在每个国家中心点放置直径6像素的圆形标记
    /// 3. 圆形初始隐藏，通过读取JSON表格中列出的国家名来显示对应的圆形
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldMapUI : MonoBehaviour
    {
        // ---- Inspector可配置字段（仅通过Inspector设置，外部脚本不应直接访问）----
        [Header("地图设置")]
        [SerializeField] private Color mapColor = new Color(0.3f, 0.5f, 0.7f, 1f);
        [SerializeField] private Vector2 mapSize = new Vector2(196f, 90f);
        [SerializeField] private bool overrideMapSize = true;
        [SerializeField] private string jsonFilePath = "countries.json";

        [Header("标记设置")]
        [SerializeField] private Color markerColor = Color.red;
        [SerializeField] private float markerDiameter = 6f;

        [Header("呼吸感动画")]
        [SerializeField] private bool enableBreathingAnimation = true;
        [SerializeField] private float breathingMinScale = 0.5f;
        [SerializeField] private float breathingMaxScale = 1.2f;
        [SerializeField] private float breathingDuration = 1.2f;

        // ---- 国家中心点坐标（相对于地图的归一化坐标 0-1）----
        // 如需添加新国家，请在此字典中追加，不要修改其他任何逻辑
        private static readonly Dictionary<string, Vector2> CountryCenters = new Dictionary<string, Vector2>
    {
        // 北美洲
        { "United States", new Vector2(0.18f, 0.30f) },
        { "Canada", new Vector2(0.20f, 0.15f) },
        { "Mexico", new Vector2(0.16f, 0.48f) },

        // 南美洲
        { "Brazil", new Vector2(0.30f, 0.65f) },
        { "Argentina", new Vector2(0.27f, 0.80f) },
        { "Colombia", new Vector2(0.23f, 0.56f) },
        { "Chile", new Vector2(0.25f, 0.78f) },
        { "Peru", new Vector2(0.23f, 0.66f) },
        { "Venezuela", new Vector2(0.26f, 0.55f) },

        // 欧洲
        { "United Kingdom", new Vector2(0.43f, 0.22f) },
        { "France", new Vector2(0.45f, 0.28f) },
        { "Germany", new Vector2(0.48f, 0.24f) },
        { "Italy", new Vector2(0.49f, 0.32f) },
        { "Spain", new Vector2(0.43f, 0.34f) },
        { "Portugal", new Vector2(0.40f, 0.34f) },
        { "Netherlands", new Vector2(0.47f, 0.22f) },
        { "Belgium", new Vector2(0.46f, 0.24f) },
        { "Switzerland", new Vector2(0.48f, 0.28f) },
        { "Austria", new Vector2(0.50f, 0.27f) },
        { "Poland", new Vector2(0.52f, 0.22f) },
        { "Sweden", new Vector2(0.50f, 0.12f) },
        { "Norway", new Vector2(0.49f, 0.14f) },
        { "Finland", new Vector2(0.54f, 0.13f) },
        { "Denmark", new Vector2(0.49f, 0.20f) },
        { "Greece", new Vector2(0.53f, 0.35f) },
        { "Turkey", new Vector2(0.56f, 0.33f) },
        { "Russia", new Vector2(0.65f, 0.18f) },
        { "Ukraine", new Vector2(0.55f, 0.24f) },
        { "Czech Republic", new Vector2(0.50f, 0.25f) },
        { "Hungary", new Vector2(0.52f, 0.27f) },
        { "Romania", new Vector2(0.54f, 0.28f) },
        { "Ireland", new Vector2(0.41f, 0.20f) },

        // 亚洲
        { "China", new Vector2(0.72f, 0.30f) },
        { "Japan", new Vector2(0.83f, 0.28f) },
        { "South Korea", new Vector2(0.80f, 0.27f) },
        { "North Korea", new Vector2(0.80f, 0.25f) },
        { "India", new Vector2(0.68f, 0.42f) },
        { "Pakistan", new Vector2(0.65f, 0.38f) },
        { "Bangladesh", new Vector2(0.70f, 0.42f) },
        { "Thailand", new Vector2(0.74f, 0.45f) },
        { "Vietnam", new Vector2(0.76f, 0.43f) },
        { "Indonesia", new Vector2(0.77f, 0.55f) },
        { "Malaysia", new Vector2(0.75f, 0.50f) },
        { "Philippines", new Vector2(0.80f, 0.46f) },
        { "Myanmar", new Vector2(0.73f, 0.42f) },
        { "Mongolia", new Vector2(0.74f, 0.22f) },
        { "Kazakhstan", new Vector2(0.63f, 0.26f) },
        { "Iran", new Vector2(0.60f, 0.35f) },
        { "Iraq", new Vector2(0.58f, 0.36f) },
        { "Saudi Arabia", new Vector2(0.59f, 0.42f) },
        { "Israel", new Vector2(0.56f, 0.37f) },
        { "United Arab Emirates", new Vector2(0.60f, 0.42f) },
        { "Afghanistan", new Vector2(0.64f, 0.35f) },

        // 非洲
        { "Egypt", new Vector2(0.54f, 0.42f) },
        { "South Africa", new Vector2(0.53f, 0.76f) },
        { "Nigeria", new Vector2(0.49f, 0.52f) },
        { "Kenya", new Vector2(0.55f, 0.58f) },
        { "Ethiopia", new Vector2(0.56f, 0.54f) },
        { "Tanzania", new Vector2(0.55f, 0.62f) },
        { "Algeria", new Vector2(0.47f, 0.40f) },
        { "Morocco", new Vector2(0.43f, 0.38f) },
        { "Libya", new Vector2(0.50f, 0.40f) },
        { "Sudan", new Vector2(0.54f, 0.48f) },
        { "Ghana", new Vector2(0.46f, 0.51f) },
        { "Angola", new Vector2(0.50f, 0.65f) },
        { "Madagascar", new Vector2(0.58f, 0.72f) },
        { "DR Congo", new Vector2(0.52f, 0.58f) },
        { "Cameroon", new Vector2(0.50f, 0.53f) },

        // 大洋洲
        { "Australia", new Vector2(0.80f, 0.72f) },
        { "New Zealand", new Vector2(0.88f, 0.80f) },

        // 中美洲/加勒比
        { "Cuba", new Vector2(0.22f, 0.42f) },
        { "Panama", new Vector2(0.22f, 0.53f) },
    };

        // ---- 内部状态（私有，外部不可访问）----
        private RectTransform _rectTransform;
        private Image _mapBackground;
        private Transform _markersContainer;
        private readonly List<GameObject> _markers = new List<GameObject>();
        private readonly HashSet<string> _activeCountries = new HashSet<string>();
        private readonly List<Coroutine> _breathingCoroutines = new List<Coroutine>();
        private bool _isDestroyed = false;

        // ---- Unity生命周期 ----

        private void Awake()
        {
            SetupMapContainer();
            CreateMapBackground();
            CreateMarkersContainer();
            CreateAllMarkers();
        }

        private void Start()
        {
            LoadCountryData();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            StopAllBreathing();
        }

        // ---- 内部初始化方法 ----

        /// <summary>
        /// 设置地图容器
        /// </summary>
        private void SetupMapContainer()
        {
            _rectTransform = GetComponent<RectTransform>();

            // 只在Inspector勾选"Override Map Size"时，强制设置为196x90
            // 否则保留用户在RectTransform上手动设置的尺寸
            if (overrideMapSize)
            {
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _rectTransform.sizeDelta = mapSize;
                _rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                // 即使不强制覆盖尺寸，也把锚点改为Middle-Center，避免Stretch模式影响坐标计算
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>
        /// 创建地图背景（纯色块，模拟世界地图背景）
        /// </summary>
        private void CreateMapBackground()
        {
            GameObject bgObj = new GameObject("MapBackground");
            bgObj.transform.SetParent(transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _mapBackground = bgObj.AddComponent<Image>();
            _mapBackground.color = mapColor;
        }

        /// <summary>
        /// 创建标记容器
        /// </summary>
        private void CreateMarkersContainer()
        {
            GameObject containerObj = new GameObject("Markers");
            containerObj.transform.SetParent(transform, false);

            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            _markersContainer = containerObj.transform;
        }

        /// <summary>
        /// 为所有国家创建圆形标记（初始隐藏）
        /// </summary>
        private void CreateAllMarkers()
        {
            foreach (var kvp in CountryCenters)
            {
                CreateMarker(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 创建单个圆形标记
        /// </summary>
        private GameObject CreateMarker(string countryName, Vector2 normalizedPosition)
        {
            GameObject markerObj = new GameObject($"Marker_{countryName}");
            markerObj.transform.SetParent(_markersContainer, false);

            RectTransform markerRect = markerObj.AddComponent<RectTransform>();

            // 将归一化坐标转换为相对于地图的本地坐标
            float xPos = (normalizedPosition.x - 0.5f) * mapSize.x;
            float yPos = (normalizedPosition.y - 0.5f) * mapSize.y;
            markerRect.anchoredPosition = new Vector2(xPos, yPos);
            markerRect.sizeDelta = new Vector2(markerDiameter, markerDiameter);

            // 创建圆形图像
            Image markerImage = markerObj.AddComponent<Image>();
            markerImage.color = markerColor;

            // 使用程序化生成的圆形Sprite
            markerImage.sprite = CreateCircleSprite((int)markerDiameter);

            // 初始隐藏
            markerObj.SetActive(false);

            _markers.Add(markerObj);

            return markerObj;
        }

        /// <summary>
        /// 程序化生成圆形Sprite
        /// </summary>
        private Sprite CreateCircleSprite(int diameter)
        {
            int texSize = Mathf.NextPowerOfTwo(diameter);
            Texture2D texture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[texSize * texSize];
            float radius = diameter / 2f;
            float centerX = texSize / 2f;
            float centerY = texSize / 2f;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (dist <= radius)
                    {
                        // 边缘抗锯齿
                        float alpha = 1f - Mathf.Clamp01((dist - radius + 1f) / 1f);
                        pixels[y * texSize + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[y * texSize + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Rect rect = new Rect(0, 0, texSize, texSize);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
        }

        // ---- JSON数据加载 ----

        /// <summary>
        /// 加载JSON国家数据并显示对应标记
        /// </summary>
        public void LoadCountryData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, jsonFilePath);

            if (!File.Exists(fullPath))
            {
                // 尝试从Resources加载
                TextAsset jsonAsset = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(jsonFilePath));
                if (jsonAsset != null)
                {
                    ProcessCountryJson(jsonAsset.text);
                    return;
                }

                Debug.LogWarning($"[WorldMapUI] JSON文件未找到: {fullPath}");
                return;
            }

            string jsonContent = File.ReadAllText(fullPath);
            ProcessCountryJson(jsonContent);
        }

        /// <summary>
        /// 直接通过JSON字符串加载数据
        /// </summary>
        public void LoadCountryDataFromJson(string jsonContent)
        {
            ProcessCountryJson(jsonContent);
        }

        /// <summary>
        /// 处理JSON数据
        /// </summary>
        private void ProcessCountryJson(string jsonContent)
        {
            try
            {
                CountryListData data = JsonUtility.FromJson<CountryListData>(jsonContent);

                if (data == null || data.countries == null || data.countries.Length == 0)
                {
                    Debug.LogWarning("[WorldMapUI] JSON数据为空或无国家列表");
                    return;
                }

                _activeCountries.Clear();
                foreach (string country in data.countries)
                {
                    _activeCountries.Add(country.Trim());
                }

                UpdateMarkers();
                Debug.Log($"[WorldMapUI] 已加载 {_activeCountries.Count} 个国家数据");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WorldMapUI] JSON解析失败: {e.Message}");
            }
        }

        /// <summary>
        /// 根据_activeCountries更新标记显示状态，并启动呼吸感动画
        /// </summary>
        private void UpdateMarkers()
        {
            StopAllBreathing();

            foreach (GameObject marker in _markers)
            {
                // 从名称中提取国家名: "Marker_China" -> "China"
                string countryName = marker.name.Replace("Marker_", "");
                bool shouldShow = _activeCountries.Contains(countryName);
                marker.SetActive(shouldShow);

                // 对显示的国家圆形启动呼吸感动画
                if (shouldShow && enableBreathingAnimation)
                {
                    Coroutine cr = StartCoroutine(BreathingAnimation(marker));
                    _breathingCoroutines.Add(cr);
                }
                else if (!shouldShow)
                {
                    // 隐藏的标记重置缩放
                    marker.transform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// 呼吸感动画协程：在minScale和maxScale之间循环缩放
        /// 该动画持续运行直到GameObject被销毁或该标记被隐藏
        /// </summary>
        private System.Collections.IEnumerator BreathingAnimation(GameObject marker)
        {
            if (marker == null) yield break;

            Transform markerTransform = marker.transform;
            float elapsed = 0f;
            bool expanding = true;

            while (!_isDestroyed && marker != null && marker.activeSelf)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / breathingDuration;

                if (t >= 1f)
                {
                    t = 0f;
                    elapsed = 0f;
                    expanding = !expanding;
                }

                // 使用 SmoothStep 实现平滑缓动
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float scale;

                if (expanding)
                {
                    scale = Mathf.Lerp(breathingMinScale, breathingMaxScale, smoothT);
                }
                else
                {
                    scale = Mathf.Lerp(breathingMaxScale, breathingMinScale, smoothT);
                }

                markerTransform.localScale = Vector3.one * scale;

                yield return null;
            }

            // 协程退出时重置缩放
            if (marker != null)
            {
                marker.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 停止所有呼吸感动画
        /// </summary>
        private void StopAllBreathing()
        {
            foreach (Coroutine cr in _breathingCoroutines)
            {
                if (cr != null)
                {
                    StopCoroutine(cr);
                }
            }
            _breathingCoroutines.Clear();
        }

        // ---- 公共API（外部脚本只能通过这些方法与本模块交互）----

        /// <summary>
        /// 更改地图背景颜色
        /// </summary>
        /// <param name="color">目标颜色</param>
        public void SetMapColor(Color color)
        {
            mapColor = color;
            if (_mapBackground != null)
            {
                _mapBackground.color = color;
            }
        }

        /// <summary>
        /// 更改标记颜色
        /// </summary>
        /// <param name="color">目标颜色</param>
        public void SetMarkerColor(Color color)
        {
            markerColor = color;
            foreach (GameObject marker in _markers)
            {
                Image img = marker.GetComponent<Image>();
                if (img != null)
                {
                    img.color = color;
                }
            }
        }

        /// <summary>
        /// 更改标记直径大小
        /// </summary>
        /// <param name="diameter">目标直径（像素）</param>
        public void SetMarkerDiameter(float diameter)
        {
            markerDiameter = diameter;
            foreach (GameObject marker in _markers)
            {
                RectTransform rect = marker.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(diameter, diameter);
                }
            }
        }

        /// <summary>
        /// 获取所有已激活的国家列表（返回副本，防止外部修改内部状态）
        /// </summary>
        public HashSet<string> GetActiveCountries()
        {
            return new HashSet<string>(_activeCountries);
        }

        /// <summary>
        /// 清除所有标记
        /// </summary>
        public void ClearAllMarkers()
        {
            _activeCountries.Clear();
            UpdateMarkers();
        }

        /// <summary>
        /// 更改地图容器尺寸（如需修改196x90大小可调用）
        /// </summary>
        /// <param name="newSize">新的地图尺寸</param>
        /// <param name="applyToRectTransform">是否同时应用到RectTransform</param>
        public void SetMapSize(Vector2 newSize, bool applyToRectTransform = true)
        {
            mapSize = newSize;
            if (applyToRectTransform && _rectTransform != null)
            {
                _rectTransform.sizeDelta = newSize;
            }
            // 标记点位置根据新尺寸重新计算
            foreach (GameObject marker in _markers)
            {
                if (marker == null) continue;

                RectTransform rect = marker.GetComponent<RectTransform>();
                string countryName = marker.name.Replace("Marker_", "");
                if (rect != null && CountryCenters.TryGetValue(countryName, out Vector2 normalizedPos))
                {
                    rect.anchoredPosition = new Vector2(
                        (normalizedPos.x - 0.5f) * mapSize.x,
                        (normalizedPos.y - 0.5f) * mapSize.y);
                }
            }
        }

        // ---- JSON数据结构（私有，仅内部使用）----

        [System.Serializable]
        private sealed class CountryListData
        {
            public string[] countries;
        }
    }
}
