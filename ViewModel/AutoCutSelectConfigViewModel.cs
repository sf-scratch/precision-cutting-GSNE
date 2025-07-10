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

        public ObservableCollection<long> AutoCutConfigIdList { get; set; }

        private long _selectedConfigId;
        public long SelectedConfigId
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

        public AutoCutSelectConfigViewModel()
        {
            _rightButtonParams = WindowLayout.RightPageButtons;
            _operatePageButtonCollection = WindowLayout.OperatePageButtons;
            AutoCutConfigIdList = new ObservableCollection<long>();
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
            NavigationParameters parameters = new NavigationParameters { { nameof(SelectedConfigId), SelectedConfigId } };
            ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(AutoCutConfig), parameters);
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private async void DeleteSelectConfig()
        {
            if (CurrentSelectedConfigId == SelectedConfigId)
            {
                MaterialSnackUtils.MaterialSnack("已选择该自动切割参数，无法删除！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            SharpenParamsEntity? sharpenParamsEnt = await connection.Table<SharpenParamsEntity>().Where(p => p.Id == SelectedConfigId).FirstOrDefaultAsync();
            CutParamsEntity? cutParamsEntity = await connection.Table<CutParamsEntity>().Where(p => p.Id == SelectedConfigId).FirstOrDefaultAsync();
            if (sharpenParamsEnt == null || cutParamsEntity == null)
            {
                MaterialSnackUtils.MaterialSnack("该自动切割参数ID不存在！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            try
            {
                await connection.RunInTransactionAsync(tx =>
                {
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
            List<long> sharpenParamsIdList = (await connection.Table<SharpenParamsEntity>().ToListAsync()).Select(p => p.Id).ToList();
            List<long> cutParamsIdList = (await connection.Table<CutParamsEntity>().ToListAsync()).Select(p => p.Id).ToList();
            AutoCutConfigIdList.Clear();
            if (sharpenParamsIdList.SequenceEqual(cutParamsIdList))
            {
                AutoCutConfigIdList.AddRange(sharpenParamsIdList);
                SelectedConfigId = await SelectedConfigEntity.GetCurrentSelectedConfigIdAsync(connection);
                CurrentSelectedConfigId = await SelectedConfigEntity.GetCurrentSelectedConfigIdAsync(connection);
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("自动切割参数列表异常！", MaterialSnackUtils.SnackType.WARNING);
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
