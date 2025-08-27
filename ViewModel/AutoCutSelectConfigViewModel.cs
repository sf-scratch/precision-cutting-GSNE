using DryIoc;
using MaterialDesignThemes.Wpf;
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
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.Pages.Auto;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutSelectConfigViewModel : CustomBindableBase
    {
        public ObservableCollection<ParamsConfigEntity> AutoCutConfigIdList { get; }

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
            AutoCutConfigIdList = new ObservableCollection<ParamsConfigEntity>();
        }

        private void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(RightButtonParams.GreenRightButton("进入", "/Assets/icon/right/enter.png", Sure));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitBottomButton()
        {
            BottomButtonCollection.Add(RightButtonParams.BlueButton("新增", "FormatListGroupPlus", AddConfig));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("删除", "DeleteOutline", DeleteSelectConfig));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("拷贝", "ContentCopy", CopyConfig));
        }

        private async void CopyConfig()
        {
            if ((await DialogHost.Show(SelectionDialog.NewInstance("确认拷贝", noBtn:"取消"))) is not string dialogResult || dialogResult != SelectionDialog.YES)
            {
                return;
            }
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync; 
            try
            {
                await connection.RunInTransactionAsync(tx =>
                {
                    ParamsConfigEntity paramsConfig = tx.Table<ParamsConfigEntity>().Where(p => p.Id == SelectedParamsConfig.Id).FirstOrDefault();
                    paramsConfig.Id = -1;
                    tx.Insert(paramsConfig);
                    paramsConfig.SharpenParamsId = paramsConfig.Id;
                    paramsConfig.CutParamsId = paramsConfig.Id;
                    tx.Update(paramsConfig);
                    SharpenParamsEntity sharpenParams = tx.Table<SharpenParamsEntity>().Where(p => p.Id == SelectedParamsConfig.SharpenParamsId).FirstOrDefault();
                    sharpenParams.Id = paramsConfig.SharpenParamsId;
                    tx.Insert(MapperConfig.Mapper.Map<SharpenParamsEntity>(sharpenParams));
                    CutParamsEntity cutParams = tx.Table<CutParamsEntity>().Where(p => p.Id == SelectedParamsConfig.CutParamsId).FirstOrDefault();
                    cutParams.Id = paramsConfig.CutParamsId;
                    tx.Insert(MapperConfig.Mapper.Map<CutParamsEntity>(cutParams));
                });
                await UpdateAutoCutConfigIdListAsync();
                MaterialSnackUtils.MaterialSnack("自动切割参数拷贝成功！", MaterialSnackUtils.SnackType.SUCCESS);
            }
            catch (Exception)
            {
                MaterialSnackUtils.MaterialSnack("自动切割参数拷贝失败！", MaterialSnackUtils.SnackType.WARNING);
            }
        }

        private void AddConfig()
        {
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
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
            if ((await DialogHost.Show(SelectionDialog.NewInstance("确认删除", noBtn: "取消"))) is not string dialogResult || dialogResult != SelectionDialog.YES)
            {
                return;
            }
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
