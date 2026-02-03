using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static SQLite.SQLite3;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BmContactSetupConf.xaml 的交互逻辑
    /// </summary>
    public partial class BmContactSetupConf : Page
    {
        public BmContactSetupConf()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void OperateClicked(object? sender, int code)
        {
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
        }

        //初始化数据
        private async Task initData()
        {
        }

        public void initTbNumber()
        {
        }

        public void initSetupValue()
        {
        }

        private void BtnContactSetupSure_RightClicked(object? sender, bool e)
        {
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}