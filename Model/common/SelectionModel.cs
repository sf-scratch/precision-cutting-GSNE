using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.common
{
    public class SelectionModel : BindableBase
    {
        private string _content;
        public string Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }

        private string _identification;
        public string Identification
        {
            get { return _identification; }
            set { SetProperty(ref _identification, value); }
        }
    }
}
