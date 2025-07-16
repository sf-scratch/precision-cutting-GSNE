using DryIoc;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.PubSubEvent;
using 精密切割系统.View.common;
using 精密切割系统.View.Pages.Auto;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutSelectConfigViewModel : CustomBindableBase
    {
        // 控制右侧按钮
        private ObservableCollection<RightButtonParams> _rightButtonParams;
        // 控制底部侧按钮
        public ObservableCollection<RightButtonParams> _operatePageButtonCollection;

        public ObservableCollection<ParamsConfigEntity> AutoCutConfigIdList { get; set; }

        private ParamsConfigEntity _selectedConfigId;
        public ParamsConfigEntity SelectedParamsConfig
        {
            get { return _selectedConfigId; }
            set { _selectedConfigId = value; RaisePropertyChanged(); }
        }

        private long _currentSelectedConfigId;
        public long CurrentSelectedConfigId
        {
            get { return _currentSelectedConfigId; }
            set { _currentSelectedConfigId = value; RaisePropertyChanged(); }
        }

        private string _describe;
        public string Describe
        {
            get { return _describe; }
            set { SetProperty(ref _describe, value); }
        }

        public AutoCutSelectConfigViewModel()
        {
            _rightButtonParams = WindowLayout.RightPageButtons;
            _operatePageButtonCollection = WindowLayout.OperatePageButtons;
            AutoCutConfigIdList = new ObservableCollection<ParamsConfigEntity>();
        }

        private void InitRightButton()
        {
            _rightButtonParams.Clear();
            _rightButtonParams.Add(RightButtonParams.GreenRightButton("确定", "/Assets/icon/right/enter.png", Sure));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitBottomButton()
        {
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("删除", "/Assets/icon/tab_1/01/tab_15.png", DeleteSelectConfig, null, 8));
            _operatePageButtonCollection.Add(RightButtonParams.BlueRightButton("新增", "/Assets/icon/tab_1/01/tab_18.png", AddConfig, null, 8));
        }

        private void AddConfig()
        {
            ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(AutoCutConfig));
        }

        private void Sure()
        {
            NavigationParameters parameters = new NavigationParameters { { "SelectedConfigId", SelectedParamsConfig.Id } };
            ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(AutoCutConfig), parameters);
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private async void DeleteSelectConfig()
        {
            if (CurrentSelectedConfigId == SelectedParamsConfig.Id)
            {
                MaterialSnackUtils.MaterialSnack("已选择该自动切割参数，无法删除！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            ParamsConfigEntity? paramsConfig = await connection.Table<ParamsConfigEntity>().Where(p => p.Id == SelectedParamsConfig.Id).FirstOrDefaultAsync();
            if (paramsConfig == null)
            {
                MaterialSnackUtils.MaterialSnack($"ParamsConfigEntity ID: {SelectedParamsConfig.Id} 不存在！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            SharpenParamsEntity? sharpenParamsEnt = await connection.Table<SharpenParamsEntity>().Where(p => p.Id == paramsConfig.SharpenParamsId).FirstOrDefaultAsync();
            CutParamsEntity? cutParamsEntity = await connection.Table<CutParamsEntity>().Where(p => p.Id == paramsConfig.CutParamsId).FirstOrDefaultAsync();
            if (sharpenParamsEnt == null || cutParamsEntity == null)
            {
                MaterialSnackUtils.MaterialSnack("该自动切割参数ID不存在！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            try
            {
                await connection.RunInTransactionAsync(tx =>
                {
                    tx.Delete(paramsConfig);
                    tx.Delete(sharpenParamsEnt);
                    tx.Delete(cutParamsEntity);
                });
                await UpdateAutoCutConfigIdListAsync();
                MaterialSnackUtils.MaterialSnack("自动切割参数删除成功！", MaterialSnackUtils.SnackType.SUCCESS);
            }
            catch (SQLiteException)
            {
                MaterialSnackUtils.MaterialSnack("自动切割参数删除失败！", MaterialSnackUtils.SnackType.WARNING);
            }
        }

        private async Task UpdateAutoCutConfigIdListAsync()
        {
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            List<ParamsConfigEntity> paramsConfigList = (await connection.Table<ParamsConfigEntity>().ToListAsync()).ToList();
            AutoCutConfigIdList.Clear();
            AutoCutConfigIdList.AddRange(paramsConfigList);
            long selectId = await SelectedConfigEntity.GetCurrentSelectedConfigIdAsync(connection);
            SelectedParamsConfig = paramsConfigList.Where(x => x.Id == selectId).FirstOrDefault() ?? new ParamsConfigEntity();
            CurrentSelectedConfigId = selectId;
            ParamsConfigEntity? paramsConfig = paramsConfigList.Where(p => p.Id == CurrentSelectedConfigId).FirstOrDefault();
            if (paramsConfig != null)
            {
                Describe = paramsConfig.Describe;
            }
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            await UpdateAutoCutConfigIdListAsync();
            InitRightButton();
            InitBottomButton();
        }
    }
}
