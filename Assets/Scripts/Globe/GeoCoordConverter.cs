using UnityEngine;

namespace CSSC_UI.Globe
{
    /// <summary>
    /// 地理坐标（经纬度）与3D球面坐标之间的转换工具
    /// 
    /// 坐标系约定：
    /// - 经度 longitude: -180~180（东经为正），对应绕Y轴旋转
    /// - 纬度 latitude: -90~90（北纬为正），对应从赤道平面向上/向下的角度
    /// - 球体中心在原点，Y轴向上指向北极，X轴指向本初子午线（经度0）
    /// - 球体半径由外部指定
    /// </summary>
    public static class GeoCoordConverter
    {
        /// <summary>
        /// 将经纬度转换为球面上的3D位置
        /// </summary>
        /// <param name="longitude">经度（度）</param>
        /// <param name="latitude">纬度（度）</param>
        /// <param name="radius">球体半径</param>
        /// <param name="heightOffset">高于球面的偏移（用于让标记浮在球面上方）</param>
        /// <returns>球面上的世界坐标</returns>
        public static Vector3 GeoToSphere(double longitude, double latitude, float radius, float heightOffset = 0.005f)
        {
            // 纬度转为弧度：北极=+90度 → phi=0（Y轴顶端），赤道=0度 → phi=PI/2，南极=-90度 → phi=PI
            double phi = (90.0 - latitude) * Mathf.Deg2Rad;

            // 经度转为弧度：绕Y轴旋转（东经为正）
            double theta = longitude * Mathf.Deg2Rad;

            float r = radius + heightOffset;

            float x = r * Mathf.Sin((float)phi) * Mathf.Cos((float)theta);
            float y = r * Mathf.Cos((float)phi);
            float z = r * Mathf.Sin((float)phi) * Mathf.Sin((float)theta);

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 将球面上的3D位置反算为经纬度
        /// </summary>
        public static void SphereToGeo(Vector3 pos, out double longitude, out double latitude)
        {
            // 归一化方向向量
            Vector3 dir = pos.normalized;

            // 纬度：Y分量 → 纬度角
            latitude = 90.0 - Mathf.Acos(dir.y) * Mathf.Rad2Deg;

            // 经度：XZ平面角度
            longitude = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 计算两个经纬度之间的大圆距离（公里）
        /// </summary>
        public static double GreatCircleDistance(double lon1, double lat1, double lon2, double lat2)
        {
            double R = 6371.0; // 地球半径（公里）
            double dLat = (lat2 - lat1) * Mathf.Deg2Rad;
            double dLon = (lon2 - lon1) * Mathf.Deg2Rad;
            double a = Mathf.Sin((float)(dLat / 2)) * Mathf.Sin((float)(dLat / 2)) +
                       Mathf.Cos((float)(lat1 * Mathf.Deg2Rad)) * Mathf.Cos((float)(lat2 * Mathf.Deg2Rad)) *
                       Mathf.Sin((float)(dLon / 2)) * Mathf.Sin((float)(dLon / 2));
            double c = 2 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1 - a)));
            return R * c;
        }
    }
}
