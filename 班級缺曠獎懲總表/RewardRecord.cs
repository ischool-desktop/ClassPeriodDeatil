using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassPeriodDetail
{
    class RewardRecord
    {
        /// <summary>
        /// 留查
        /// </summary>
        public bool Flag { get; set; }
        /// <summary>
        /// 全勤
        /// </summary>
        public bool 全勤 = true;
        /// <summary>
        /// 大功
        /// </summary>
        public int MeritACount { get; set; }
        /// <summary>
        /// 小功
        /// </summary>
        public int MeritBCount { get; set; }
        /// <summary>
        /// 嘉獎
        /// </summary>
        public int MeritCCount { get; set; }

        /// <summary>
        /// 大過
        /// </summary>
        public int DemeritACount { get; set; }
        /// <summary>
        /// 小過
        /// </summary>
        public int DemeritBCount { get; set; }
        /// <summary>
        /// 警告
        /// </summary>
        public int DemeritCCount { get; set; }
        
        /// <summary>
        /// 缺曠資料
        /// </summary>
        public Dictionary<string, int> Attendance = new Dictionary<string, int>();
    }
}
