using System;
using System.Collections.Generic;
using UnityEngine;

namespace CSSC_UI.Globe
{
    /// <summary>
    /// 单条船只的实时AIS数据
    /// </summary>
    [Serializable]
    public class AISShipInfo
    {
        public string mmsi;
        public string shipName;
        public string callSign;
        public string shipType;
        public float length;
        public float width;
        public float grossTonnage;
        public double longitude;
        public double latitude;
        public float speed;
        public float course;
        public float heading;
        public string status;
        public float draught;
        public string destination;
        public string eta;
        public string flag;
        public string lastUpdate;
    }

    /// <summary>
    /// AIS数据包（顶层JSON结构）
    /// </summary>
    [Serializable]
    public class AISDataPackage
    {
        public string updateTimestamp;
        public string dataSource;
        public int totalShipCount;
        public List<AISShipInfo> shipList;
    }

    /// <summary>
    /// 航线轨迹点（经纬度 + 时间戳）
    /// </summary>
    [Serializable]
    public class TrackPoint
    {
        public double longitude;
        public double latitude;
        public DateTime timestamp;
        public float speed;
        public float course;

        public TrackPoint(double lon, double lat, DateTime time, float spd = 0, float crs = 0)
        {
            longitude = lon;
            latitude = lat;
            timestamp = time;
            speed = spd;
            course = crs;
        }
    }

    /// <summary>
    /// 船只完整状态（含历史轨迹和当前信息）
    /// </summary>
    [Serializable]
    public class ShipState
    {
        public string mmsi;
        public string shipName;
        public string callSign;
        public string shipType;
        public string status;
        public string destination;
        public string flag;

        /// <summary>历史轨迹点列表（按时间排序）</summary>
        public List<TrackPoint> trackHistory = new List<TrackPoint>();

        /// <summary>当前最新位置</summary>
        public TrackPoint currentPosition;

        /// <summary>航线颜色</summary>
        public Color routeColor = Color.white;
    }
}
