using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using 精密切割系统.Helpers;
using 精密切割系统.View.Pages;

namespace 精密切割系统.Assets.config.menu
{
    internal class MenuData
    {
        public static MenuItem GetF1Menu()
        {
            MenuItem menuItem = new MenuItem("主目录 (0.0)", GetF1MenuList(), true);
            return menuItem;
        }

        public static MenuItem GetF2Menu()
        {
            MenuItem menuItem = new MenuItem("手动操作 (1.0)", GetF2MenuList());
            return menuItem;
        }

        public static MenuItem GetF4Menu()
        {
            MenuItem menuItem = new MenuItem("刀片参数维护(3.0)", GetF4MenuList());
            return menuItem;
        }

        public static MenuItem GetF5Menu()
        {
            MenuItem menuItem = new MenuItem("一般效能参数维护(4.0)", GetF5MenuList());
            return menuItem;
        }

        //public static MenuItem GetF6Menu()
        //{
        //    MenuItem menuItem = new MenuItem("机器功能参数维护（6.0）", GetF6MenuList());
        //    return menuItem;
        //}
        public static MenuItem GetF7Menu()
        {
            MenuItem menuItem = new MenuItem("工程技术维修（5.0）", GetF7MenuList());
            return menuItem;
        }

        //F1
        public static List<MenuBean> GetF1MenuList()
        {
            var list = new List<MenuBean>();
            if (GlobalParams.HasTheta)
            {
                list.Add(new MenuBean(1, "全自动", "/Assets/icon/menu_0/menu_0_1.png", "/Assets/icon/menu_0/menu_0_1_white.png"));
            }
            list.Add(new MenuBean(2, "手动操作", "/Assets/icon/menu_0/menu_0_2.png", "/Assets/icon/menu_0/menu_0_2_white.png"));
            list.Add(new MenuBean(3, "型号目录", "/Assets/icon/menu_0/menu_0_3.png", "/Assets/icon/menu_0/menu_0_3_white.png", 2, "Pages/F3_ModelCatalog/MCDeviceDataListConf"));
            list.Add(new MenuBean(4, "刀片参数维护", "/Assets/icon/menu_0/menu_0_4.png", "/Assets/icon/menu_0/menu_0_4_white.png"));
            list.Add(new MenuBean(5, "一般效能参数维护", "/Assets/icon/menu_0/menu_0_5.png", "/Assets/icon/menu_0/menu_0_5_white.png"));
            //list.Add(new MenuBean(6, "机器效能参数维护", "/Assets/icon/menu_0/menu_0_6.png", "/Assets/icon/menu_0/menu_0_6_white.png"));
            list.Add(new MenuBean(7, "工程技术维修", "AccountWrenchOutline", "/Assets/icon/menu_0/menu_0_7_white.png"));
            // list.Add(new MenuBean(8, "电火花修刀", "/Assets/icon/menu_0/menu_0_8.png", "/Assets/icon/menu_0/menu_0_8_white.png", 3, "Pages/F8_ElectricalDischargeTruing/ElectricalDischargeTruing"));
            return list;
        }

        //F2
        public static List<MenuBean> GetF2MenuList()
        {
            var list = new List<MenuBean>();
            // list.Add(new MenuBean(201, "Teach", "/Assets/icon/menu_2/menu_2_1.png", "/Assets/icon/menu_2/menu_2_1_white.png"));
            list.Add(new MenuBean(202, "校准", "/Assets/icon/menu_2/menu_2_2.png", "/Assets/icon/menu_2/menu_2_2_white.png", 3, "Pages/F2_ManualOperation/MQManualAlignmentConf"));
            // list.Add(new MenuBean(203, "自动切割", "/Assets/icon/menu_2/menu_2_3.png", "/Assets/icon/menu_2/menu_2_3_white.png", 3, "Pages/F2_ManualOperation/MQAutoCutoConf"));
            // list.Add(new MenuBean(204, "自动切割", "/Assets/icon/menu_2/menu_2_4.png", "/Assets/icon/menu_2/menu_2_4_white.png", 3, "Pages/F2_ManualOperation/MQAutoCutoConf"));
            list.Add(new MenuBean(204, "半自动切割", "/Assets/icon/menu_2/menu_2_4.png", "/Assets/icon/menu_2/menu_2_4_white.png", 3, "Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf"));
            return list;
        }

