using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.common
{
    public class MessageModel : BindableBase
    {
        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private string _time;
        public string Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        public static MessageModel Create(string message)
        {
            return new MessageModel() { Message = message , Time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") };
        }
    }
}
