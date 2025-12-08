using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.cut
{
    public class ThetaAlignService
    {
        private static readonly Lazy<ThetaAlignService> _lazy = new(() => new ThetaAlignService());

        public static ThetaAlignService Instance
        {
            get { return _lazy.Value; }
        }

        private const int AlignOutTime = 40;
        private const int AlignDefaultMoveDistance = 40;

        private ThetaAlignStatus _currentThetaAlignStatus;

        public ThetaAlignStatus CurrentThetaAlignStatus
        {
            get { return _currentThetaAlignStatus; }
        }

        private float _thetaAlignCompletedDeg;

        public float? ThetaAlignCompletedDeg
        {
            get { return _currentThetaAlignStatus == ThetaAlignStatus.Completed ? _thetaAlignCompletedDeg : null; }
        }

        private PointF _pointA;
        private SemaphoreSlim _thetaAlignSemaphore;

        private ThetaAlignService()
        {
            _thetaAlignSemaphore = new SemaphoreSlim(1, 1);
        }

        public async Task ThetaHorizontalAlignAsync()
        {
            if (!await _thetaAlignSemaphore.WaitAsync(TimeSpan.Zero))
            {
                MaterialSnackUtils.MaterialSnack("拉直中，请勿重复点击！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(AlignOutTime));
                CancellationToken token = cts.Token;
                DataPoint<float> cameraThetaCenterPoint = Appsettings.CameraThetaCenterPoint;
                PointF center = new PointF(cameraThetaCenterPoint.X, cameraThetaCenterPoint.Y);
                float xLocation, yLocation;
                switch (_currentThetaAlignStatus)
                {
                    case ThetaAlignStatus.Horizontal:
                        MaterialSnackUtils.MaterialSnack("横向拉直中！", MaterialSnackUtils.SnackType.SUCCESS, 0);
                        xLocation = await PlcControl.tagControl.Xaxis.GetCurrentLocationWaitAsync(token) ?? 0;
                        yLocation = await PlcControl.tagControl.Yaxis.GetCurrentLocationWaitAsync(token) ?? 0;
                        PointF pointB = new PointF(xLocation, yLocation);
                        float angle = CalculateAngleToHorizontal(_pointA, pointB);
                        float distance = -CalculateSignedDistance(_pointA, pointB, center);
                        PointF rotatePointA = RotatePointAroundCenter(_pointA, center, angle);
                        Tools.LogInfo($"A点 X:{_pointA.X} Y:{_pointA.Y}");
                        Tools.LogInfo($"B点 X:{xLocation} Y:{yLocation}");
                        Tools.LogInfo($"center X:{center.X} Y:{center.Y}");
                        Tools.LogInfo($"校正角度:{angle}");
                        Tools.LogInfo($"返回A位置:{rotatePointA.X}  {center.Y + distance}");
                        Task thetaTask = PlcControl.tagControl.ThetaAxis.StartRelativeAsync(angle, default, token);
                        Task xTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(rotatePointA.X, 80, token);
                        Task yTask = PlcControl.tagControl.Yaxis.StartAbsoluteAsync(center.Y + distance, default, token);
                        await Task.WhenAll(thetaTask, xTask, yTask);
                        _currentThetaAlignStatus = ThetaAlignStatus.Completed;
                        //SetCalibrationAngle();
                        _thetaAlignCompletedDeg = await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync() ?? 0;
                        MaterialSnackUtils.MaterialSnack("横向拉直完成！", MaterialSnackUtils.SnackType.SUCCESS);
                        break;

                    case ThetaAlignStatus.Vertical:
                        _currentThetaAlignStatus = ThetaAlignStatus.None;
                        MaterialSnackUtils.MaterialSnack("已取消竖向拉直!", MaterialSnackUtils.SnackType.WARNING);
                        break;

                    default:
                        xLocation = await PlcControl.tagControl.Xaxis.GetCurrentLocationWaitAsync(token) ?? 0;
                        yLocation = await PlcControl.tagControl.Yaxis.GetCurrentLocationWaitAsync(token) ?? 0;
                        _pointA = new PointF(xLocation, yLocation);
                        await PlcControl.tagControl.Xaxis.StartRelativeAsync(Appsettings.HorizontalStraighteningStroke ?? AlignDefaultMoveDistance, 80, default);
                        _currentThetaAlignStatus = ThetaAlignStatus.Horizontal;
                        MaterialSnackUtils.MaterialSnack("请继续横向拉直第二点", MaterialSnackUtils.SnackType.SUCCESS);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack("横向拉直超时！", MaterialSnackUtils.SnackType.WARNING);
            }
            finally
            {
                _thetaAlignSemaphore.Release();
            }
        }

        public async Task ThetaVerticalAlignAsync()
        {
            if (!await _thetaAlignSemaphore.WaitAsync(TimeSpan.Zero))
            {
                MaterialSnackUtils.MaterialSnack("拉直中，请勿重复点击！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(AlignOutTime));
                CancellationToken token = cts.Token;
                DataPoint<float> cameraThetaCenterPoint = Appsettings.CameraThetaCenterPoint;
                PointF center = new PointF(cameraThetaCenterPoint.X, cameraThetaCenterPoint.Y);
                float xLocation, yLocation;
                switch (_currentThetaAlignStatus)
                {
                    case ThetaAlignStatus.Horizontal:
                        _currentThetaAlignStatus = ThetaAlignStatus.None;
                        MaterialSnackUtils.MaterialSnack("已取消横向拉直!", MaterialSnackUtils.SnackType.WARNING);
                        break;

                    case ThetaAlignStatus.Vertical:
                        MaterialSnackUtils.MaterialSnack("竖向拉直中！", MaterialSnackUtils.SnackType.SUCCESS, 0);
                        xLocation = await PlcControl.tagControl.Xaxis.GetCurrentLocationWaitAsync(token) ?? 0;
                        yLocation = await PlcControl.tagControl.Yaxis.GetCurrentLocationWaitAsync(token) ?? 0;
                        PointF pointB = new PointF(xLocation, yLocation);
                        float angle = CalculateAngleToVertical(_pointA, pointB);
                        float distance = -CalculateSignedDistance(_pointA, pointB, center);
                        PointF rotatePointA = RotatePointAroundCenter(_pointA, center, angle);
                        Tools.LogInfo($"A点 X:{_pointA.X} Y:{_pointA.Y}");
                        Tools.LogInfo($"B点 X:{xLocation} Y:{yLocation}");
                        Tools.LogInfo($"center X:{center.X} Y:{center.Y}");
                        Tools.LogInfo($"校正角度:{angle}");
                        Tools.LogInfo($"返回A位置:{center.X + distance}  {rotatePointA.Y}");
                        Task thetaTask = PlcControl.tagControl.ThetaAxis.StartRelativeAsync(angle, default, token);
                        Task xTask = PlcControl.tagControl.Xaxis.StartAbsoluteAsync(center.X + distance, default, token);
                        Task yTask = PlcControl.tagControl.Yaxis.StartAbsoluteAsync(rotatePointA.Y, 60, token);
                        await Task.WhenAll(thetaTask, xTask, yTask);
                        _currentThetaAlignStatus = ThetaAlignStatus.Completed;
                        //SetCalibrationAngle();
                        _thetaAlignCompletedDeg = await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync() ?? 0;
                        MaterialSnackUtils.MaterialSnack("竖向拉直完成！", MaterialSnackUtils.SnackType.SUCCESS);
                        break;

                    default:
                        xLocation = await PlcControl.tagControl.Xaxis.GetCurrentLocationWaitAsync(token) ?? 0;
                        yLocation = await PlcControl.tagControl.Yaxis.GetCurrentLocationWaitAsync(token) ?? 0;
                        _pointA = new PointF(xLocation, yLocation);
                        await PlcControl.tagControl.Yaxis.StartRelativeAsync(-Appsettings.VerticalStraighteningStroke ?? -AlignDefaultMoveDistance, 60, default);
                        _currentThetaAlignStatus = ThetaAlignStatus.Vertical;
                        MaterialSnackUtils.MaterialSnack("请继续竖向拉直第二点", MaterialSnackUtils.SnackType.SUCCESS);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack("竖向拉直超时！", MaterialSnackUtils.SnackType.WARNING);
            }
            finally
            {
                _thetaAlignSemaphore.Release();
            }
        }

        public void Reset()
        {
            _currentThetaAlignStatus = ThetaAlignStatus.None;
            _thetaAlignCompletedDeg = 0;
        }

        private static float CalculateAngleToVertical(PointF pointA, PointF pointB)
        {
            // 计算两点之间的差值（向量）
            float dx = pointB.X - pointA.X;
            float dy = pointB.Y - pointA.Y;

            // 计算相对于正Y轴（竖直向上）的角度
            float angleRadians = MathF.Atan2(dx, dy); // 注意参数顺序为(dx, dy)

            // 转换为角度（弧度转角度）
            float angleDegrees = angleRadians * (180.0f / MathF.PI);

            // 调整角度范围到[-180, 180]
            if (angleDegrees > 180)
                angleDegrees -= 360;
            else if (angleDegrees < -180)
                angleDegrees += 360;

            // 检查是否需要转换为负角度（如果绝对值更小）
            if (angleDegrees > 90)
                angleDegrees -= 180;
            else if (angleDegrees < -90)
                angleDegrees += 180;

            return angleDegrees;
        }

        private static float CalculateAngleToHorizontal(PointF pointA, PointF pointB)
        {
            // 计算两点之间的差值
            float dx = pointB.X - pointA.X;
            float dy = pointB.Y - pointA.Y;

            // 使用Math.Atan2计算角度（弧度）
            // 注意：Atan2返回的是从正X轴逆时针方向的角度
            float angleRadians = MathF.Atan2(dy, dx);

            // 转换为顺时针为正的角度（取负值）
            angleRadians = -angleRadians;

            // 将弧度转换为角度
            float angleDegrees = angleRadians * (180.0f / MathF.PI);

            // 规范化角度到[-180, 180]范围
            if (angleDegrees > 180)
                angleDegrees -= 360;
            else if (angleDegrees < -180)
                angleDegrees += 360;

            // 检查是否需要转换为负角度（如果绝对值更小）
            if (angleDegrees > 90)
                angleDegrees -= 180;
            else if (angleDegrees < -90)
                angleDegrees += 180;

            return angleDegrees;
        }

        private static float CalculateSignedDistance(PointF linePointA, PointF linePointB, PointF testPoint)
        {
            // 计算直线向量
            float dx = linePointB.X - linePointA.X;
            float dy = linePointB.Y - linePointA.Y;

            // 计算叉积 (dx*(testY-linePointA.Y) - dy*(testX-linePointA.X))
            float cross = dx * (testPoint.Y - linePointA.Y) - dy * (testPoint.X - linePointA.X);

            // 计算直线长度（取正值）
            float len = MathF.Sqrt(dx * dx + dy * dy);

            // 避免除以零（如果两点重合）
            if (len == 0)
                throw new ArgumentException("直线的两个端点不能重合");

            // 返回有向距离（向上为负，向下为正）
            return cross / len;
        }

        /// <summary>
        /// 点绕圆心旋转（顺时针）
        /// </summary>
        private static PointF RotatePointAroundCenter(PointF point, PointF center, float angle)
        {
            // 转换为弧度
            float theta = angle * MathF.PI / 180f;
            float x = point.X - center.X;
            float y = point.Y - center.Y;

            // 右手系顺时针旋转矩阵
            float newX = x * MathF.Cos(theta) - y * MathF.Sin(theta);  // sin项取负
            float newY = x * MathF.Sin(theta) + y * MathF.Cos(theta);  // 与左手系相反

            return new PointF(newX + center.X, newY + center.Y);
        }

        private void SetCalibrationAngle()
        {
            // 获取当前面的切割角度，然后用当前角度减去切割角度 等于拉直误差角度
            FileTableItemChModel chModel = CurrentUtils.GetFileTableItemChModel();
            float tempCh = Tools.GetFloatStringValue(chModel.ThetaDeg);
            float thetaCurrentDeg = Tools.GetFloatStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.thetaCurLocationKey));
            GlobalParams.calibrationAngle = thetaCurrentDeg - tempCh;
            Tools.LogInfo($"GlobalParams.calibrationAngle:{GlobalParams.calibrationAngle}");
            Tools.LogInfo($"thetaCurrentDeg:{thetaCurrentDeg}");
            Tools.LogInfo($"tempCh:{tempCh}");
        }
    }

    public enum ThetaAlignStatus
    {
        None,
        Horizontal,
        Vertical,
        Completed
    }
}