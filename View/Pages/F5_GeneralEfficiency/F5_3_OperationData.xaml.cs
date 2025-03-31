using System;
using System.Collections.Generic;
using System.Linq;
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
using 精密切割系统.database.db.modle;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages
{
    /// <summary>
    /// F5_3_FunctionData.xaml 的交互逻辑
    /// </summary>
    public partial class F5_3_FunctionData : Page
    {
        public F5_3_FunctionData()
        {
            InitializeComponent();
        }

        public FunctionalParametersModel functionParameter = null;

        public async void loadDBData()
        {
            var list = await SqlHelper.TableAsync<FunctionalParametersModel>().Where(t => t.Id == 1).ToListAsync();
            if (list.Count() >= 1)
            {
                functionParameter = list[0];
            }
            else
            {
                functionParameter = new FunctionalParametersModel();
                await SqlHelper.AddAsync(functionParameter);
            }
        }

        public async Task saveData()
        {
            if (functionParameter != null)
            {
                await SqlHelper.UpdateAsync(functionParameter);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await saveData();
        }
    }
}
