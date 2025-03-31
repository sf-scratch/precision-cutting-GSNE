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
        public CustomKeyboard()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }
        private MainWindow? mainWindow;

        private bool upperFlag = false;

        public void btnClick(object sender, string key)
        {
            // 处理按下事件
            KeyboardBtn btn = (KeyboardBtn)sender;
            //  0 是字母  null 或者是1，则是数字或者字母
            if (btn.BtnType == null)
            {
                KeyboardSimulator.SimulateKeyPress(btn.BtnValue);
            } 
            else if (btn.BtnType.Equals("0"))
            {
                // 0 
                KeyboardSimulator.SimulateKeyPress(btn.BtnValue);
            }
            else if (btn.BtnType.Equals("2"))
            {
                string sendKey = "";
                if (btn.BtnValue == "Shift")
                {
                    upperFlag = upperFlag ? false : true;
                    SetLettersCase(upperFlag);
                    sendKey = "shift";
                }
                else if (btn.BtnValue == "Backtab")
                {
                    sendKey = "shift+tab";
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
                    KeyboardSimulator.SimulateKeyPress(sendKey);
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
                    KeyboardSimulator.SimulateKeyPress(sendKey);
                }
            }
        }

        // caseType 大小写类型 0 大写 1 小写
        public void SetLettersCase(bool upperFlag)
        {
            List<KeyboardBtn> list = Tools.GetChildrenOfType<KeyboardBtn>(this);
            list.ForEach(btn =>
            {
                if ("0".Equals(btn.BtnType))
                {
                    string btnText = btn.BtnValue;
                    if (btnText != null && btnText.Length > 0)
                    {
                        btn.BtnValue = upperFlag ? btnText.ToUpper() : btnText.ToLower();
                    }
                }
                
            });
        }

        public static bool IsBetweenAandZ(char c)
        {
            return c >= 'a' && c <= 'z';
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            List<KeyboardBtn> list = Tools.GetChildrenOfType<KeyboardBtn>(this);
            list.ForEach(btn => {
                btn.KeyPressed -= btnClick;
                btn.KeyPressed += btnClick;
            });
        }

        private void functionBtn()
        {
            /*string functionText = BtnType;
            string sendKey = "";
            if (functionText == "Shift")
            {
                upperFlag = upperFlag ? false : true;
                CustomKeyboard customKeyboard = (CustomKeyboard)this.Parent;
                customKeyboard.setLettersCase(upperFlag);
            }
            else if (functionText == "Backtab")
            {
                sendKey = "+{TAB}";
            }
            else if (functionText == "Tab")
            {
                sendKey = "{TAB}";
            }
            else if (functionText == "Del")
            {
                sendKey = "{DEL}";
            }
            else if (functionText == "Home")
            {
                sendKey = "^a";
            }
            if (functionText != "")
            {
                SendKeys.Send(sendKey);
            }*/
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var _this = this;
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    List<KeyboardBtn> list = Tools.GetChildrenOfType<KeyboardBtn>(_this);
                    list.ForEach(btn => {
                        btn.KeyPressed -= btnClick;
                        btn.KeyPressed += btnClick;
                    });
                    
                }));
                
            });
            
        }

    }
}
