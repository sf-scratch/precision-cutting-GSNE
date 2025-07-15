using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.View.common;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutHistoryViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;
        public ObservableCollection<KnifeWearModel> KnifeWearList { get; set; }

        public AutoCutHistoryViewModel()
        {
        }

        public AutoCutHistoryViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            KnifeWearList = new ObservableCollection<KnifeWearModel>();
        }
        private void InitRightButton()
        {
            WindowLayout.RightPageButtons.Clear();
            WindowLayout.RightPageButtons.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void Back()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BladeReplacementConfiguration));
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            List<KnifeWearEntity> knifeWears = await connection.Table<KnifeWearEntity>().ToListAsync();
            KnifeWearList.Clear();
            foreach (KnifeWearEntity entity in knifeWears)
            {
                KnifeWearModel model = MapperConfig.Mapper.Map<KnifeWearModel>(entity);
                model.FirstCutImage = Path.Combine(AppContext.BaseDirectory, model.FirstCutImage);
                model.SecondCutImage = Path.Combine(AppContext.BaseDirectory, model.SecondCutImage);
                model.LastCutImage = Path.Combine(AppContext.BaseDirectory, model.LastCutImage);
                KnifeWearList.Add(model);
            }
            InitRightButton();
        }
    }
}
