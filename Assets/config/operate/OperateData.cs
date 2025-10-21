using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Assets.config.menu;
using 精密切割系统.Helpers;

namespace 精密切割系统.Assets.config.buttom
{
    internal class OperateData
    {
        //Tab01
        public static List<OperateBean> GetTab01Operate()
        {
            var list = new List<OperateBean>();
            if (GlobalParams.HasTheta)
            {
                list.Add(new OperateBean(5302, "关机", "Power"));
                list.Add(new OperateBean(1, "传感器吹气", "/Assets/icon/tab_0/tab_01.png"));
                list.Add(new OperateBean(2, "传感器吹水", "/Assets/icon/tab_0/tab_02.png"));
                list.Add(new OperateBean(7, "切割安全门", "/Assets/icon/tab_0/tab_09.png"));
                list.Add(new OperateBean(4, "相机安全门", "/Assets/icon/tab_0/tab_04.png"));
                list.Add(new OperateBean(6, "系统初始化", "RotateRight"));
                list.Add(new OperateBean(5, "切割水", "WaterThermometerOutline"));
                list.Add(new OperateBean(9, "相机吹气", "WeatherWindy"));
                list.Add(new OperateBean(3, "C/T真空", "VacuumOutline"));
                list.Add(new OperateBean(8, "主轴", "HorizontalRotateClockwise"));
            }
            else
            {
                list.Add(new OperateBean(5302, "关机", "Power"));
                list.Add(new OperateBean(7, "相机镜头盖", "DoorSliding"));
                list.Add(new OperateBean(9, "相机吹气", "WeatherWindy"));
                list.Add(new OperateBean(3, "C/T真空", "VacuumOutline"));
                list.Add(new OperateBean(5, "切割水", "WaterThermometerOutline"));
                list.Add(new OperateBean(6, "系统初始化", "RotateRight"));
                list.Add(new OperateBean(8, "主轴", "HorizontalRotateClockwise"));
            }
            return list;
        }

