using UnityEngine;
using UnityEngine.UI;

namespace CSSC.UI.Dashboard
{
    /// <summary>
    /// 图片/图标/Logo 的占位组件。
    /// 仅包含一个空 Image，运行时可通过外部数据动态设置 Sprite。
    /// </summary>
    public class DashboardImagePlaceholder : MonoBehaviour
    {
        [Header("占位配置")]
        [Tooltip("占位标识，用于数据匹配")]
        public string imageId = "";

        [Tooltip("默认占位色")]
        public Color placeholderColor = new Color(0.08f, 0.12f, 0.18f, 0.45f);

        private Image _image;
        public Image Image => _image;

        private void Awake()
        {
            EnsureImage();
        }

        private void EnsureImage()
        {
            _image = GetComponent<Image>();
            if (_image == null)
            {
                _image = gameObject.AddComponent<Image>();
            }
            _image.color = placeholderColor;
            _image.raycastTarget = false;
        }

        public void SetSprite(Sprite sprite)
        {
            EnsureImage();
            _image.sprite = sprite;
            _image.color = Color.white;
        }

        public void SetColor(Color color)
        {
            EnsureImage();
            _image.color = color;
        }
    }
}
