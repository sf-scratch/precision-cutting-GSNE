using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;

namespace 精密切割系统.Helpers
{
    internal class CutUtils
    {
        // 验证输入的格式是否正确
        public static bool ValidateRepetitions(List<int> sequences, List<string> repetitions)
        {
            bool flag = true;
            if (sequences.Count != repetitions.Count)
            {
                return false;
            }

            bool isInRepeatBlock = false;  // 用于标记当前是否处于重复块中

            for (int i = 0; i < repetitions.Count; i++)
            {
                var repetition = repetitions[i];

                // 1. 检查 repetition 是否是 "S"、空字符串或可解析为正整数的字符串
                if (repetition != "S" && !string.IsNullOrEmpty(repetition) && !int.TryParse(repetition, out int repeatCount))
                {
                    return false;
                }

                // 2. 如果当前处于重复块中，检查是否有合法的重复次数
                if (isInRepeatBlock)
                {
                    if (string.IsNullOrEmpty(repetition))
                    {
                        continue;  // 继续等待重复次数
                    }
                    else if (int.TryParse(repetition, out repeatCount))
                    {
                        // 检查重复次数是否在 2 到 99 的范围内
                        if (repeatCount < 2 || repeatCount > 99)
                        {
                            return false;
                        }
                        isInRepeatBlock = false;  // 结束重复块
                    }
                    else
                    {
                        return false;
                    }
                }

                // 3. 如果遇到 "S"，标记进入重复块
                if (repetition == "S")
                {
                    if (isInRepeatBlock)
                    {
                        return false;
                    }
                    isInRepeatBlock = true;
                }
            }

            // 4. 如果最后还有未闭合的重复块，抛出错误
            if (isInRepeatBlock)
            {
                return false;
            }

            return flag;
        }

        public static bool ValidateInput(List<int> sequences, List<string> repetitions)
        {
            // 校验长度是否一致
            if (sequences.Count != repetitions.Count)
            {
                return false;
            }

            Stack<int> repeatStack = new Stack<int>(); // 栈，用于跟踪每个 "S" 的位置

            for (int i = 0; i < repetitions.Count; i++)
            {
                string repeat = repetitions[i];

                // 检查重复字段的合法性
                if (!string.IsNullOrEmpty(repeat) && repeat != "S" && !int.TryParse(repeat, out _))
                {
                    return false; // 如果字段既不是 "S"，也不是数字，则非法
                }

                if (repeat == "S")
                {
                    // 遇到 "S"，压入当前索引，表示开始一个新的重复区间
                    repeatStack.Push(i);
                }
                else if (int.TryParse(repeat, out _))
                {
                    // 遇到数字指令时，必须关闭最近的 "S"
                    if (repeatStack.Count == 0)
                    {
                        return false; // 没有未关闭的 "S"
                    }
                    repeatStack.Pop(); // 匹配成功，弹出栈顶的 "S"
                }
                else if (string.IsNullOrEmpty(repeat))
                {
                    // 空字符串允许，但不能关闭 "S"
                    if (repeatStack.Count > 0)
                    {
                        continue; // 如果有未关闭的 "S"，空字符串不会影响栈状态
                    }
                }
            }

            // 遍历结束后，确保所有 "S" 都已关闭
            return repeatStack.Count == 0;
        }


        public static List<int> CombineSequences2(List<int> sequences, List<string> repetitions)
        {
            List<int> finalSequence = new List<int>();
            Stack<List<int>> tempSequencesStack = new Stack<List<int>>(); // 用于支持多个重复区间
            Stack<bool> repeatingStack = new Stack<bool>(); // 标记重复状态的堆栈

            tempSequencesStack.Push(new List<int>()); // 初始状态的临时序列
            repeatingStack.Push(false); // 初始状态非重复

            for (int i = 0; i < sequences.Count; i++)
            {
                string repeat = repetitions[i];
                int currentValue = sequences[i];

                if (repeat == "S")
                {
                    // 开启新的重复区间
                    tempSequencesStack.Push(new List<int>() { currentValue });
                    repeatingStack.Push(true);
                }
                else if (string.IsNullOrEmpty(repeat))
                {
                    if (repeatingStack.Peek())
                    {
                        tempSequencesStack.Peek().Add(currentValue); // 添加到当前重复区间
                    }
                    else
                    {
                        finalSequence.Add(currentValue); // 添加到最终序列
                    }
                }
                else if (int.TryParse(repeat, out int repeatCount))
                {
                    if (repeatingStack.Peek())
                    {
                        List<int> tempSequence = tempSequencesStack.Pop(); // 结束当前重复区间
                        repeatingStack.Pop();

                        // 添加当前数字到当前重复区间
                        tempSequence.Add(currentValue);

                        // 将临时序列重复指定次数加入最终结果
                        for (int j = 0; j < repeatCount; j++)
                        {
                            finalSequence.AddRange(tempSequence);
                        }
                    }
                    else
                    {
                        finalSequence.Add(currentValue); // 非重复状态，直接加入
                    }
                }
            }

            // 如果还有未处理的临时序列，添加到最终序列
            while (tempSequencesStack.Count > 0)
            {
                finalSequence.AddRange(tempSequencesStack.Pop());
            }

            return finalSequence;
        }


