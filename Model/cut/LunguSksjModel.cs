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

        private string _bladeEdgeType;
        /// <summary>
        /// 刀刃规格
        /// </summary>
        public string BladeEdgeType
        {
            get { return _bladeEdgeType; }
            set { _bladeEdgeType = value; OnPropertyChanged(); }
        }

        private string _bladeOuterDiameter;
        /// <summary>
        /// 刀片外径
        /// </summary>
        public string BladeOuterDiameter
        {
            get { return _bladeOuterDiameter; }
            set { _bladeOuterDiameter = value; OnPropertyChanged(); }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
