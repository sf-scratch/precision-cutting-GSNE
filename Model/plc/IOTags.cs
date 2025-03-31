using HslCommunication.Instrument.CJT;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Driver;

namespace 精密切割系统.Model.plc
{
    internal class IOTags
    {
        
        public static List<Driver.Tag> ioTagsDI = new List<Driver.Tag>
        {
            new Driver.Tag("R30000", "X轴正限位", "bool"),
            new Driver.Tag("R30001", "X轴负限位", "bool"),
            new Driver.Tag("R30002", "X轴伺服错误", "bool"),
            new Driver.Tag("R30003", "Y轴正限位", "bool"),
            new Driver.Tag("R30004", "Y轴负限位", "bool"),
            new Driver.Tag("R30005", "Y轴伺服错误", "bool"),

            new Driver.Tag("R30006", "Z1轴正限位", "bool"),
            new Driver.Tag("R30007", "Z1轴负限位", "bool"),
            new Driver.Tag("R30008", "Z1轴伺服错误", "bool"),
            new Driver.Tag("R30012", "Z1轴刹车允许打开", "bool"),

            new Driver.Tag("R30009", "相机轴正限位", "bool"),
            new Driver.Tag("R30010", "相机轴负限位", "bool"),
            new Driver.Tag("R30011", "相机轴伺服错误", "bool"),
            new Driver.Tag("R30013", "相机轴刹车允许打开", "bool"),

            new Driver.Tag("R30015", "θ轴伺服错误", "bool"),
            new Driver.Tag("R30014", "修刀轴伺服错误", "bool"),

            new Driver.Tag("R37000", "手动正转按钮", "bool"),
            new Driver.Tag("R37001", "手动反转按钮", "bool"),
            new Driver.Tag("R37002", "手动X轴选择", "bool"),
            new Driver.Tag("R37003", "手动Y轴选择", "bool"),
            new Driver.Tag("R37004", "手动Z1轴选择", "bool"),
            new Driver.Tag("R37005", "手动高速", "bool"),
            new Driver.Tag("R37006", "手动低速", "bool"),
            new Driver.Tag("R37007", "寸动", "bool"),
            new Driver.Tag("R37014", "电极限位OK", "bool"),
            new Driver.Tag("R37015", "翻盖门锁紧", "bool"),

            new Driver.Tag("R37100", "急停", "bool"),
            new Driver.Tag("R37101", "主轴气源压力检测", "bool"),
            new Driver.Tag("R37104", "推拉门关闭检测", "bool"),
            new Driver.Tag("R37105", "推拉门推开检测", "bool"),
            new Driver.Tag("R37106", "工件真空压力检测", "bool"),
            new Driver.Tag("R37107", "主轴切割水流量检测", "bool"),
            new Driver.Tag("R37110", "冷却设备温度调试中", "bool"),
            new Driver.Tag("R37111", "冷却设备错误复位输入", "bool"),
            new Driver.Tag("R37112", "电火花阳极输入", "bool"),
            new Driver.Tag("R37113", "电火花阴极输入", "bool"),

            new Driver.Tag("R37202", "主轴变频器启动运行中", "bool"),
            new Driver.Tag("R37203", "主轴变频器启动速度达到", "bool"),
            new Driver.Tag("R37204", "主轴变频器报警", "bool"),
            new Driver.Tag("R37210", "丝杆润滑油准备好", "bool"),
            new Driver.Tag("R37211", "丝杆润滑设备润滑液位检测", "bool"),
            new Driver.Tag("R37212", "主轴冷却水流量检测", "bool"),
            new Driver.Tag("R37213", "电极限位OK", "bool"),

            new Driver.Tag("R37300", "切割水温度正常", "bool"),
            new Driver.Tag("R37301", "切割水工作中", "bool"),
            new Driver.Tag("R37302", "切割水液面正常", "bool"),
            new Driver.Tag("R37303", "主轴冷却水工作中", "bool"),
            new Driver.Tag("R37304", "主轴冷却水液面正常", "bool"),
            new Driver.Tag("R37305", "主轴冷却温度正常", "bool"),
            new Driver.Tag("R37306", "主轴冷却停止工作", "bool")
        };

        public static List<Driver.Tag> ioTagsDO = new List<Driver.Tag>
        {
            new Driver.Tag("R30104", "MR1200", "冷却系统工作中", "bool", ""),
            new Driver.Tag("R30104", "MR1201", "红色指示", "bool", ""),
            new Driver.Tag("R30104", "MR1202", "黄色指示", "bool", ""),
            new Driver.Tag("R30104", "MR1203", "绿色指示", "bool", ""),
            new Driver.Tag("R30104", "MR1204", "蜂鸣器", "bool", ""),
            new Driver.Tag("R30104", "MR1205", "电极气缸", "bool", ""),
            new Driver.Tag("R30104", "MR1206", "工件真空", "bool", ""),
            new Driver.Tag("R30104", "MR1207", "推拉安全门锁紧", "bool", ""),
            new Driver.Tag("R30104", "MR1208", "翻转安全门锁紧", "bool", ""),
            new Driver.Tag("R30104", "MR1209", "工件真空关闭", "bool", ""),
            new Driver.Tag("R30104", "MR1210", "推拉安全门解锁", "bool", ""),
            new Driver.Tag("R30104", "MR1211", "相机吹气", "bool", ""),
            new Driver.Tag("R30104", "MR1212", "切割水", "bool", ""),
            new Driver.Tag("R30104", "MR1213", "主轴运行中", "bool", ""),
            new Driver.Tag("R30104", "MR1214", "主轴报警复位", "bool", ""),
            new Driver.Tag("R30104", "MR1215", "主轴运动使能", "bool", ""),
            new Driver.Tag("R30104", "MR1300", "丝杆润滑泵", "bool", ""),
            new Driver.Tag("R30104", "MR1301", "电火花阳极放电选择", "bool", ""),
            new Driver.Tag("R30104", "MR1302", "电火花阴极放电选择", "bool", ""),
            new Driver.Tag("R30104", "MR1303", "电火花放电中", "bool", ""),
            new Driver.Tag("R30104", "MR1304", "电火花准备好", "bool", ""),
            new Driver.Tag("R30104", "MR1305", "电火花阳极放电显示", "bool", ""),
            new Driver.Tag("R30104", "MR1306", "电火花阴极放电显示", "bool", "")
        };
    }
}
