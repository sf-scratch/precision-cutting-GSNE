using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class ParamsConfigModel : BindableBase
    {
        private long _id;
        public long Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private long _sharpenParamsId;
        public long SharpenParamsId
        {
            get { return _sharpenParamsId; }
            set { SetProperty(ref _sharpenParamsId, value); }
        }

        private long _cutParamsId;
        public long CutParamsId
        {
            get { return _cutParamsId; }
            set { SetProperty(ref _cutParamsId, value); }
        }

        private string _describe;
        public string Describe
        {
            get { return _describe; }
            set { SetProperty(ref _describe, value); }
        }
    }
}
