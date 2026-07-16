using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CSSC_UI.Globe;

namespace CSSC_UI.Globe.Editor
{
    /// <summary>
    /// 一键搭建3D地球场景的编辑器工具
    /// 菜单：Tools > CSSC > Setup Globe Scene
    /// </summary>
    public static class GlobeSceneSetup
    {
        private const string SCENE_NAME = "GlobeScene";
        private const string SCENE_PATH = "Assets/Scenes/GlobeScene.unity";

        [MenuItem("Tools/CSSC/Setup Globe Scene", false, 100)]
        public static void SetupScene()
        {
            // 创建新场景
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- 1. 创建相机 ----
            GameObject cameraObj = new GameObject("MainCamera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.08f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cameraObj.transform.position = new Vector3(0, 0, -5);
            cameraObj.transform.LookAt(Vector3.zero);
            cameraObj.tag = "MainCamera";

            // ---- 2. 创建Canvas（用于弹窗UI） ----
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // ---- 3. 创建弹窗面板 ----
            GameObject popupPanel = CreatePopupPanel(canvasObj.transform);

            // ---- 4. 创建地球GameObject ----
            GameObject globeObj = new GameObject("GlobeRoot");
            globeObj.transform.position = Vector3.zero;
            GlobeController controller = globeObj.AddComponent<GlobeController>();

            // 设置序列化字段
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("globeRadius").floatValue = 1f;
            so.FindProperty("trackHeightOffset").floatValue = 0.008f;
            so.FindProperty("trackLineWidth").floatValue = 0.004f;
            so.FindProperty("mouseRotateSpeed").floatValue = 0.3f;
            so.FindProperty("zoomSpeed").floatValue = 0.5f;
            so.FindProperty("minZoom").floatValue = 1.5f;
            so.FindProperty("maxZoom").floatValue = 10f;
            so.FindProperty("focusDistance").floatValue = 2.5f;
            so.FindProperty("scrollStepsToReset").intValue = 5;
            so.ApplyModifiedProperties();

            // ---- 5. 挂载弹窗脚本 ----
            ShipInfoPopup popup = canvasObj.AddComponent<ShipInfoPopup>();
            so = new SerializedObject(popup);
            so.FindProperty("popupPanel").objectReferenceValue = popupPanel;
            so.FindProperty("shipNameText").objectReferenceValue = popupPanel.transform.Find("ShipName").GetComponent<Text>();
            so.FindProperty("mmsiText").objectReferenceValue = popupPanel.transform.Find("MMSI").GetComponent<Text>();
            so.FindProperty("startPointText").objectReferenceValue = popupPanel.transform.Find("StartPoint").GetComponent<Text>();
            so.FindProperty("endPointText").objectReferenceValue = popupPanel.transform.Find("EndPoint").GetComponent<Text>();
            so.FindProperty("popupOffset").vector2Value = new Vector2(15f, -15f);
            so.ApplyModifiedProperties();

            // ---- 6. 复制AIS数据文件到StreamingAssets ----
            EnsureStreamingAssetsData();

            // ---- 7. 保存场景 ----
            EnsureFolderExists("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[GlobeSceneSetup] 场景已创建并保存到 {SCENE_PATH}");
            Debug.Log("[GlobeSceneSetup] 请按 Play 运行，查看效果。");
            Debug.Log("[GlobeSceneSetup] 交互说明：");
            Debug.Log("  - 左键拖拽：旋转地球");
            Debug.Log("  - 滚轮：缩放");
            Debug.Log("  - 鼠标悬浮航线：显示弹窗（船名/船号/起始点/结束点）");
            Debug.Log("  - 点击航线：镜头聚焦船只位置");
            Debug.Log("  - 聚焦后向上滚轮5次：回到初始视角");
        }

        private static GameObject CreatePopupPanel(Transform parent)
        {
            GameObject panel = new GameObject("ShipInfoPopup");
            panel.transform.SetParent(parent, false);

            // 背景
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.15f, 0.92f);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(320, 160);
            rect.anchoredPosition = Vector2.zero;

            // 添加边框效果（用Outline组件）
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.6f, 1f, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            // 垂直布局
            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10);
            vlg.spacing = 6;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = panel.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 标题
            GameObject titleObj = CreateTextObject("Title", panel.transform, "船只信息", 18, FontStyle.Bold,
                new Color(0.3f, 0.7f, 1f), TextAnchor.MiddleLeft);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(296, 28);

            // 分隔线
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(panel.transform, false);
            Image divImg = divider.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.6f, 1f, 0.4f);
            RectTransform divRect = divider.GetComponent<RectTransform>();
            divRect.sizeDelta = new Vector2(296, 1);

            // 船名
            CreateTextObject("ShipName", panel.transform, "船名：--", 14, FontStyle.Normal, Color.white);

            // 船号
            CreateTextObject("MMSI", panel.transform, "船号(MMSI)：--", 14, FontStyle.Normal, new Color(0.8f, 0.8f, 0.85f));

            // 起始点
            CreateTextObject("StartPoint", panel.transform, "起始点：--", 14, FontStyle.Normal, new Color(0.6f, 0.9f, 0.6f));

            // 结束点
            CreateTextObject("EndPoint", panel.transform, "结束点：--", 14, FontStyle.Normal, new Color(0.9f, 0.6f, 0.4f));

            return panel;
        }

        private static GameObject CreateTextObject(string name, Transform parent, string text,
            int fontSize, FontStyle style, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;

            // 使用默认字体
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(296, fontSize + 6);

            return obj;
        }

        private static void EnsureStreamingAssetsData()
        {
            string targetDir = Application.dataPath + "/StreamingAssets";
            if (!System.IO.Directory.Exists(targetDir))
            {
                System.IO.Directory.CreateDirectory(targetDir);
            }

            // 检查12345.txt是否已存在
            string targetPath = targetDir + "/12345.txt";
            if (!System.IO.File.Exists(targetPath))
            {
                // 尝试从Downloads复制
                string sourcePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile)
                    + "/Downloads/12345.txt";
                if (System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Copy(sourcePath, targetPath, true);
                    Debug.Log($"[GlobeSceneSetup] 已复制AIS数据到 {targetPath}");
                }
                else
                {
                    Debug.LogWarning($"[GlobeSceneSetup] AIS数据文件未找到: {sourcePath}");
                    Debug.LogWarning("请手动将 12345.txt 复制到 Assets/StreamingAssets/ 目录");
                }
            }
            else
            {
                Debug.Log($"[GlobeSceneSetup] AIS数据文件已存在: {targetPath}");
            }

            AssetDatabase.Refresh();
        }

        private static void EnsureFolderExists(string folderPath)
        {
            string fullPath = Application.dataPath + "/" + folderPath.Replace("Assets/", "");
            if (!System.IO.Directory.Exists(fullPath))
            {
                System.IO.Directory.CreateDirectory(fullPath);
            }
        }
    }
}
