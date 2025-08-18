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
        public string Icon { get; set; }

        public OperateBean(int Code, string Title,string Icon) { 
            this.Code = Code;
            this.Title = Title;
            this.Icon = Icon;
        }
        public OperateBean(int Code, string Title,string Icon,string PageUrl) { 
            this.Code = Code;
            this.Title = Title;
            this.PageUrl = PageUrl;
            this.Icon = Icon;
        }
    }
}
