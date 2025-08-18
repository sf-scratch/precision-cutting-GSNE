using MaterialDesignThemes.Wpf;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.View.common;
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutHistoryViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;

        private AsyncDelegateCommand _selectStartDateDialogCommand;
        public AsyncDelegateCommand SelectStartDateDialogCommand =>
            _selectStartDateDialogCommand ?? (_selectStartDateDialogCommand = new AsyncDelegateCommand(ExecuteSelectStartDateDialogCommand));

        async Task ExecuteSelectStartDateDialogCommand()
        {
            var res = await DialogHost.Show(new SelectDateTimeDialog());
            if (res is DateTime dateTime && dateTime != default)
            {
                StartDate = dateTime;
                await UpdateKnifeWearListAsync();
            }
        }

        private AsyncDelegateCommand _selectEndDateDialogCommand;
        public AsyncDelegateCommand SelectEndDateDialogCommand =>
            _selectEndDateDialogCommand ?? (_selectEndDateDialogCommand = new AsyncDelegateCommand(ExecuteSelectEndDateDialogCommand));

        async Task ExecuteSelectEndDateDialogCommand()
        {
            var res = await DialogHost.Show(new SelectDateTimeDialog());
            if (res is DateTime dateTime && dateTime != default)
            {
                EndDate = dateTime;
                await UpdateKnifeWearListAsync();
            }
        }

        public ObservableCollection<KnifeWearModel> KnifeWearList { get; set; }

        private Visibility _progressBarVisibility;
        public Visibility ProgressBarVisibility
        {
            get { return _progressBarVisibility; }
            set { SetProperty(ref _progressBarVisibility, value); }
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get { return _startDate; }
            set { SetProperty(ref _startDate, value); }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get { return _endDate; }
            set { SetProperty(ref _endDate, value); }
        }

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
            RightButtonCollection.Clear();
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void Back()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BladeReplacementConfiguration));
        }

        private async Task UpdateKnifeWearListAsync()
        {
            KnifeWearList.Clear();
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            List<KnifeWearEntity> knifeWears = await connection.Table<KnifeWearEntity>().Where(p => p.StartTime >= StartDate && p.StartTime <= EndDate).ToListAsync();
            foreach (KnifeWearEntity entity in knifeWears)
            {
                KnifeWearModel model = MapperConfig.Mapper.Map<KnifeWearModel>(entity);
                model.FirstCutImage = Path.Combine(AppContext.BaseDirectory, model.FirstCutImage ?? string.Empty);
                model.SecondCutImage = Path.Combine(AppContext.BaseDirectory, model.SecondCutImage ?? string.Empty);
                model.LastCutImage = Path.Combine(AppContext.BaseDirectory, model.LastCutImage ?? string.Empty);
                KnifeWearList.Add(model);
            }
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            try
            {
                StartDate = DateTime.Now.AddDays(-7);
                EndDate = DateTime.Now;
                ProgressBarVisibility = Visibility.Visible;
                await UpdateKnifeWearListAsync();
            }
            finally
            {
                ProgressBarVisibility = Visibility.Collapsed;
                InitRightButton();
            }
        }
    }
}
