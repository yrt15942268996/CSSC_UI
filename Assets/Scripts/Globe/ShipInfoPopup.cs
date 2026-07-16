using UnityEngine;
using UnityEngine.UI;

namespace CSSC_UI.Globe
{
    /// <summary>
    /// 船只信息弹窗：鼠标悬浮航线时显示
    /// 显示：船名、船号(MMSI)、起始点、结束点
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShipInfoPopup : MonoBehaviour
    {
        #region === Inspector 配置 ===

        [Header("弹窗UI引用")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private Text shipNameText;
        [SerializeField] private Text mmsiText;
        [SerializeField] private Text startPointText;
        [SerializeField] private Text endPointText;

        [Header("弹窗设置")]
        [SerializeField] private Vector2 popupOffset = new Vector2(15f, -15f);
        [SerializeField] private float popupSmoothSpeed = 8f;

        #endregion

        #region === 内部状态 ===

        private GlobeController _globeController;
        private RectTransform _popupRect;
        private Canvas _parentCanvas;
        private Camera _uiCamera;
        private bool _isVisible = false;
        private Vector2 _targetScreenPos;
        private bool _isDestroyed = false;

        #endregion

        #region === Unity 生命周期 ===

        private void Start()
        {
            _globeController = FindFirstObjectByType<GlobeController>();
            if (_globeController != null)
            {
                _globeController.OnRouteHover += HandleRouteHover;
            }

            if (popupPanel != null)
            {
                _popupRect = popupPanel.GetComponent<RectTransform>();
                popupPanel.SetActive(false);
            }

            _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas != null && _parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                _uiCamera = _parentCanvas.worldCamera;
            }
        }

        private void Update()
        {
            if (!_isVisible || popupPanel == null || _popupRect == null) return;

            // 平滑移动弹窗到目标位置
            Vector2 currentPos = _popupRect.anchoredPosition;
            _popupRect.anchoredPosition = Vector2.Lerp(currentPos, _targetScreenPos, Time.deltaTime * popupSmoothSpeed);
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            if (_globeController != null)
            {
                _globeController.OnRouteHover -= HandleRouteHover;
            }
        }

        #endregion

        #region === 事件处理 ===

        private void HandleRouteHover(GameObject routeObj, bool isHovering, ShipState ship)
        {
            if (_isDestroyed) return;

            if (isHovering && ship != null)
            {
                ShowPopup(ship);
            }
            else
            {
                HidePopup();
            }
        }

        #endregion

        #region === 弹窗显示/隐藏 ===

        private void ShowPopup(ShipState ship)
        {
            if (popupPanel == null) return;

            // 更新文本
            if (shipNameText != null)
                shipNameText.text = $"船名：{ship.shipName}";

            if (mmsiText != null)
                mmsiText.text = $"船号(MMSI)：{ship.mmsi}";

            if (startPointText != null)
            {
                if (ship.trackHistory.Count > 0)
                {
                    var first = ship.trackHistory[0];
                    startPointText.text = $"起始点：({first.latitude:F4}°N, {first.longitude:F4}°E)";
                }
                else
                {
                    startPointText.text = "起始点：--";
                }
            }

            if (endPointText != null)
            {
                if (ship.currentPosition != null)
                {
                    endPointText.text = $"结束点：({ship.currentPosition.latitude:F4}°N, {ship.currentPosition.longitude:F4}°E)";
                }
                else
                {
                    endPointText.text = "结束点：--";
                }
            }

            // 定位弹窗到鼠标位置
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _popupRect.parent as RectTransform,
                Input.mousePosition,
                _uiCamera,
                out Vector2 localPoint
            );

            _targetScreenPos = localPoint + popupOffset;
            _popupRect.anchoredPosition = _targetScreenPos;

            popupPanel.SetActive(true);
            _isVisible = true;
        }

        private void HidePopup()
        {
            _isVisible = false;
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
        }

        #endregion
    }
}