        //F4
        public static List<MenuBean> GetF4MenuList()
        {
            var list = new List<MenuBean>();
            //list.Add(new MenuBean(401, "刀片更换", "/Assets/icon/menu_4/menu_4_1.png", "/Assets/icon/menu_4/menu_4_1_white.png",3, "Pages/F4_BladeMaintenance/FullyAutomatic"));
            list.Add(new MenuBean(409, "刀片更换", "SawBlade", "/Assets/icon/menu_4/menu_4_2_white.png", 3, "Pages/F4_BladeMaintenance/BMBladeReplacementConf"));
            // list.Add(new MenuBean(403, "刀片配置参数", "/Assets/icon/menu_4/menu_4_3.png", "/Assets/icon/menu_4/menu_4_3_white.png"));
            // list.Add(new MenuBean(404, "刀片对齐", "/Assets/icon/menu_4/menu_4_4.png", "/Assets/icon/menu_4/menu_4_4_white.png"));
            // list.Add(new MenuBean(405, "刀片状态信息", "/Assets/icon/menu_4/menu_4_5.png", "/Assets/icon/menu_4/menu_4_5_white.png"));
            if (GlobalParams.HasTheta)
            {
                list.Add(new MenuBean(439, "自动切割参数清单", "ClipboardListOutline", "/Assets/icon/menu_4/menu_4_5_white.png", 3, "Pages/Auto/AutoCutSelectConfig"));
            }
            list.Add(new MenuBean(440, "磨刀参数清单", "ClipboardListOutline", "/Assets/icon/menu_4/menu_4_5_white.png", 3, "Pages/F4_BladeMaintenance/BmSharpenParameter"));
            list.Add(new MenuBean(447, "刀片测高参数", "ArrowUpDownBoldOutline", "/Assets/icon/menu_4/menu_4_3_white.png", 2, "Pages/F4_BladeMaintenance/BMSetupDataConf"));
            list.Add(new MenuBean(501, "预切割参数维护", "FormatListNumbered", "/Assets/icon/menu_5/menu_5_1_white.png", 2, "Pages/F5_GeneralEfficiency/F5_1_PrecutData"));
            list.Add(new MenuBean(512, "刀片状态信息", "/Assets/icon/menu_5/menu_5_1.png", "/Assets/icon/menu_5/menu_5_1_white.png", 2, "Pages/F4_BladeMaintenance/BladeInfo"));
            return list;
        }

        //F5
        public static List<MenuBean> GetF5MenuList()
        {
            var list = new List<MenuBean>();
            //list.Add(new MenuBean(502, "测量维护", "/Assets/icon/menu_5/menu_5_2.png", "/Assets/icon/menu_5/menu_5_2_white.png"));
            list.Add(new MenuBean(520, "轴空运行", "/Assets/icon/menu_5/menu_5_2.png", "/Assets/icon/menu_5/menu_5_2_white.png", 3, ""));
            list.Add(new MenuBean(503, "功能参数维护", "/Assets/icon/menu_5/menu_5_3.png", "/Assets/icon/menu_5/menu_5_3_white.png", 2, "Pages/F5_GeneralEfficiency/F5_3_1_OperationData"));
            //list.Add(new MenuBean(601, "轴空转", "/Assets/icon/menu_6/menu_6_1.png", "/Assets/icon/menu_6/menu_6_1_white.png", 2, "Pages/F6_EngineeringTechnology/ETAxisIdlingMaintenanceConf"));
            list.Add(new MenuBean(605, "位置校准", "/Assets/icon/menu_6/menu_6_5.png", "/Assets/icon/menu_6/menu_6_5_white.png", 2, "Pages/F6_EngineeringTechnology/ETPositionAlignmentConf"));
            list.Add(new MenuBean(606, "各模式初始位置", "/Assets/icon/menu_6/menu_6_6.png", "/Assets/icon/menu_6/menu_6_6_white.png", 2, "Pages/F6_EngineeringTechnology/ETInitialPositionConf"));
            if (GlobalParams.HasTheta)
            {
                list.Add(new MenuBean(607, "θ轴旋转中心位校正", "CropRotate", "/Assets/icon/menu_6/menu_6_6_white.png", 3, "Pages/F4_BladeMaintenance/ThetaCenterAlignConf"));
            }
            return list;
        }

