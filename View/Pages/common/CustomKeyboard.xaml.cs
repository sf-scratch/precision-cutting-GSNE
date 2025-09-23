using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;

namespace 精密切割系统.View.Pages.common
{
    /// <summary>
    /// CustomKeyboard.xaml 的交互逻辑
    /// </summary>
    public partial class CustomKeyboard : UserControl
    {
        private MainWindow? mainWindow;
        private bool _upperFlag = true;
        public CustomKeyboard()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        public void btnClick(object sender, string key)
        {
            // 处理按下事件
            KeyboardBtn btn = (KeyboardBtn)sender;
            //  0 是字母  null 或者是1，则是数字或者字母
            if (btn.BtnType == null)
            {
                CustomKeyPress(btn.BtnValue);
            }
            else if (btn.BtnType.Equals("0"))
            {
                // 0 
                CustomKeyPress(btn.BtnValue);
            }
            else if (btn.BtnType.Equals("2"))
            {
                string sendKey = "";
                if (btn.BtnValue == "Caps")
                {
                    _upperFlag = _upperFlag ? false : true;
                    SetLettersCase(_upperFlag);
                    sendKey = "capslock";
                }
                else if (btn.BtnValue == "Shift")
                {
                    sendKey = "shift";
                }
                else if (btn.BtnValue == "Home")
                {
                    sendKey = "ctrl+a";
                }
                else if (btn.BtnValue == "Tab")
                {
                    sendKey = "tab";
                }
                else if (btn.BtnValue == ".")
                {
                    sendKey = "dot";
                }
                else if (btn.BtnValue == "Del")
                {
                    sendKey = "del";
                }
                else if (btn.BtnValue == "+")
                {
                    sendKey = "plus";
                }
                else if (btn.BtnValue == "-")
                {
                    sendKey = "minus";
                }
                else if (btn.BtnValue == "Down")
                {
                    mainWindow.ShowKeyboardPage(0);
                }
                if (!string.IsNullOrEmpty(sendKey))
                {
                    CustomKeyPress(sendKey);
                }
            }
            else if (btn.BtnType.Equals("3"))
            {
                string sendKey = "";
                if ("↑".Equals(btn.BtnValue))
                {
                    sendKey = "up";
                }
                else if ("↓".Equals(btn.BtnValue))
                {
                    sendKey = "down";
                }
                else if ("←".Equals(btn.BtnValue))
                {
                    sendKey = "left";
                }
                else if ("→".Equals(btn.BtnValue))
                {
                    sendKey = "right";
                }
                if (btn.BtnValue != "")
                {
                    CustomKeyPress(sendKey);
                }
            }
        }

        private void CustomKeyPress(string key)
        {
            Task.Run(() => KeyboardSimulator.SimulateKeyPress(key));
        }

        // caseType 大小写类型 0 大写 1 小写
        public void SetLettersCase(bool upperFlagValue)
        {
            List<KeyboardBtn> list = Tools.GetChildrenOfType<KeyboardBtn>(this);
            list.ForEach(btn =>
            {
                if ("0".Equals(btn.BtnType))
                {
                    string btnText = btn.BtnValue;
                    if (btnText != null && btnText.Length > 0)
                    {
                        btn.BtnValue = upperFlagValue ? btnText.ToUpper() : btnText.ToLower();
                    }
                }
            });
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetLettersCase(_upperFlag);
            List<KeyboardBtn> list = Tools.GetChildrenOfType<KeyboardBtn>(this);
            list.ForEach(btn => {
                btn.KeyPressed -= btnClick;
                btn.KeyPressed += btnClick;
            });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            List<KeyboardBtn> list = Tools.GetChildrenOfType<KeyboardBtn>(this);
            list.ForEach(btn => {
                btn.KeyPressed -= btnClick;
            });
        }
    }
}
