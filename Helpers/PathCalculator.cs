using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Model.cut;
using static Emgu.CV.Dai.OpenVino;

namespace 精密切割系统.Helpers
{
    public class PathRecord
    {
        public float Speed { get; }       // 行走速度(mm/s)
        public float? PathLength { get; set; }  // 实际路长(mm)，通过后才知道
        public float? ActualTime { get; set; }  // 实际耗时(s)，通过后才知道

        public PathRecord(float speed)
        {
            Speed = speed;
        }
    }

    public class PathCalculator
    {
        private List<PathRecord> _records;

        public PathCalculator(List<PathRecord> pathRecords)
        {
            _records = pathRecords;
        }

        public PathCalculator(List<float> speeds)
        {
            List<PathRecord> records = [];
            foreach (float speed in speeds)
            {
                records.Add(new PathRecord(speed));
            }
            _records = records;
        }

        //// 记录通过信息
        //public void ReportPass(int index, float pathLength, float actualTime)
        //{
        //    if (index >= _records.Count)
        //    {
        //        return;
        //    }
        //    var record = _records[index];
        //    record.PathLength = pathLength;
        //    record.ActualTime = actualTime;
        //}

        // 记录通过信息
        public void ReportPass(int index, float pathLength, float actualTime)
        {
            if (index >= _records.Count)
            {
                return;
            }
            var record = _records[index];
            record.PathLength = pathLength;
            record.ActualTime = actualTime;
        }

        // 估算剩余时间
        public float EstimateRemainingTime()
        {
            var remaining = _records.Where(p => !p.ActualTime.HasValue).ToList();
            if (!remaining.Any()) return 0;

            // 1. 计算平均速度/路长比
            var passed = _records.Where(p => p.ActualTime.HasValue).ToList();
            float avgRatio = passed.Any()
                ? passed.Average(p => p.Speed / p.PathLength!.Value)
                : 1f; // 默认值

            // 2. 估算平均延迟
            float avgDelay = passed.Any()
                ? passed.Average(p => p.ActualTime!.Value - (p.PathLength!.Value / p.Speed))
                : 0f;

            // 3. 预测剩余时间
            return remaining.Sum(p =>
            {
                float estPathLength = p.Speed / avgRatio; // 预估路长
                return estPathLength / p.Speed + avgDelay;
            });
        }
    }
}