        //Tab0201
        public static List<OperateBean> GetTab0201Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(1, "刀片测高", "/Assets/icon/tab_0/tab_01.png"));
            list.Add(new OperateBean(2, "型号参数", "/Assets/icon/tab_0/tab_02.png", "Pages/F3_ModelCatalog/MCDeviceDataListConf"));
            list.Add(new OperateBean(3, "C/T真空", "/Assets/icon/tab_0/tab_03.png"));
            list.Add(new OperateBean(4, "主轴电机", "/Assets/icon/tab_0/tab_04.png"));
            list.Add(new OperateBean(5, "切割水", "/Assets/icon/tab_0/tab_05.png"));
            list.Add(new OperateBean(6, "系统初始化", "/Assets/icon/tab_0/tab_08.png"));
            list.Add(new OperateBean(7, "安全门", "/Assets/icon/tab_0/tab_09.png"));
            list.Add(new OperateBean(9, "相机吹气", "/Assets/icon/tab_0/tab_06.png"));
            list.Add(new OperateBean(-1, "推拉门", "/Assets/icon/tab_0/tab_010.png"));
            return list;
        }

        //Tab0202
        public static List<OperateBean> GetTab0202Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(2021, "Measured Align Result", "/Assets/icon/tab_1/02/tab_19.png"));
            list.Add(new OperateBean(2022, "高度辅助", "/Assets/icon/tab_1/02/tab_20.png"));
            list.Add(new OperateBean(2023, "手动校准", "/Assets/icon/tab_1/02/tab_21.png"));
            list.Add(new OperateBean(2024, "Chear No.of Work", "/Assets/icon/tab_1/02/tab_22.png"));
            list.Add(new OperateBean(3, "C/T真空", "/Assets/icon/tab_1/02/tab_23.png"));
            list.Add(new OperateBean(2026, "Dress", "/Assets/icon/tab_1/02/tab_24.png"));
            list.Add(new OperateBean(2027, "速度更改", "/Assets/icon/tab_1/02/tab_25.png"));
            list.Add(new OperateBean(2028, "型号参数", "/Assets/icon/tab_1/02/tab_26.png", "Pages/F3_ModelCatalog/MCDeviceDataListConf"));
            list.Add(new OperateBean(2029, "Precut ON", "/Assets/icon/tab_1/02/tab_27.png"));
            return list;
        }

        public static List<OperateBean> GetContactSetupOperate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(4001, "刀片更换", "/Assets/icon/tab_0/tab_01.png"));
            list.Add(new OperateBean(4470, "刀片测高参数", "/Assets/icon/tab_1/02/tab_20.png"));
            list.Add(new OperateBean(4471, "清零", "/Assets/icon/tab_5/tab_01.png"));
            // list.Add(new OperateBean(8004, "面板按钮", "/Assets/icon/tab_8/tab_85.png"));
            list.Add(new OperateBean(2422, "刀片状态信息", "/Assets/icon/tab_8/tab_85.png"));
            return list;
        }

        /// <summary>
        /// 获取空操作栏
        /// </summary>
        /// <returns></returns>
        public static List<OperateBean> GetNullOperate()
        {
            var list = new List<OperateBean>();
            return list;
        }

        //Tab03
        public static List<OperateBean> GetTab03Operate()
        {
            var list = new List<OperateBean>();
            //list.Add(new OperateBean(301, "名称分类", "/Assets/icon/tab_1/01/tab_11.png"));
            list.Add(new OperateBean(302, "拷贝", "/Assets/icon/tab_1/01/tab_12.png"));
            list.Add(new OperateBean(303, "移动", "/Assets/icon/tab_1/01/tab_13.png"));
            list.Add(new OperateBean(304, "名称变更", "/Assets/icon/tab_1/01/tab_14.png"));
            list.Add(new OperateBean(305, "删除", "/Assets/icon/tab_1/01/tab_15.png"));
            list.Add(new OperateBean(306, "指定参数", "/Assets/icon/tab_1/01/tab_16.png"));
            list.Add(new OperateBean(307, "子目录删除", "/Assets/icon/tab_1/01/tab_17.png"));
            list.Add(new OperateBean(308, "子目录作成", "/Assets/icon/tab_1/01/tab_18.png"));
            list.Add(new OperateBean(309, "导入配置", "/Assets/icon/tab_1/01/tab_18.png"));
            list.Add(new OperateBean(310, "导出配置", "/Assets/icon/tab_1/01/tab_18.png"));
            return list;
        }

        // 半自动切割操作按钮
        public static List<OperateBean> GetSemiAutoCuttingOperate(bool SpeedChange, bool HeightChange)
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(-1, "", ""));
            if (HeightChange)
            {
                list.Add(new OperateBean(2401, "高度补偿", "/Assets/icon/tab_1/02/tab_20.png"));
            }
            else
            {
                list.Add(new OperateBean(-1, "", ""));
            }
            list.Add(new OperateBean(2405, "型号参数", "/Assets/icon/tab_0/tab_02.png"));
            list.Add(new OperateBean(2023, "手动校准", "/Assets/icon/tab_1/02/tab_21.png"));
            list.Add(new OperateBean(5, "切割水", "/Assets/icon/tab_0/tab_05.png"));
            list.Add(new OperateBean(5001, "暖机", "/Assets/icon/menu_2/menu_2_3_white.png"));
            if (SpeedChange)
            {
                list.Add(new OperateBean(2403, "速度更改", "/Assets/icon/tab_1/02/tab_25.png"));
            }
            else
            {
                list.Add(new OperateBean(-1, "", ""));
            }
            list.Add(new OperateBean(2422, "刀片状态信息", "/Assets/icon/tab_1/03/tab_03.png"));
            list.Add(new OperateBean(2404, "预切启动", "/Assets/icon/tab_1/02/tab_27.png"));
            list.Add(new OperateBean(3, "C/T真空", "/Assets/icon/tab_1/02/tab_23.png"));
            return list;
        }

        // 半自动切割切割中操作按钮
        public static List<OperateBean> GetSemiAutoCuttingRunOperate(bool SpeedChange, bool HeightChange)
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(-1, "", ""));
            if (HeightChange)
            {
                // list.Add(new OperateBean(2401, "高度补偿", "/Assets/icon/tab_1/02/tab_20.png"));
                list.Add(new OperateBean(-1, "", ""));
            }
            else
            {
                list.Add(new OperateBean(-1, "", ""));
            }
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(24221, "刀片状态信息", "/Assets/icon/tab_1/03/tab_03.png"));
            list.Add(new OperateBean(24051, "型号参数", "/Assets/icon/tab_0/tab_02.png"));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(-1, "", ""));
            if (SpeedChange)
            {
                // list.Add(new OperateBean(2403, "速度更改", "/Assets/icon/tab_1/02/tab_25.png"));
                list.Add(new OperateBean(-1, "", ""));
            }
            else
            {
                list.Add(new OperateBean(-1, "", ""));
            }
            return list;
        }

        // 半自动切割暂停中操作按钮
        public static List<OperateBean> GetSemiAutoCuttingStopOperate(bool SpeedChange, bool HeightChange)
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(-1, "", "/Assets/icon/tab_1/03/tab_07.png"));
            if (HeightChange)
            {
                list.Add(new OperateBean(2401, "高度补偿", "/Assets/icon/tab_1/02/tab_20.png"));
            }
            else
            {
                list.Add(new OperateBean(-1, "", ""));
            }

            list.Add(new OperateBean(23407, "基准线调窄", "/Assets/icon/tab_1/03/tab_02.png"));
            list.Add(new OperateBean(2422, "刀片状态信息", "/Assets/icon/tab_1/03/tab_03.png"));
            list.Add(new OperateBean(2409, "基准线校准", "/Assets/icon/tab_1/03/tab_08.png"));
            list.Add(new OperateBean(2412, "画面2", "/Assets/icon/tab_1/03/tab_07.png"));
            if (SpeedChange)
            {
                list.Add(new OperateBean(2403, "速度更改", "/Assets/icon/tab_1/02/tab_25.png"));
            }
            else
            {
                list.Add(new OperateBean(-1, "", ""));
            }
            list.Add(new OperateBean(23408, "基准线调宽", "/Assets/icon/tab_1/03/tab_05.png"));
            // list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(2405, "型号参数", "/Assets/icon/tab_0/tab_02.png"));
            return list;
        }

        public static List<OperateBean> GetThetaCenterAlignConfOperate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(44002, "对焦", "/Assets/icon/tab_0/tab_02.png"));
            return list;
        }

        public static List<OperateBean> GetSemiAutoCuttingStopTwoOperate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(2406, "自动停止", "/Assets/icon/tab_1/03/tab_07.png"));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(2411, "画面1", "/Assets/icon/tab_1/03/tab_07.png"));
            list.Add(new OperateBean(2442, "对焦", "/Assets/icon/tab_1/03/tab_01.png"));
            return list;
        }

        // 半自动切割切割中操作按钮
        public static List<OperateBean> GetManualAlignmentOperate()
        {
            var list = new List<OperateBean>();
            if (GlobalParams.HasTheta)
            {
                list.Add(new OperateBean(-1, "", ""));
                list.Add(new OperateBean(2441, "全局对焦", "FocusAuto"));
                list.Add(new OperateBean(2445, "对焦确认", "/Assets/icon/tab_1/03/tab_01.png"));
                list.Add(new OperateBean(2407, "基准线调窄", "UnfoldLessHorizontal"));
                list.Add(new OperateBean(2443, "θ轴竖向校正", "/Assets/icon/tab_1/03/theta-align-vertical.png"));
                list.Add(new OperateBean(2433, "刀痕识别", "TextRecognition"));
                list.Add(new OperateBean(2442, "精细对焦", "FocusAuto"));
                list.Add(new OperateBean(2050, "测量", "/Assets/icon/tab_1/03/tab_03.png"));
                list.Add(new OperateBean(2408, "基准线调宽", "UnfoldMoreHorizontal"));
                list.Add(new OperateBean(2453, "θ轴横向校正", "/Assets/icon/tab_1/03/tab_04.png"));
            }
            else
            {
                list.Add(new OperateBean(2407, "基准线调窄", "UnfoldLessHorizontal"));
                list.Add(new OperateBean(2441, "全局对焦", "FocusAuto"));
                //list.Add(new OperateBean(2442, "精细对焦", "FocusAuto"));
                list.Add(new OperateBean(2445, "对焦确认", "/Assets/icon/tab_1/03/tab_01.png"));
                list.Add(new OperateBean(2050, "测量", "/Assets/icon/tab_1/03/tab_03.png"));
                //list.Add(new OperateBean(2433, "刀痕识别", "TextRecognition"));
                list.Add(new OperateBean(-1, "", ""));
                list.Add(new OperateBean(2408, "基准线调宽", "UnfoldMoreHorizontal"));
            }
            return list;
        }

        /// <summary>
        /// 测量操作按钮
        /// </summary>
        /// <returns></returns>
        public static List<OperateBean> GetMeasurementOperate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(2466, "Z轴上升", "/Assets/icon/tab_1/03/z_axis_up.png"));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(2407, "基准线调窄", "/Assets/icon/tab_1/03/tab_02.png"));
            list.Add(new OperateBean(2443, "θ轴竖向校正", "/Assets/icon/tab_1/03/theta-align-vertical.png"));
            list.Add(new OperateBean(2477, "Z轴下降", "/Assets/icon/tab_1/03/z_axis_down.png"));
            list.Add(new OperateBean(2442, "对焦", "/Assets/icon/tab_1/03/tab_01.png"));
            list.Add(new OperateBean(2570, "位置清零", "/Assets/icon/tab_1/03/z_axis_down.png"));
            list.Add(new OperateBean(2408, "基准线调宽", "/Assets/icon/tab_1/03/tab_05.png"));
            list.Add(new OperateBean(2453, "θ轴横向校正", "/Assets/icon/tab_1/03/tab_04.png"));
            return list;
        }

        // 对焦相关操作按钮
        public static List<OperateBean> GetManualFocusOperate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(2466, "Z轴上升", "/Assets/icon/tab_1/03/z_axis_up.png"));
            list.Add(new OperateBean(2442, "对焦", "/Assets/icon/tab_1/03/tab_01.png"));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(2477, "Z轴下降", "/Assets/icon/tab_1/03/z_axis_down.png"));
            return list;
        }

        // 4.4磨刀参数清单底部按钮
        public static List<OperateBean> GetTab4400Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(4400, "拷贝", "/Assets/icon/tab_1/01/tab_12.png"));
            list.Add(new OperateBean(4401, "删除", "/Assets/icon/tab_1/01/tab_15.png"));
            return list;
        }

        // 4.4.0磨刀参数清单-磨刀程序 底部按钮
        public static List<OperateBean> GetTab4401Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(44010, "清零", "/Assets/icon/tab_5/tab_01.png"));
            list.Add(new OperateBean(2023, "手动校准", "/Assets/icon/tab_1/02/tab_21.png"));
            list.Add(new OperateBean(3, "C/T真空", "/Assets/icon/tab_1/02/tab_27.png"));
            return list;
        }

        // 4.4.1 磨刀暂停的底部按钮
        public static List<OperateBean> GetTab4403Operate()
        {
            var list = new List<OperateBean>();
            //list.Add(new OperateBean(4406, "停止", "/Assets/icon/tab_8/tab_84.png"));
            list.Add(new OperateBean(2442, "对焦", "/Assets/icon/tab_1/03/tab_01.png"));
            /*list.Add(new OperateBean(2407, "基准线调窄", "/Assets/icon/tab_1/03/tab_02.png"));
            list.Add(new OperateBean(2408, "基准线调宽", "/Assets/icon/tab_1/03/tab_05.png"));
            list.Add(new OperateBean(2040, "崩边调窄", "/Assets/icon/tab_1/03/tab_03.png"));
            list.Add(new OperateBean(2041, "崩边调宽", "/Assets/icon/tab_1/03/tab_06.png"));
            list.Add(new OperateBean(2409, "基准线校准", "/Assets/icon/tab_1/03/tab_08.png"));*/
            return list;
        }

        // 5.1.1底部按钮
        public static List<OperateBean> GetTab5100Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(5100, "拷贝", "/Assets/icon/tab_1/01/tab_12.png"));
            list.Add(new OperateBean(5101, "删除", "/Assets/icon/tab_1/01/tab_15.png"));
            return list;
        }

        // 7.1.4 轴运动控制底部按钮
        public static List<OperateBean> GetTab7140Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(7100, "慢相对向前", "/Assets/icon/tab_7/01/tab_01.png"));
            list.Add(new OperateBean(7101, "慢相对向后", "/Assets/icon/tab_7/01/tab_02.png"));
            list.Add(new OperateBean(7102, "相对向前", "/Assets/icon/tab_7/01/tab_03.png"));
            list.Add(new OperateBean(7103, "相对向后", "/Assets/icon/tab_7/01/tab_04.png"));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(7104, "慢点动向前", "/Assets/icon/tab_7/01/tab_05.png"));
            list.Add(new OperateBean(7105, "慢点动向后", "/Assets/icon/tab_7/01/tab_06.png"));
            list.Add(new OperateBean(7106, "点动向前", "/Assets/icon/tab_7/01/tab_07.png"));
            list.Add(new OperateBean(7107, "点动向后", "/Assets/icon/tab_7/01/tab_08.png"));
            return list;
        }

        public static List<OperateBean> GetTab7120Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(7200, "上一页", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(7201, "下一页", "/Assets/icon/tab_7/tab_02.png"));
            list.Add(new OperateBean(7202, "首页", "/Assets/icon/tab_7/tab_03.png"));
            list.Add(new OperateBean(7203, "中间页", "/Assets/icon/tab_7/tab_04.png"));
            list.Add(new OperateBean(7204, "最后页", "/Assets/icon/tab_7/tab_05.png"));

            list.Add(new OperateBean(7205, "输出和输入", "/Assets/icon/tab_7/tab_06.png"));
            list.Add(new OperateBean(7206, "输入", "/Assets/icon/tab_7/tab_07.png"));
            list.Add(new OperateBean(7207, "输出", "/Assets/icon/tab_7/tab_08.png"));
            return list;
        }

        public static List<OperateBean> GetTab5110Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(5110, "位置清零", "/Assets/icon/tab_5/tab_11.png"));
            list.Add(new OperateBean(5111, "长度控制", "/Assets/icon/tab_5/tab_03.png"));
            return list;
        }

        public static List<OperateBean> GetFlangeTrimmingOperate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(7400, "X轴低速左移", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(7401, "Y轴低速后移", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(7402, "Z轴低速向上", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(-1, "", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(-1, "", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(7403, "X轴低速右移", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(7404, "Y轴低速前移", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(7405, "Z轴低速向下", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(-1, "", "/Assets/icon/tab_7/tab_01.png"));
            list.Add(new OperateBean(3, "C/T真空", "/Assets/icon/tab_0/tab_03.png"));

            return list;
        }

        public static List<OperateBean> GetTab5120Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(5120, "位置清零", "/Assets/icon/tab_5/tab_11.png"));
            list.Add(new OperateBean(5121, "刀数控制", "/Assets/icon/tab_5/tab_02.png"));
            return list;
        }

        // 电火花修刀操作按钮
        public static List<OperateBean> GetElectricalDischargeTruingOperate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(8001, "结束修刀", "/Assets/icon/tab_8/tab_84.png"));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(8002, "确认Y轴前端位置", "/Assets/icon/tab_8/tab_81.png"));
            list.Add(new OperateBean(-1, "面板按钮", "/Assets/icon/tab_8/tab_85.png"));
            list.Add(new OperateBean(-1, "", ""));
            list.Add(new OperateBean(8005, "清零", "/Assets/icon/tab_5/tab_01"));
            list.Add(new OperateBean(8006, "确认Z0位置", "/Assets/icon/tab_8/tab_82.png"));
            list.Add(new OperateBean(8003, "确认Y轴后端位置", "/Assets/icon/tab_8/tab_82.png"));

            return list;
        }

        public static List<OperateBean> GetAutoAlignPositionOperate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(7002, "停止测量", "/Assets/icon/tab_1/02/tab_21.png"));
            list.Add(new OperateBean(2023, "手动校准", "/Assets/icon/tab_1/02/tab_21.png"));
            list.Add(new OperateBean(7004, "数据导出", "/Assets/icon/tab_1/02/tab_21.png"));
            list.Add(new OperateBean(7005, "数据导入", "/Assets/icon/tab_1/02/tab_21.png"));
            return list;
        }

        // 3.1.2 目录详情
        public static List<OperateBean> GetMCDeviceDataOperate(string chName = "Ch 1")
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(3001, chName, "/Assets/icon/tab_3/tab_31.png"));
            list.Add(new OperateBean(3002, "预切割参数", "/Assets/icon/tab_3/tab_32.png"));
            list.Add(new OperateBean(3003, "功能选择", "/Assets/icon/tab_3/tab_32.png"));
            list.Add(new OperateBean(3004, "导入数据", "/Assets/icon/tab_3/tab_32.png"));
            list.Add(new OperateBean(3005, "导出数据", "/Assets/icon/tab_3/tab_32.png"));
            list.Add(new OperateBean(5002, "校准参数", "/Assets/icon/tab_3/tab_32.png"));
            return list;
        }

        // 3.1.2 目录详情
        public static List<OperateBean> GetMCDeviceDataOperate02(string chName = "Ch 1")
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(3001, chName, "/Assets/icon/tab_3/tab_31.png"));
            //list.Add(new OperateBean(3002, "预切割参数", "/Assets/icon/tab_3/tab_32.png"));
            //list.Add(new OperateBean(3003, "导入数据", "/Assets/icon/tab_3/tab_32.png"));
            //list.Add(new OperateBean(3004, "导出数据", "/Assets/icon/tab_3/tab_32.png"));
            return list;
        }

        public static List<OperateBean> GetTab53Operate()
        {
            var list = new List<OperateBean>();
            list.Add(new OperateBean(5300, "设置时日", "/Assets/icon/tab_5/tab_04.png"));
            list.Add(new OperateBean(5301, "工作盘真空", "/Assets/icon/tab_5/tab_04.png"));
            list.Add(new OperateBean(2407, "暖机", "/Assets/icon/menu_2/menu_2_3_white.png"));
            return list;
        }
    }
}