using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class LunguSksjModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _lunguId;

        public string LunguId
        {
            get { return _lunguId; }
            set { _lunguId = value; OnPropertyChanged(); }
        }


        private float _abAverageThickness;
        public float ABAverageThickness
        {
            get { return _abAverageThickness; }
            set { _abAverageThickness = value; OnPropertyChanged(); }
        }

        private float _longestBlade;
        public float LongestBlade
        {
            get { return _longestBlade; }
            set { _longestBlade = value; OnPropertyChanged(); }
        }

        private float _existingBlade;

        public float ExistingBlade
        {
            get { return _existingBlade; }
            set { _existingBlade = value; }
        }


        private string _bladeType;
        /// <summary>
        /// 刀片类型
        /// </summary>
        public string BladeType
        {
            get { return _bladeType; }
            set { _bladeType = value; OnPropertyChanged(); }
        }

        private string _orderType;
        /// <summary>
        /// 订单类型
        /// </summary>
        public string OrderType
        {
            get { return _orderType; }
            set { _orderType = value; OnPropertyChanged(); }
        }

        private string _sjSpec;
        /// <summary>
        /// 实际刀刃规格
        /// </summary>
        public string SjSpec
        {
            get { return _sjSpec; }
            set { _sjSpec = value; OnPropertyChanged(); }
        }

        private string _bladeEdgeType;
        /// <summary>
        /// 刀刃规格
        /// </summary>
        public string BladeEdgeType
        {
            get { return _bladeEdgeType; }
            set { _bladeEdgeType = value; OnPropertyChanged(); }
        }

        private float _bladeOuterDiameter;
        /// <summary>
        /// 刀片外径
        /// </summary>
        public float BladeOuterDiameter
        {
            get { return _bladeOuterDiameter; }
            set { _bladeOuterDiameter = value; OnPropertyChanged(); }
        }

        private float _ymhtxd;

        public float Ymhtxd
        {
            get { return _ymhtxd; }
            set { _ymhtxd = value; }
        }


        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
