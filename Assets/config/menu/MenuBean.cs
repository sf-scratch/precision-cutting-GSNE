using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace 精密切割系统.Assets.config.menu
{
    public class MenuBean
    {
        public int Code { get; set; }

        // 菜单类型 1 二级菜单，2 页面菜单
        public int Type { get; set; }

        public string PageUrl { get; set; }
        public string Title { get; set; }
        public string WhiteIcon { get; set; }
        public string BlackIcon { get; set; }

        public MenuBean(int Code, string Title, string BlackIcon, string WhiteIcon, int type = 1, string pageUrl = "")
        {
            this.Code = Code;
            this.Title = Title;
            this.Type = type;
            if (type != 1)
            {
                this.PageUrl = pageUrl;
            }

            // 设置图像源
            this.BlackIcon = BlackIcon;

            // 设置图像源
            this.WhiteIcon = WhiteIcon;
        }
    }
}