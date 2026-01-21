using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.View.Pages.F3_ModelCatalog;

namespace 精密切割系统.Helpers
{
    internal class VerifyUtils
    {
        /// <summary>
        /// 检查自动切割高度补偿
        /// </summary>
        /// <param name="cutSteps"></param>
        /// <returns></returns>
        public static async Task<CommonResult> CheckAutomaticCompensationCutHeightAsync(List<CutStep> cutSteps)
        {
            if (cutSteps == null || cutSteps.Count == 0)
            {
                return CommonResult.Failure("切割步骤数据异常，请重新设置切割参数！");
            }
            float depthCompensationValue = SemiAutoCutService.Instance.DepthCompensationValue;
            AutomaticCompensationCutHeightEntity automaticCompensationCutHeight = await SqlHelper.GetOrCreateEntityAsync(() => new AutomaticCompensationCutHeightEntity());
            int cutHeightCompensationFrequency = automaticCompensationCutHeight.CutHeightCompensationFrequency.ToInt();
            float cutHeightReductionDistance = automaticCompensationCutHeight.CutHeightReductionDistance.ToFloat();
            if (cutHeightCompensationFrequency <= 0 || cutHeightReductionDistance <= 0)
            {
                return CommonResult.Success();
            }
            float minCutHeight = cutSteps.Select(p => p.CutHeight).Min();
            int frequencyTimes = (int)(minCutHeight / cutHeightReductionDistance);
            long unsafeCutTimes = frequencyTimes * cutHeightCompensationFrequency;
            if (unsafeCutTimes < cutSteps.Count)
            {
                return CommonResult.Failure($"将在第 {unsafeCutTimes} 刀时切到工作盘，请检查型号参数设置！");
            }
            return CommonResult.Success();
        }

        /// <summary>
        /// 检查自动切割高度补偿
        /// </summary>
        /// <param name="cutSteps"></param>
        /// <returns></returns>
        public static async Task<CommonResult> CheckAutomaticCompensationCutHeightAsync(List<ChCutStep> cutSteps)
        {
            if (cutSteps == null || cutSteps.Count == 0)
            {
                return CommonResult.Failure("切割步骤数据异常，请重新设置切割参数！");
            }
            float depthCompensationValue = SemiAutoCutService.Instance.DepthCompensationValue;
            AutomaticCompensationCutHeightEntity automaticCompensationCutHeight = await SqlHelper.GetOrCreateEntityAsync(() => new AutomaticCompensationCutHeightEntity());
            int cutHeightCompensationFrequency = automaticCompensationCutHeight.CutHeightCompensationFrequency.ToInt();
            float cutHeightReductionDistance = automaticCompensationCutHeight.CutHeightReductionDistance.ToFloat();
            if (cutHeightCompensationFrequency <= 0 || cutHeightReductionDistance <= 0)
            {
                return CommonResult.Success();
            }
            float minCutHeight = float.MaxValue;
            int totalCutSteps = 0;
            foreach (var step in cutSteps)
            {
                minCutHeight = Math.Min(minCutHeight, step.CutSteps.Select(p => p.CutHeight).Min());
                totalCutSteps += step.CutSteps.Count;
            }
            int frequencyTimes = (int)(minCutHeight / cutHeightReductionDistance);
            long unsafeCutTimes = frequencyTimes * cutHeightCompensationFrequency;
            if (unsafeCutTimes < totalCutSteps)
            {
                return CommonResult.Failure($"将在第 {unsafeCutTimes} 刀时切到工作盘，请检查型号参数设置！");
            }
            return CommonResult.Success();
        }
    }
}