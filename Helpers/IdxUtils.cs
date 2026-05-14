using DryIoc.ImTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    public class IdxUtils
    {
        public static float? StepDistanceX { get; set; }

        public static float? StepDistanceY { get; set; }

        public static IdxChData[] ChThetaDeg { get; set; } = [];

        private static int _degIndex = 0;

        public static async Task UpdateStepDistanceAsync(bool isForcedUpdate = false)
        {
            if (isForcedUpdate || StepDistanceX is null || StepDistanceY is null)
            {
                var stepResult = await AutoCutUtils.GetCurrentStepDistance();
                if (stepResult.IsSuccess)
                {
                    StepDistanceX = stepResult.Data.StepDistanceX;
                    StepDistanceY = stepResult.Data.StepDistanceY;
                }
            }
        }

        public static async Task UpdateChThetaDegAsync(int? chNum = null)
        {
            var degResult = await AutoCutUtils.GetCurrentChThetaDegAsync();
            if (degResult.IsSuccess)
            {
                if (degResult.Data is not null)
                {
                    // 排序 + 去重（按 Ch 升序，保留第一个）
                    var result = degResult.Data
                        .OrderBy(x => x.Ch)              // 按 Ch 升序排序
                        .GroupBy(x => x.Ch)              // 按 Ch 分组
                        .Select(g => g.First())          // 每组取第一个
                        .ToArray();
                    ChThetaDeg = result;
                }
                else
                {
                    ChThetaDeg = [];
                }
            }
            else
            {
                ChThetaDeg = [];
            }
            if (chNum is null)
            {
                _degIndex = 0;// 重置索引
            }
            else
            {
                // 查找对应chNum的索引
                int index = Array.FindIndex(ChThetaDeg, chData => chData.Ch == chNum);
                if (index != -1)
                {
                    _degIndex = index; // 设置为找到的索引
                }
                else
                {
                    _degIndex = 0; // 未找到，重置索引
                }
            }
            await CurrentUtils.UpdateCurrentChAsync(string.Format(GlobalParams.StringFormatCH, ChThetaDeg[_degIndex].Ch));
        }

        public static async Task<float?> ToNextChThetaDegAsync()
        {
            if (ChThetaDeg == null || ChThetaDeg.Length == 0)
                return null;

            _degIndex++;
            if (_degIndex >= ChThetaDeg.Length)
            {
                _degIndex = ChThetaDeg.Length - 1;
                return null;
            }
            else if (_degIndex < 0)
            {
                _degIndex = 0;
                return null;
            }
            await CurrentUtils.UpdateCurrentChAsync(string.Format(GlobalParams.StringFormatCH, ChThetaDeg[_degIndex].Ch));
            return ChThetaDeg[_degIndex].ThetaDeg;
        }

        public static async Task<float?> ToPrevChThetaDegAsync()
        {
            if (ChThetaDeg == null || ChThetaDeg.Length == 0)
                return null;

            if (_degIndex >= ChThetaDeg.Length)
            {
                _degIndex = ChThetaDeg.Length - 1;
                return null;
            }
            else if (_degIndex < 0)
            {
                _degIndex = 0;
                return null;
            }
            await CurrentUtils.UpdateCurrentChAsync(string.Format(GlobalParams.StringFormatCH, _degIndex > 0 ? ChThetaDeg[_degIndex - 1].Ch : ChThetaDeg.First().Ch));
            return ChThetaDeg[_degIndex--].ThetaDeg;
        }
    }

    public record IdxChData(int Ch, float ThetaDeg);
}