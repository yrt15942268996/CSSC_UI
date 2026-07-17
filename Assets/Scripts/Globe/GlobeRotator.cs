using UnityEngine;

namespace CSSC_UI.Globe
{
    /// <summary>
    /// 简单的自转脚本：地球 / 云层都可以挂，各自独立旋转。
    /// </summary>
    public class GlobeRotator : MonoBehaviour
    {
        [Tooltip("旋转速度（度/秒）")]
        [SerializeField] private float rotationSpeed = 5f;

        [Tooltip("旋转轴（本地坐标）")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        private void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
