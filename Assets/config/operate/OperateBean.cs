using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace 精密切割系统.Assets.config.buttom
{
    public class OperateBean
    {
        public int Code { get; set; }
        public string Title { get; set; }
        public string PageUrl { get; set; }
        public ImageSource Icon { get; set; }

        public OperateBean(int Code, string Title,string Icon) { 
            this.Code = Code;
            this.Title = Title;
            // 设置图像源
            BitmapImage BlacBitmapImage = new BitmapImage();
            BlacBitmapImage.BeginInit();
            BlacBitmapImage.UriSource = new Uri(Icon, UriKind.Relative);
            this.Icon = BlacBitmapImage;
            BlacBitmapImage.EndInit();
        }
        public OperateBean(int Code, string Title,string Icon,string PageUrl) { 
            this.Code = Code;
            this.Title = Title;
            this.PageUrl = PageUrl;
            // 设置图像源
            BitmapImage BlacBitmapImage = new BitmapImage();
            BlacBitmapImage.BeginInit();
            BlacBitmapImage.UriSource = new Uri(Icon, UriKind.Relative);
            this.Icon = BlacBitmapImage;
            BlacBitmapImage.EndInit();
        }
    }
}
