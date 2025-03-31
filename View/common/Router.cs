using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.View.common
{
    //全局路由，负责路由调整，页面显示等功能
    internal class Router
    {
        /// <summary>
        /// 主页
        /// </summary>
        public static void ToMainPage()
        {
            // 设置主菜单
            //MainForm.Instance.BringControlToFront(MainForm.Instance.MainContainer, new MainMenu());
            // 右边
            //MainForm.Instance.BringControlToFront(MainForm.Instance.LeftContainer, new MainDeviceStatus());
            // 操作栏
            //MainForm.Instance.BringControlToFront(MainForm.Instance.OperateContainer, new MainOperate());
        }
    }
}
