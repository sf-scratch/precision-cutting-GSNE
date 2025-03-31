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
        public ImageSource WhiteIcon { get; set; }
        public ImageSource BlackIcon { get; set; }

        public MenuBean(int Code, string Title, string BlackIcon, string WhiteIcon, int type = 1, string pageUrl = "")
        {
            this.Code = Code;
            this.Title = Title;
            this.Type = type;
            if (type != 1) { 
                this.PageUrl = pageUrl;
            }


            // 设置图像源
            BitmapImage BlacBitmapImage = new BitmapImage();
            BlacBitmapImage.BeginInit();
            BlacBitmapImage.UriSource = new Uri(BlackIcon, UriKind.Relative);
            this.BlackIcon = BlacBitmapImage;
            BlacBitmapImage.EndInit();

            // 设置图像源
            BitmapImage WhiteBitmapImage = new BitmapImage();
            WhiteBitmapImage.BeginInit();
            WhiteBitmapImage.UriSource = new Uri(WhiteIcon, UriKind.Relative);
            this.WhiteIcon = WhiteBitmapImage;
            WhiteBitmapImage.EndInit();
        }

    }
}
