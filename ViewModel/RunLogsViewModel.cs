using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Model.sqlite;

namespace 精密切割系统.ViewModel
{
    class RunLogsViewModel
    {
        public string title { get; set; }
        public string content { get; set; }
        public RunLogsViewModel()
        {
        }
        public RunLogsViewModel(string titleParams, string contentParams)
        {
            this.title = titleParams;
            this.content = contentParams;
        }
    }
}
