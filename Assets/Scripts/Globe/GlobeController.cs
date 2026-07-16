using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CSSC_UI.Globe
{
    /// <summary>
    /// 3D地球控制器：创建地球球体，管理经纬度映射、航线绘制、交互
    /// 
    /// 独立自包含设计，不依赖项目其他脚本。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GlobeController : MonoBehaviour
    {
        #region === Inspector 配置 ===

        [Header("地球设置")]
        [SerializeField] private float globeRadius = 1f;
        [SerializeField] private Material globeMaterial;
        [Tooltip("地球贴图（等距矩形投影 Equirectangular）")]
        [SerializeField] private Texture2D earthTexture;

        [Header("轨道线设置")]
        [SerializeField] private float trackHeightOffset = 0.008f;
        [SerializeField] private float trackLineWidth = 0.004f;
        [SerializeField] private bool showShipMarkers = true;
        [SerializeField] private float shipMarkerSize = 0.015f;

        [Header("交互设置")]
        [SerializeField] private float mouseRotateSpeed = 0.3f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float minZoom = 1.5f;
        [SerializeField] private float maxZoom = 10f;
        [SerializeField] private float focusDistance = 2.5f;
        [Tooltip("滚轮向上滚动多少下回到初始视角")]
        [SerializeField] private int scrollStepsToReset = 5;

        [Header("数据配置")]
        [SerializeField] private string aisDataFilePath = "12345.txt";

        #endregion

        #region === 公开事件 ===

        /// <summary>鼠标悬浮航线变化事件（航线GameObject, 是否悬浮, 船只数据）</summary>
        public event Action<GameObject, bool, ShipState> OnRouteHover;

        /// <summary>点击航线事件</summary>
        public event Action<ShipState> OnRouteClick;

        #endregion

        #region === 内部状态 ===

        private GameObject _globeObject;
        private Transform _globeTransform;
        private Transform _cameraTransform;
        private Camera _mainCamera;

        // 船只数据
        private readonly Dictionary<string, ShipState> _shipStates = new Dictionary<string, ShipState>();
        // 每条船的航线LineRenderer
        private readonly Dictionary<string, LineRenderer> _shipRouteRenderers = new Dictionary<string, LineRenderer>();
        // 每条船的当前标记
        private readonly Dictionary<string, GameObject> _shipRouteObjects = new Dictionary<string, GameObject>();
        // 船只标记（球体上的点）
        private readonly Dictionary<string, GameObject> _shipMarkers = new Dictionary<string, GameObject>();
        // 航线容器
        private Transform _routesContainer;

        // 相机控制
        private Vector3 _cameraOrbitEuler = new Vector3(0f, 0f, 0f);
        private float _cameraDistance = 3.5f;
        private Vector3 _focusTarget;
        private bool _isFocusing = false;
        private ShipState _focusedShip;
        private Vector3 _initialCameraLocalPos;
        private Quaternion _initialCameraLocalRot;
        private int _scrollUpCount = 0;
        private const float SCROLL_RESET_TIMEOUT = 1.5f;
        private float _scrollResetTimer = 0f;

        // 鼠标悬浮
        private GameObject _hoveredRoute;
        private float _routeHoverCheckInterval = 0.05f;
        private float _routeHoverTimer;

        // 颜色缓存
        private static readonly Color[] ShipDefaultColors = new Color[]
        {
            new Color(0.2f, 0.8f, 1.0f),   // 青蓝
            new Color(1.0f, 0.6f, 0.2f),   // 橙
            new Color(0.2f, 1.0f, 0.4f),   // 绿
            new Color(1.0f, 0.3f, 0.6f),   // 粉红
            new Color(0.9f, 0.8f, 0.1f),   // 金
            new Color(0.6f, 0.3f, 1.0f),   // 紫
            new Color(1.0f, 0.4f, 0.4f),   // 红
            new Color(0.3f, 0.9f, 0.8f),   // 青
        };

        private static readonly Color HoverColor = new Color(1f, 1f, 0.3f);  // 亮黄
        private static readonly Color ClickedColor = new Color(1f, 0.5f, 0f); // 橙红

        private bool _initialized = false;
        private bool _isDestroyed = false;

        #endregion

        #region === Unity 生命周期 ===

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("[GlobeController] 场景中没有MainCamera！");
                return;
            }

            _cameraTransform = _mainCamera.transform;

            // 记录初始相机位置
            _initialCameraLocalPos = _cameraTransform.localPosition;
            _initialCameraLocalRot = _cameraTransform.localRotation;

            CreateGlobe();
            CreateRoutesContainer();
            LoadAISData();
            DrawAllRoutes();

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _isDestroyed) return;

            HandleMouseInput();
            HandleScrollInput();
            CheckRouteHover();

            // 滚动重置计时器
            if (_scrollUpCount > 0)
            {
                _scrollResetTimer += Time.deltaTime;
                if (_scrollResetTimer > SCROLL_RESET_TIMEOUT)
                {
                    _scrollUpCount = 0;
                    _scrollResetTimer = 0f;
                }
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            _shipStates.Clear();
            _shipRouteRenderers.Clear();
            _shipMarkers.Clear();
        }

        #endregion

        #region === 地球创建 ===

        private void CreateGlobe()
        {
            // 优先使用已存在的子物体（编辑器搭建场景时创建的）
            _globeObject = transform.Find("Globe")?.gameObject;

            if (_globeObject == null)
            {
                _globeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _globeObject.name = "Globe";
                _globeObject.transform.SetParent(transform);
            }

            _globeObject.transform.localPosition = Vector3.zero;
            _globeObject.transform.localScale = Vector3.one * globeRadius * 2f;
            _globeTransform = _globeObject.transform;

            // 移除默认碰撞体（如果存在），航线自身有 MeshCollider
            SphereCollider sc = _globeObject.GetComponent<SphereCollider>();
            if (sc != null)
            {
                Destroy(sc);
            }

            // 材质
            Renderer renderer = _globeObject.GetComponent<Renderer>();
            if (globeMaterial != null)
            {
                renderer.material = globeMaterial;
            }
            else
            {
                // 创建默认材质
                Shader shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                Material mat = new Material(shader);
                mat.color = new Color(0.15f, 0.25f, 0.45f);
                renderer.material = mat;
            }

            // 贴图
            if (earthTexture != null)
            {
                renderer.material.mainTexture = earthTexture;
            }
        }

        private void CreateRoutesContainer()
        {
            GameObject container = new GameObject("RoutesContainer");
            container.transform.SetParent(_globeTransform);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            _routesContainer = container.transform;
        }

        #endregion

        #region === 数据加载 ===

        private void LoadAISData()
        {
            // 尝试从StreamingAssets加载
            string path = Path.Combine(Application.streamingAssetsPath, aisDataFilePath);
            if (!File.Exists(path))
            {
                // 也尝试从项目外部的Downloads路径
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads", aisDataFilePath);
            }

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[GlobeController] AIS数据文件不存在: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                AISDataPackage data = ParseAISJson(json);

                if (data == null || data.shipList == null || data.shipList.Count == 0)
                {
                    Debug.LogError("[GlobeController] AIS数据解析失败或数据为空");
                    return;
                }

                Debug.Log($"[GlobeController] 解析到 {data.shipList.Count} 艘船只");
                for (int i = 0; i < data.shipList.Count; i++)
                {
                    AISShipInfo info = data.shipList[i];
                    Debug.Log($"[GlobeController]   {i+1}. mmsi={info.mmsi}, name={info.shipName}, lon={info.longitude}, lat={info.latitude}");
                    ShipState state = new ShipState
                    {
                        mmsi = info.mmsi,
                        shipName = info.shipName,
                        callSign = info.callSign,
                        shipType = info.shipType,
                        status = info.status,
                        destination = info.destination,
                        flag = info.flag,
                        routeColor = ShipDefaultColors[i % ShipDefaultColors.Length],
                        currentPosition = new TrackPoint(
                            info.longitude, info.latitude,
                            DateTime.TryParse(info.lastUpdate, out DateTime dt) ? dt : DateTime.Now,
                            info.speed, info.course
                        )
                    };

                    // 当前只有实时位置，作为轨迹的第一个点
                    state.trackHistory.Add(state.currentPosition);

                    _shipStates[state.mmsi] = state;
                }

                Debug.Log($"[GlobeController] 加载了 {_shipStates.Count} 艘船只数据");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GlobeController] 加载AIS数据异常: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 手动解析JSON（绕过JsonUtility对嵌套List的限制）
        /// </summary>
        private AISDataPackage ParseAISJson(string json)
        {
            // 先尝试JsonUtility
            AISDataPackage result = JsonUtility.FromJson<AISDataPackage>(json);
            if (result != null && result.shipList != null && result.shipList.Count > 0)
            {
                return result;
            }

            Debug.Log("[GlobeController] JsonUtility解析失败，尝试手动解析...");

            // 手动解析：提取shipList数组
            result = new AISDataPackage();
            result.shipList = new List<AISShipInfo>();

            // 找到shipList数组
            int startIdx = json.IndexOf("\"shipList\"");
            if (startIdx < 0)
            {
                Debug.LogError("[GlobeController] JSON中找不到shipList字段");
                return result;
            }

            int arrayStart = json.IndexOf('[', startIdx);
            if (arrayStart < 0)
            {
                Debug.LogError("[GlobeController] shipList后找不到数组起始");
                return result;
            }

            // 找到匹配的右括号
            int depth = 0;
            int arrayEnd = -1;
            for (int i = arrayStart; i < json.Length; i++)
            {
                if (json[i] == '[') depth++;
                else if (json[i] == ']') { depth--; if (depth == 0) { arrayEnd = i; break; } }
            }

            if (arrayEnd < 0)
            {
                Debug.LogError("[GlobeController] shipList数组未正确闭合");
                return result;
            }

            // 逐个解析数组中的对象
            int objStart = arrayStart + 1;
            while (objStart < arrayEnd)
            {
                // 找到 {
                int braceStart = json.IndexOf('{', objStart, arrayEnd - objStart);
                if (braceStart < 0) break;

                // 找到匹配的 }
                depth = 0;
                int braceEnd = -1;
                for (int i = braceStart; i <= arrayEnd; i++)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}') { depth--; if (depth == 0) { braceEnd = i; break; } }
                }

                if (braceEnd < 0) break;

                string shipJson = json.Substring(braceStart, braceEnd - braceStart + 1);
                AISShipInfo info = JsonUtility.FromJson<AISShipInfo>(shipJson);
                if (info != null)
                {
                    result.shipList.Add(info);
                }

                objStart = braceEnd + 1;
            }

            return result;
        }

        #endregion

        #region === 航线绘制 ===

        private void DrawAllRoutes()
        {
            foreach (var kvp in _shipStates)
            {
                DrawRoute(kvp.Value);
            }
            Debug.Log($"[GlobeController] 航线绘制完成，共 {_shipRouteRenderers.Count} 条航线, {_shipMarkers.Count} 个标记");
        }

        private void DrawRoute(ShipState ship)
        {
            if (ship.trackHistory.Count < 1) return;

            // 创建航线GameObject
            GameObject routeObj = new GameObject($"Route_{ship.mmsi}");
            routeObj.transform.SetParent(_routesContainer);
            routeObj.transform.localPosition = Vector3.zero;
            routeObj.transform.localRotation = Quaternion.identity;

            float radius = globeRadius * 2f; // 球体实际缩放后的半径

            // 如果只有1个点，用Sphere标记代替LineRenderer
            if (ship.trackHistory.Count == 1)
            {
                // 创建一个小球作为标记点
                GameObject pointMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointMarker.name = $"RoutePoint_{ship.mmsi}";
                pointMarker.transform.SetParent(routeObj.transform);
                Vector3 pos = GeoCoordConverter.GeoToSphere(
                    ship.trackHistory[0].longitude,
                    ship.trackHistory[0].latitude,
                    radius * 0.5f,
                    trackHeightOffset
                );
                pointMarker.transform.localPosition = pos;
                pointMarker.transform.localScale = Vector3.one * 0.03f;

                Renderer r = pointMarker.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = ship.routeColor;
                mat.SetFloat("_Glossiness", 0.3f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", ship.routeColor * 0.8f);
                }
                r.material = mat;

                Destroy(pointMarker.GetComponent<SphereCollider>());

                // 给整个routeObj加碰撞体（大一点的球）用于鼠标检测
                SphereCollider sc = routeObj.AddComponent<SphereCollider>();
                sc.center = pos;
                sc.radius = 0.05f;
            }
            else
            {
                // 多个点，用LineRenderer绘制
                LineRenderer lr = routeObj.AddComponent<LineRenderer>();
                lr.positionCount = ship.trackHistory.Count;
                lr.startWidth = trackLineWidth;
                lr.endWidth = trackLineWidth;
                lr.useWorldSpace = false;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = ship.routeColor;
                lr.endColor = ship.routeColor;
                lr.sortingOrder = 1;
                lr.textureMode = LineTextureMode.Tile;
                lr.numCapVertices = 4;
                lr.numCornerVertices = 4;

                for (int i = 0; i < ship.trackHistory.Count; i++)
                {
                    Vector3 pos = GeoCoordConverter.GeoToSphere(
                        ship.trackHistory[i].longitude,
                        ship.trackHistory[i].latitude,
                        radius * 0.5f,
                        trackHeightOffset
                    );
                    lr.SetPosition(i, pos);
                }

                // 添加碰撞组件用于鼠标检测
                MeshCollider routeCollider = routeObj.AddComponent<MeshCollider>();
                Mesh colliderMesh = CreateLineMesh(lr);
                if (colliderMesh != null)
                {
                    routeCollider.sharedMesh = colliderMesh;
                    routeCollider.convex = false;
                }
            }

            // 存储关联的ShipState
            RouteMetadata metadata = routeObj.AddComponent<RouteMetadata>();
            metadata.ShipState = ship;

            _shipRouteRenderers[ship.mmsi] = routeObj.GetComponent<LineRenderer>();

            // 创建船只当前位置标记（所有情况都创建，作为明显标记）
            if (showShipMarkers && ship.currentPosition != null)
            {
                CreateShipMarker(ship);
            }
        }

        /// <summary>
        /// 从LineRenderer创建碰撞检测用的细条Mesh
        /// </summary>
        private Mesh CreateLineMesh(LineRenderer lr)
        {
            if (lr.positionCount < 2) return null;

            int count = lr.positionCount;
            Vector3[] vertices = new Vector3[count * 2];
            int[] triangles = new int[(count - 1) * 6];
            Vector2[] uvs = new Vector2[count * 2];

            float halfWidth = trackLineWidth * 3f; // 碰撞检测范围略宽于可见线

            for (int i = 0; i < count; i++)
            {
                Vector3 point = lr.GetPosition(i);
                Vector3 dirFromCenter = point.normalized;
                Vector3 tangent;

                if (i < count - 1)
                {
                    Vector3 nextPoint = lr.GetPosition(i + 1);
                    tangent = Vector3.Cross(dirFromCenter, (nextPoint - point).normalized).normalized;
                }
                else if (i > 0)
                {
                    Vector3 prevPoint = lr.GetPosition(i - 1);
                    tangent = Vector3.Cross(dirFromCenter, (point - prevPoint).normalized).normalized;
                }
                else
                {
                    tangent = Vector3.Cross(dirFromCenter, Vector3.up).normalized;
                    if (tangent.sqrMagnitude < 0.001f)
                        tangent = Vector3.Cross(dirFromCenter, Vector3.forward).normalized;
                }

                vertices[i * 2] = point + tangent * halfWidth;
                vertices[i * 2 + 1] = point - tangent * halfWidth;
                uvs[i * 2] = new Vector2(0, (float)i / count);
                uvs[i * 2 + 1] = new Vector2(1, (float)i / count);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int ti = i * 6;
                triangles[ti] = i * 2;
                triangles[ti + 1] = i * 2 + 1;
                triangles[ti + 2] = i * 2 + 2;
                triangles[ti + 3] = i * 2 + 1;
                triangles[ti + 4] = i * 2 + 3;
                triangles[ti + 5] = i * 2 + 2;
            }

            Mesh mesh = new Mesh();
            mesh.name = "RouteCollider";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateShipMarker(ShipState ship)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Marker_{ship.mmsi}";
            marker.transform.SetParent(_routesContainer);

            float radius = globeRadius * 2f;
            Vector3 pos = GeoCoordConverter.GeoToSphere(
                ship.currentPosition.longitude,
                ship.currentPosition.latitude,
                radius * 0.5f,
                trackHeightOffset * 1.5f
            );
            marker.transform.localPosition = pos;
            marker.transform.localScale = Vector3.one * shipMarkerSize;

            Renderer renderer = marker.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = ship.routeColor;
            mat.SetFloat("_Glossiness", 0f);
            // 尝试自发光
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", ship.routeColor * 0.5f);
            }
            renderer.material = mat;

            Destroy(marker.GetComponent<Collider>());

            _shipMarkers[ship.mmsi] = marker;
        }

        #endregion

        #region === 公开API：实时更新船只位置 ===

        /// <summary>
        /// 通过长连接推送数据时调用，更新船只位置并追加轨迹点
        /// </summary>
        public void UpdateShipPosition(string mmsi, double longitude, double latitude, float speed, float course, DateTime timestamp)
        {
            if (!_shipStates.TryGetValue(mmsi, out ShipState ship)) return;

            TrackPoint newPoint = new TrackPoint(longitude, latitude, timestamp, speed, course);
            ship.trackHistory.Add(newPoint);
            ship.currentPosition = newPoint;

            // 更新LineRenderer
            if (_shipRouteRenderers.TryGetValue(mmsi, out LineRenderer lr))
            {
                lr.positionCount = ship.trackHistory.Count;
                float radius = globeRadius * 2f;
                for (int i = 0; i < ship.trackHistory.Count; i++)
                {
                    Vector3 pos = GeoCoordConverter.GeoToSphere(
                        ship.trackHistory[i].longitude,
                        ship.trackHistory[i].latitude,
                        radius * 0.5f,
                        trackHeightOffset
                    );
                    lr.SetPosition(i, pos);
                }

                // 更新碰撞网格
                MeshCollider mc = lr.GetComponent<MeshCollider>();
                if (mc != null)
                {
                    Destroy(mc.sharedMesh);
                    Mesh newMesh = CreateLineMesh(lr);
                    mc.sharedMesh = newMesh;
                }
            }

            // 更新标记位置
            if (_shipMarkers.TryGetValue(mmsi, out GameObject marker))
            {
                float radius = globeRadius * 2f;
                Vector3 pos = GeoCoordConverter.GeoToSphere(longitude, latitude, radius * 0.5f, trackHeightOffset * 1.5f);
                marker.transform.localPosition = pos;
            }
        }

        /// <summary>
        /// 批量更新船只数据（完整数据包）
        /// </summary>
        public void UpdateShipDataBatch(string jsonData)
        {
            try
            {
                AISDataPackage data = JsonUtility.FromJson<AISDataPackage>(jsonData);
                if (data?.shipList == null) return;

                foreach (var info in data.shipList)
                {
                    DateTime time = DateTime.TryParse(info.lastUpdate, out DateTime dt) ? dt : DateTime.Now;
                    UpdateShipPosition(info.mmsi, info.longitude, info.latitude, info.speed, info.course, time);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GlobeController] 批量更新失败: {e.Message}");
            }
        }

        #endregion

        #region === 鼠标交互 ===

        private void HandleMouseInput()
        {
            // 左键拖拽旋转
            if (Input.GetMouseButton(0))
            {
                if (_isFocusing) return;

                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
                {
                    // 先检查是否在航线上（不旋转，而是点击航线）
                    if (!CheckClickOnRoute())
                    {
                        _cameraOrbitEuler.y += mouseX * mouseRotateSpeed * 100f;
                        _cameraOrbitEuler.x -= mouseY * mouseRotateSpeed * 100f;
                        _cameraOrbitEuler.x = Mathf.Clamp(_cameraOrbitEuler.x, -89f, 89f);
                        UpdateCameraPosition();
                    }
                }
            }

            // 左键释放（处理点击）
            if (Input.GetMouseButtonUp(0))
            {
                if (Input.GetAxis("Mouse X") == 0 && Input.GetAxis("Mouse Y") == 0)
                {
                    CheckClickOnRoute();
                }
            }
        }

        private bool CheckClickOnRoute()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                RouteMetadata meta = hit.collider.GetComponent<RouteMetadata>();
                if (meta != null && meta.ShipState != null)
                {
                    FocusOnShip(meta.ShipState);
                    OnRouteClick?.Invoke(meta.ShipState);
                    return true;
                }
            }
            return false;
        }

        private void CheckRouteHover()
        {
            _routeHoverTimer += Time.deltaTime;
            if (_routeHoverTimer < _routeHoverCheckInterval) return;
            _routeHoverTimer = 0f;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            GameObject hitRoute = null;
            ShipState hitShip = null;

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                RouteMetadata meta = hit.collider.GetComponent<RouteMetadata>();
                if (meta != null && meta.ShipState != null)
                {
                    hitRoute = hit.collider.gameObject;
                    hitShip = meta.ShipState;
                }
            }

            // 悬浮变化
            if (hitRoute != _hoveredRoute)
            {
                // 还原旧悬浮：如果之前有悬浮，且当前未处于聚焦选中状态，或者聚焦的航线不是之前悬浮的航线
                if (_hoveredRoute != null)
                {
                    if (_focusedShip == null || _shipRouteRenderers[_focusedShip.mmsi].gameObject != _hoveredRoute)
                    {
                        ResetRouteColor(_hoveredRoute);
                    }
                }

                // 设置新悬浮
                if (hitRoute != null)
                {
                    SetRouteColor(hitRoute, HoverColor);
                }

                _hoveredRoute = hitRoute;

                // 触发事件
                RouteMetadata oldMeta = _hoveredRoute?.GetComponent<RouteMetadata>();
                OnRouteHover?.Invoke(hitRoute, hitRoute != null, hitShip);
            }
        }

        private void SetRouteColor(GameObject routeObj, Color color)
        {
            if (routeObj == null) return;
            LineRenderer lr = routeObj.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.startColor = color;
                lr.endColor = color;
            }
        }

        private void ResetRouteColor(GameObject routeObj)
        {
            if (routeObj == null) return;
            RouteMetadata meta = routeObj.GetComponent<RouteMetadata>();
            if (meta?.ShipState != null)
            {
                SetRouteColor(routeObj, meta.ShipState.routeColor);
            }
        }

        #endregion

        #region === 滚轮缩放 & 重置视角 ===

        private void HandleScrollInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            if (_isFocusing)
            {
                // 聚焦模式下，向上滚动累计计数
                if (scroll > 0)
                {
                    _scrollUpCount++;
                    _scrollResetTimer = 0f;

                    if (_scrollUpCount >= scrollStepsToReset)
                    {
                        ResetCameraView();
                        return;
                    }
                }
                else
                {
                    // 向下滚动可以继续缩放
                    _cameraDistance += scroll * zoomSpeed;
                    _cameraDistance = Mathf.Clamp(_cameraDistance, minZoom, maxZoom);
                    UpdateCameraPosition();
                }
            }
            else
            {
                // 非聚焦模式，正常缩放
                _cameraDistance -= scroll * zoomSpeed;
                _cameraDistance = Mathf.Clamp(_cameraDistance, minZoom, maxZoom);
                UpdateCameraPosition();
            }
        }

        private void FocusOnShip(ShipState ship)
        {
            _focusedShip = ship;
            _isFocusing = true;
            _scrollUpCount = 0;
            _scrollResetTimer = 0f;

            // 计算目标位置：船只位置朝向相机方向偏移
            float radius = globeRadius * 2f;
            Vector3 shipPos = GeoCoordConverter.GeoToSphere(
                ship.currentPosition.longitude,
                ship.currentPosition.latitude,
                radius * 0.5f,
                trackHeightOffset * 1.5f
            );
            Vector3 worldShipPos = _globeTransform.TransformPoint(shipPos);

            // 设置聚焦目标
            _focusTarget = worldShipPos;

            // 调整相机朝向该点
            Vector3 dirToShip = (worldShipPos - _globeTransform.position).normalized;
            _cameraOrbitEuler.x = -Mathf.Asin(dirToShip.y) * Mathf.Rad2Deg;
            _cameraOrbitEuler.y = Mathf.Atan2(dirToShip.x, dirToShip.z) * Mathf.Rad2Deg;
            _cameraDistance = focusDistance;

            UpdateCameraPosition();

            // 高亮选中航线
            if (_shipRouteRenderers.TryGetValue(ship.mmsi, out LineRenderer lr))
            {
                SetRouteColor(lr.gameObject, ClickedColor);
            }
        }

        private void ResetCameraView()
        {
            _isFocusing = false;
            _focusedShip = null;
            _scrollUpCount = 0;
            _scrollResetTimer = 0f;

            _cameraOrbitEuler = Vector3.zero;
            _cameraDistance = Vector3.Distance(_initialCameraLocalPos, Vector3.zero);

            UpdateCameraPosition();

            // 还原所有航线颜色
            foreach (var kvp in _shipRouteRenderers)
            {
                if (_shipStates.TryGetValue(kvp.Key, out ShipState ship))
                {
                    SetRouteColor(kvp.Value.gameObject, ship.routeColor);
                }
            }

            // 还原悬浮状态
            _hoveredRoute = null;
        }

        private void UpdateCameraPosition()
        {
            if (_cameraTransform == null) return;

            Quaternion rotation = Quaternion.Euler(_cameraOrbitEuler.x, _cameraOrbitEuler.y, 0);
            Vector3 negDistance = new Vector3(0f, 0f, -_cameraDistance);
            Vector3 position = rotation * negDistance + _globeTransform.position;

            _cameraTransform.position = position;
            _cameraTransform.LookAt(_globeTransform.position);
        }

        #endregion
    }

    /// <summary>
    /// 附加到航线GameObject上的元数据组件
    /// </summary>
    public class RouteMetadata : MonoBehaviour
    {
        public ShipState ShipState;
    }
}
