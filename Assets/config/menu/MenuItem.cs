using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Assets.config.menu
{
    public class MenuItem
    {
        public bool IsHome { get; set; } = false;
        public string Title { get; set; }
        public List<MenuBean> list { get; set; }
        public MenuItem(string Title, List<MenuBean> list, bool IsHome)
        {
            this.Title = Title;
            this.list = list;
            this.IsHome = IsHome;
        }

        public MenuItem(string Title, List<MenuBean> list)
        {
            this.Title = Title;
            this.list = list;
            this.IsHome = false;
        }

        public static implicit operator string(MenuItem v)
        {
            throw new NotImplementedException();
        }
    }
}