        //F6
        public static List<MenuBean> GetF6MenuList()
        {
            var list = new List<MenuBean>();
            //list.Add(new MenuBean(601, "轴空转", "/Assets/icon/menu_6/menu_6_1.png", "/Assets/icon/menu_6/menu_6_1_white.png", 2 , "Pages/F6_EngineeringTechnology/ETAxisIdlingMaintenanceConf"));
            //list.Add(new MenuBean(602, "像素尺寸测量", "/Assets/icon/menu_6/menu_6_2.png", "/Assets/icon/menu_6/menu_6_2_white.png"));
            //list.Add(new MenuBean(603, "强制维护", "/Assets/icon/menu_6/menu_6_3.png", "/Assets/icon/menu_6/menu_6_3_white.png"));
            //list.Add(new MenuBean(604, "速度设置", "/Assets/icon/menu_6/menu_6_4.png", "/Assets/icon/menu_6/menu_6_4_white.png"));
            //list.Add(new MenuBean(605, "位置校准", "/Assets/icon/menu_6/menu_6_5.png", "/Assets/icon/menu_6/menu_6_5_white.png", 2, "Pages/F6_EngineeringTechnology/ETPositionAlignmentConf"));
            //list.Add(new MenuBean(606, "各模式初始位置", "/Assets/icon/menu_6/menu_6_6.png", "/Assets/icon/menu_6/menu_6_6_white.png", 2, "Pages/F6_EngineeringTechnology/ETInitialPositionConf"));
            return list;
        }

        //F7
        public static List<MenuBean> GetF7MenuList()
        {
            var list = new List<MenuBean>();
            //list.Add(new MenuBean(709, "法兰修整", "ScissorsCutting", "/Assets/icon/menu_7/menu_7_3_white.png", 3, "Pages/F7_ElectricSpark/FlangeTrimmingConf"));
            //list.Add(new MenuBean(701, "轴运动补偿设定", "/Assets/icon/menu_7/menu_7_1.png", "/Assets/icon/menu_7/menu_7_1_white.png", 2, "Pages/F7_ElectricSpark/ESAxisDataConf"));
            // list.Add(new MenuBean(705, "精度测量", "/Assets/icon/menu_7/menu_7_5.png", "/Assets/icon/menu_7/menu_7_5_white.png", 2, "Pages/F7_ElectricSpark/FrmMain"));
            //list.Add(new MenuBean(706, "自动精度补偿", "/Assets/icon/menu_7/menu_7_5.png", "/Assets/icon/menu_7/menu_7_5_white.png", 3, "Pages/F7_ElectricSpark/AutoAlignPosition"));
            list.Add(new MenuBean(702, "I/O设备检查", "/Assets/icon/menu_7/menu_7_2.png", "/Assets/icon/menu_7/menu_7_2_white.png", 2, "Pages/F7_ElectricSpark/ESIOCheckConf"));
            list.Add(new MenuBean(703, "功能参数设定", "/Assets/icon/menu_7/menu_7_3.png", "/Assets/icon/menu_7/menu_7_3_white.png", 2, "Pages/F7_ElectricSpark/ESUserDefineDataConf"));
            list.Add(new MenuBean(704, "轴运动控制", "AxisArrow", "/Assets/icon/menu_7/menu_7_3_white.png", 2, "Pages/F7_ElectricSpark/F7_2_Axis_Operation"));
            list.Add(new MenuBean(708, "运行日志", "/Assets/icon/menu_7/menu_7_4.png", "/Assets/icon/menu_7/menu_7_4_white.png", 2, "Pages/F7_ElectricSpark/RunLogsPage"));
            // list.Add(new MenuBean(707, "轨道加油记录", "/Assets/icon/menu_7/bunkering-icon.png", "/Assets/icon/menu_7/menu_7_5_white.png", 2, "Pages/F7_ElectricSpark/BunkeringRecord"));
            // list.Add(new MenuBean(704, "制造商配置", "/Assets/icon/menu_7/menu_7_4.png", "/Assets/icon/menu_7/menu_7_4_white.png"));
            return list;
        }
    }
}