        public static List<int> CombineSequences(List<int> sequences, List<string> repetitions)
        {
            // 最终生成的序列组合
            List<int> finalSequence = new List<int>();
            // 用于临时存储需要重复的序列
            List<int> tempSequence = new List<int>();
            bool isRepeating = false;  // 标记是否处于重复区间
            for (int i = 0; i < sequences.Count; i++)
            {
                // 判断是否是"S"，开始记录重复组合
                if (repetitions[i] == "S")
                {
                    tempSequence.Clear();  // 清空之前的临时组合
                    tempSequence.Add(sequences[i]);  // 添加当前序列到临时组合
                    isRepeating = true;  // 标记开始重复
                }
                // 如果是空字符串，直接添加到最终序列（或者如果在重复状态，则加入临时组合）
                else if (string.IsNullOrEmpty(repetitions[i]))
                {
                    if (isRepeating)
                    {
                        tempSequence.Add(sequences[i]);  // 处于重复状态，添加到临时组合
                    }
                    else
                    {
                        finalSequence.Add(sequences[i]);  // 不在重复状态，直接添加到最终序列
                    }
                }
                // 如果是数字，重复前面收集的临时组合
                else if (int.TryParse(repetitions[i], out int repeatCount))
                {
                    if (isRepeating)
                    {
                        tempSequence.Add(sequences[i]);  // 加上当前序列
                        if (repeatCount != 0)// 如果重复次数不为0，则添加到最终序列
                        {
                            for (int j = 0; j < repeatCount; j++)
                            {
                                finalSequence.AddRange(tempSequence);  // 将组合重复对应次数
                            }
                            isRepeating = false;  // 结束重复状态
                        }
                    }
                    else
                    {
                        finalSequence.Add(sequences[i]);  // 直接添加数字对应的序列
                    }
                }
            }

            return finalSequence;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="cutDepths"></param>
        /// <param name="feedSpeeds"></param>
        /// <param name="yIndexs"></param>
        /// <param name="cutLines"></param>
        /// <returns></returns>
        public static int AreIndexesContinuous(
            float[] cutDepths,
            float[] feedSpeeds,
            float[] yIndexs,
            float[] cutLines)
        {
            // 获取满足条件的索引
            var validIndexes = cutDepths
                .Select((value, index) => new { Value = value, Index = index })
                .Where(x => x.Value > 0 && feedSpeeds[x.Index] > 0 && yIndexs[x.Index] > 0 && cutLines[x.Index] > 0)
                .Select(x => x.Index)
                .OrderBy(x => x)
                .ToList();
            // 检查是否有有效的索引
            if (!validIndexes.Any())
            {
                return 0; // 没有符合条件的索引
            }
            // 检查索引是否连续
            bool areIndexesContinuous = validIndexes.Zip(validIndexes.Skip(1), (current, next) => next - current == 1).All(x => x);

            // 如果有效索引是连续的，返回最大索引，否则返回0
            return areIndexesContinuous ? validIndexes.Max() + 1 : 0;
        }
        // 生成包含从 "0" 到 (number - 1) 的 List<string>
        public static List<int> GenerateNumberList(int number)
        {
            List<int> list = new List<int>();

            for (int i = 0; i < number; i++)
            {
                list.Add(i + 1);
            }

            return list;
        }

        public static void UpdateGlobalRunFlag(List<OperateBean> operateBeans)
        {
            List<int> codes = operateBeans.Select(bean => bean.Code).ToList();

            if (GlobalParams.cutStatusInfo == 1)
            {
                AddCodesToGlobalRunEnable(codes);
            }
            else
            {
                RemoveCodesFromGlobalRunEnable(codes);
            }
        }

        private static void AddCodesToGlobalRunEnable(List<int> codes)
        {
            foreach (var code in codes)
            {
                GlobalParams.globalRunEnableOperateBtnCodes.Add(code);
            }
        }

        private static void RemoveCodesFromGlobalRunEnable(List<int> codes)
        {
            foreach (var code in codes)
            {
                GlobalParams.globalRunEnableOperateBtnCodes.Remove(code);
            }
        }

    }
}
