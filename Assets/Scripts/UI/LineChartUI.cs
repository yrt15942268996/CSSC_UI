using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CSSC_UI
{
    /// <summary>
    /// 折线图UI组件（独立模块，不依赖其他脚本，也不被其他脚本修改）
    /// 
    /// 【重要】本脚本设计为完全自包含模块：
    /// - 不依赖项目中任何其他脚本
    /// - 所有内部状态均为 private，外部只能通过设计好的公共API交互
    /// - 使用 sealed 关键字防止被继承修改行为
    /// - 使用 DisallowMultipleComponent 防止在同一GameObject上重复挂载
    /// - 使用独立命名空间 CSSC_UI 避免与其他脚本类名冲突
    /// 
    /// 功能：
    /// 1. 11个数据点，直接使用像素坐标(X,Y)定位，图表大小190x76
    /// 2. 从左到右依次连线，每到一个点显示直径3.2px圆形
    /// 3. 连线完成后在折线上做流光循环动画
    /// 4. 面板销毁时自动停止所有动画
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class LineChartUI : MonoBehaviour
    {
        // ---- 数据配置 ----
        [Header("数据配置")]
        [Tooltip("是否从JSON文件加载配置。勾选后运行时会用JSON数据覆盖Inspector中的值")]
        [SerializeField] private bool useJsonConfig = false;
        [SerializeField] private string jsonFilePath = "linechart_config.json";
        [SerializeField] private Vector2[] pointPositions = new Vector2[11]
        {
            new Vector2(10f, 38f),
            new Vector2(28f, 30f),
            new Vector2(46f, 50f),
            new Vector2(64f, 20f),
            new Vector2(82f, 45f),
            new Vector2(100f, 35f),
            new Vector2(118f, 55f),
            new Vector2(136f, 25f),
            new Vector2(154f, 48f),
            new Vector2(172f, 32f),
            new Vector2(190f, 40f),
        };

        [Header("图表外观")]
        [SerializeField] private Vector2 chartSize = new Vector2(190f, 76f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        [SerializeField] private Color lineColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private float lineWidth = 2f;
        [SerializeField] private Color dotColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float dotDiameter = 3.2f;

        [Header("连线动画")]
        [SerializeField] private float drawDuration = 1.5f;

        [Header("流光动画")]
        [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private float glowSpeed = 2f;
        [SerializeField] private float glowWidth = 30f;
        [SerializeField] private bool enableGlow = true;

        // ---- 内部状态（私有，外部不可访问）----
        private RectTransform _rectTransform;
        private Image _background;
        private RectTransform _chartArea;

        // 折线相关
        private readonly List<GameObject> _lineSegments = new List<GameObject>();
        private readonly List<GameObject> _dots = new List<GameObject>();
        private readonly List<Vector2> _calculatedPositions = new List<Vector2>();
        private GameObject _fullLineObject;

        // 动画相关
        private Coroutine _drawCoroutine;
        private Coroutine _glowCoroutine;
        private readonly List<Coroutine> _activeCoroutines = new List<Coroutine>();
        private bool _isDestroyed = false;
        private bool _drawComplete = false;

        // ---- 数据模型 ----
        [System.Serializable]
        private sealed class LineChartConfig
        {
            public PointData[] points;
            public string lineColorHex;
            public float drawDuration;
        }

        [System.Serializable]
        private sealed class PointData
        {
            public float x;
            public float y;
        }

        // ---- Unity生命周期 ----

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            SetupBackground();
            CreateChartArea();
        }

        private void Start()
        {
            // 只有勾选了useJsonConfig才会从JSON加载覆盖Inspector的值
            if (useJsonConfig && !string.IsNullOrEmpty(jsonFilePath))
            {
                LoadConfigFromJson();
            }

            // 启动绘制动画
            StartDrawAnimation();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            StopAllCoroutinesInternal();
        }

        // ---- 初始化 ----

        private void SetupBackground()
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _background = bgObj.AddComponent<Image>();
            _background.color = backgroundColor;
        }

        private void CreateChartArea()
        {
            GameObject areaObj = new GameObject("ChartArea");
            areaObj.transform.SetParent(transform, false);

            _chartArea = areaObj.AddComponent<RectTransform>();
            _chartArea.anchorMin = new Vector2(0.5f, 0.5f);
            _chartArea.anchorMax = new Vector2(0.5f, 0.5f);
            _chartArea.pivot = new Vector2(0.5f, 0.5f);
            _chartArea.sizeDelta = chartSize;
            _chartArea.anchoredPosition = Vector2.zero;
        }

        // ---- JSON配置加载 ----

        private void LoadConfigFromJson()
        {
            string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, jsonFilePath);

            if (!System.IO.File.Exists(fullPath))
            {
                // 尝试从Resources加载
                TextAsset jsonAsset = Resources.Load<TextAsset>(
                    System.IO.Path.GetFileNameWithoutExtension(jsonFilePath));
                if (jsonAsset != null)
                {
                    ApplyConfigJson(jsonAsset.text);
                    return;
                }
                Debug.Log($"[LineChartUI] 未找到配置文件 {jsonFilePath}，使用Inspector默认值");
                return;
            }

            string jsonContent = System.IO.File.ReadAllText(fullPath);
            ApplyConfigJson(jsonContent);
        }

        private void ApplyConfigJson(string jsonContent)
        {
            try
            {
                LineChartConfig config = JsonUtility.FromJson<LineChartConfig>(jsonContent);
                if (config == null) return;

                if (config.points != null && config.points.Length == 11)
                {
                    pointPositions = new Vector2[11];
                    for (int i = 0; i < 11; i++)
                    {
                        pointPositions[i] = new Vector2(config.points[i].x, config.points[i].y);
                    }
                }
                if (config.drawDuration > 0f)
                {
                    drawDuration = config.drawDuration;
                }
                if (!string.IsNullOrEmpty(config.lineColorHex))
                {
                    if (ColorUtility.TryParseHtmlString(config.lineColorHex, out Color parsedColor))
                    {
                        lineColor = parsedColor;
                    }
                }
                Debug.Log($"[LineChartUI] 已从JSON加载配置");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LineChartUI] JSON解析失败: {e.Message}");
            }
        }

        // ---- 绘制动画 ----

        private void StartDrawAnimation()
        {
            if (_drawCoroutine != null)
            {
                StopCoroutine(_drawCoroutine);
            }
            _drawCoroutine = StartCoroutine(DrawAnimation());
            _activeCoroutines.Add(_drawCoroutine);
        }

        private System.Collections.IEnumerator DrawAnimation()
        {
            ClearChart();

            // 将像素坐标转换为以chartArea中心为原点的本地坐标
            CalculateLocalPositions();

            // 逐点连线（共11个点，10段线）
            for (int i = 0; i < pointPositions.Length; i++)
            {
                // 在第i个点处画圆
                GameObject dot = CreateDot(_calculatedPositions[i], i);
                _dots.Add(dot);

                // 从第i个点连到第i+1个点
                if (i < pointPositions.Length - 1)
                {
                    float segmentDuration = drawDuration / (pointPositions.Length - 1);
                    yield return StartCoroutine(
                        DrawLineSegment(_calculatedPositions[i], _calculatedPositions[i + 1], segmentDuration));
                }
            }

            _drawComplete = true;
            Debug.Log("[LineChartUI] 折线绘制完成");

            // 启动流光动画
            if (enableGlow)
            {
                StartGlowAnimation();
            }
        }

        /// <summary>
        /// 将用户配置的像素坐标转换为chartArea内的本地坐标（中心为原点）
        /// 坐标原点为chartArea左下角，X向右，Y向上
        /// </summary>
        private void CalculateLocalPositions()
        {
            _calculatedPositions.Clear();

            float halfW = chartSize.x / 2f;
            float halfH = chartSize.y / 2f;

            for (int i = 0; i < pointPositions.Length; i++)
            {
                // 用户输入的坐标以chartArea左下角为原点(0,0)
                // 转换为以中心为原点的本地坐标
                float localX = pointPositions[i].x - halfW;
                float localY = pointPositions[i].y - halfH;
                _calculatedPositions.Add(new Vector2(localX, localY));
            }
        }

        /// <summary>
        /// 创建圆形标记点
        /// </summary>
        private GameObject CreateDot(Vector2 position, int index)
        {
            GameObject dotObj = new GameObject($"Dot_{index}");
            dotObj.transform.SetParent(_chartArea, false);

            RectTransform dotRect = dotObj.AddComponent<RectTransform>();
            dotRect.anchoredPosition = position;
            dotRect.sizeDelta = new Vector2(dotDiameter, dotDiameter);

            Image dotImage = dotObj.AddComponent<Image>();
            dotImage.color = dotColor;
            dotImage.sprite = CreateCircleSprite((int)dotDiameter);

            dotObj.transform.localScale = Vector3.one;

            return dotObj;
        }

        /// <summary>
        /// 在两个点之间画线（带动画）
        /// </summary>
        private System.Collections.IEnumerator DrawLineSegment(Vector2 start, Vector2 end, float duration)
        {
            GameObject lineObj = new GameObject($"LineSegment_{_lineSegments.Count}");
            lineObj.transform.SetParent(_chartArea, false);

            RectTransform lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.pivot = new Vector2(0f, 0.5f);
            lineRect.anchoredPosition = start;
            lineRect.sizeDelta = new Vector2(0f, lineWidth);

            Image lineImage = lineObj.AddComponent<Image>();
            lineImage.color = lineColor;

            _lineSegments.Add(lineObj);

            float elapsed = 0f;
            float totalLength = Vector2.Distance(start, end);
            Vector2 direction = (end - start).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            lineRect.rotation = Quaternion.Euler(0f, 0f, angle);

            while (elapsed < duration && !_isDestroyed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentLength = Mathf.Lerp(0f, totalLength, t);
                lineRect.sizeDelta = new Vector2(currentLength, lineWidth);
                yield return null;
            }

            if (!_isDestroyed)
            {
                lineRect.sizeDelta = new Vector2(totalLength, lineWidth);
            }
        }

        // ---- 流光动画 ----

        private void StartGlowAnimation()
        {
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
            }

            CreateFullLineForGlow();
            _glowCoroutine = StartCoroutine(GlowAnimation());
            _activeCoroutines.Add(_glowCoroutine);
        }

        /// <summary>
        /// 创建一条完整的折线用于流光效果
        /// </summary>
        private void CreateFullLineForGlow()
        {
            if (_calculatedPositions.Count < 2) return;

            _fullLineObject = new GameObject("GlowLine");
            _fullLineObject.transform.SetParent(_chartArea, false);

            RectTransform glowRect = _fullLineObject.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            GlowLineRenderer renderer = _fullLineObject.AddComponent<GlowLineRenderer>();
            renderer.Initialize(_calculatedPositions, lineWidth, glowColor, glowSpeed, glowWidth);
        }

        private System.Collections.IEnumerator GlowAnimation()
        {
            while (!_isDestroyed && enableGlow)
            {
                yield return null;
            }
        }

        // ---- 工具方法 ----

        private void ClearChart()
        {
            _drawComplete = false;

            foreach (GameObject obj in _lineSegments)
            {
                if (obj != null) Destroy(obj);
            }
            _lineSegments.Clear();

            foreach (GameObject obj in _dots)
            {
                if (obj != null) Destroy(obj);
            }
            _dots.Clear();

            if (_fullLineObject != null)
            {
                Destroy(_fullLineObject);
                _fullLineObject = null;
            }
        }

        private void StopAllCoroutinesInternal()
        {
            foreach (Coroutine cr in _activeCoroutines)
            {
                if (cr != null)
                {
                    StopCoroutine(cr);
                }
            }
            _activeCoroutines.Clear();
            _drawCoroutine = null;
            _glowCoroutine = null;
        }

        private Sprite CreateCircleSprite(int diameter)
        {
            int texSize = Mathf.NextPowerOfTwo(diameter);
            if (texSize < 4) texSize = 4;

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

        // ---- 公共API ----

        /// <summary>
        /// 重新设置11个像素坐标点并重新绘制
        /// 坐标原点为chartArea左下角，X向右，Y向上
        /// </summary>
        public void SetPointPositions(Vector2[] positions)
        {
            if (positions == null || positions.Length != 11)
            {
                Debug.LogWarning("[LineChartUI] 坐标点必须为11个");
                return;
            }
            pointPositions = positions;
            StartDrawAnimation();
        }

        /// <summary>
        /// 设置折线颜色
        /// </summary>
        public void SetLineColor(Color color)
        {
            lineColor = color;
        }

        /// <summary>
        /// 设置圆形标记点颜色
        /// </summary>
        public void SetDotColor(Color color)
        {
            dotColor = color;
        }

        /// <summary>
        /// 设置绘制动画时长
        /// </summary>
        public void SetDrawDuration(float duration)
        {
            drawDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        /// 获取当前所有坐标点（返回副本）
        /// </summary>
        public Vector2[] GetPointPositions()
        {
            Vector2[] copy = new Vector2[pointPositions.Length];
            System.Array.Copy(pointPositions, copy, pointPositions.Length);
            return copy;
        }

        /// <summary>
        /// 获取是否绘制完成
        /// </summary>
        public bool IsDrawComplete()
        {
            return _drawComplete;
        }

        /// <summary>
        /// 手动触发重新绘制
        /// </summary>
        public void Redraw()
        {
            StartDrawAnimation();
        }

        /// <summary>
        /// 设置背景颜色
        /// </summary>
        public void SetBackgroundColor(Color color)
        {
            backgroundColor = color;
            if (_background != null)
            {
                _background.color = color;
            }
        }

        // ================================================================
        // 内部类：流光折线渲染器
        // ================================================================
        [RequireComponent(typeof(CanvasRenderer))]
        private sealed class GlowLineRenderer : MaskableGraphic
        {
            private List<Vector2> _points;
            private float _lineWidth;
            private Color _glowColor;
            private float _glowSpeed;
            private float _glowWidth;
            private float _glowOffset = 0f;

            public void Initialize(List<Vector2> points, float lineWidth, Color glowColor, float glowSpeed, float glowWidth)
            {
                _points = new List<Vector2>(points);
                _lineWidth = lineWidth;
                _glowColor = glowColor;
                _glowSpeed = glowSpeed;
                _glowWidth = glowWidth;
                SetVerticesDirty();
            }

            private void Update()
            {
                if (_points == null || _points.Count < 2) return;

                _glowOffset += _glowSpeed * Time.deltaTime;

                float totalLength = 0f;
                for (int i = 0; i < _points.Count - 1; i++)
                {
                    totalLength += Vector2.Distance(_points[i], _points[i + 1]);
                }
                _glowOffset %= totalLength + _glowWidth;

                SetVerticesDirty();
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();

                if (_points == null || _points.Count < 2) return;

                // 基础折线
                for (int segIdx = 0; segIdx < _points.Count - 1; segIdx++)
                {
                    Vector2 start = _points[segIdx];
                    Vector2 end = _points[segIdx + 1];

                    Vector2 dir = (end - start).normalized;
                    Vector2 perp = new Vector2(-dir.y, dir.x) * (_lineWidth / 2f);

                    AddQuad(vh, start - perp, start + perp, end + perp, end - perp, _glowColor);
                }

                // 计算总长度
                float totalLength = 0f;
                for (int i = 0; i < _points.Count - 1; i++)
                {
                    totalLength += Vector2.Distance(_points[i], _points[i + 1]);
                }

                Color brightGlow = new Color(1f, 1f, 1f, _glowColor.a);

                float glowStart = _glowOffset;
                float glowEnd = _glowOffset + _glowWidth;

                float segStartDist = 0f;
                for (int segIdx = 0; segIdx < _points.Count - 1; segIdx++)
                {
                    Vector2 start = _points[segIdx];
                    Vector2 end = _points[segIdx + 1];
                    float segLength = Vector2.Distance(start, end);
                    float segEndDist = segStartDist + segLength;

                    if (glowStart < segEndDist && glowEnd > segStartDist)
                    {
                        float localStart = Mathf.Max(glowStart - segStartDist, 0f);
                        float localEnd = Mathf.Min(glowEnd - segStartDist, segLength);

                        float tStart = localStart / segLength;
                        float tEnd = localEnd / segLength;

                        Vector2 gStart = Vector2.Lerp(start, end, tStart);
                        Vector2 gEnd = Vector2.Lerp(start, end, tEnd);

                        Vector2 dir = (end - start).normalized;
                        Vector2 perp = new Vector2(-dir.y, dir.x) * (_lineWidth / 2f);

                        AddQuad(vh, gStart - perp, gStart + perp, gEnd + perp, gEnd - perp, brightGlow);
                    }

                    // 流光循环回绕
                    if (glowEnd > totalLength)
                    {
                        float wrapEnd = glowEnd - totalLength;
                        float wrapSegStartDist = 0f;
                        for (int wrapIdx = 0; wrapIdx < _points.Count - 1 && wrapSegStartDist < wrapEnd; wrapIdx++)
                        {
                            Vector2 ws = _points[wrapIdx];
                            Vector2 we = _points[wrapIdx + 1];
                            float wsLen = Vector2.Distance(ws, we);
                            float wsEnd = wrapSegStartDist + wsLen;

                            if (wrapEnd > wrapSegStartDist)
                            {
                                float le = Mathf.Min(wrapEnd - wrapSegStartDist, wsLen);

                                float te = le / wsLen;

                                Vector2 gs = ws;
                                Vector2 ge = Vector2.Lerp(ws, we, te);

                                Vector2 wd = (we - ws).normalized;
                                Vector2 wp = new Vector2(-wd.y, wd.x) * (_lineWidth / 2f);

                                AddQuad(vh, gs - wp, gs + wp, ge + wp, ge - wp, brightGlow);
                            }

                            wrapSegStartDist = wsEnd;
                            if (wrapSegStartDist >= wrapEnd) break;
                        }
                    }

                    segStartDist = segEndDist;
                }
            }

            private void AddQuad(VertexHelper vh, Vector2 bl, Vector2 tl, Vector2 tr, Vector2 br, Color color)
            {
                int startIdx = vh.currentVertCount;

                vh.AddVert(bl, color, Vector2.zero);
                vh.AddVert(tl, color, Vector2.zero);
                vh.AddVert(tr, color, Vector2.zero);
                vh.AddVert(br, color, Vector2.zero);

                vh.AddTriangle(startIdx, startIdx + 1, startIdx + 2);
                vh.AddTriangle(startIdx, startIdx + 2, startIdx + 3);
            }
        }
    }
}
