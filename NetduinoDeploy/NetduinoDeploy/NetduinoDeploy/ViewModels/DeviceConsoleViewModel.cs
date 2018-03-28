using System;
using Xamarin.Forms;

namespace NetduinoDeploy
{
    public class DeviceConsoleViewModel : ViewModelBase
    {
        public string ConsoleOutput
        {
            get => _consoleOutput;
            set
            {
                _consoleOutput = value;
                OnPropertyChanged(nameof(ConsoleOutput));
            }
        }
        string _consoleOutput = string.Empty;

        public Command OnClearSelected;

        public DeviceConsoleViewModel()
        {
            OnClearSelected = new Command(ClearConsole);

            MessagingCenter.Subscribe<Application, string>(this, "Console", (sender, message) => DisplayConsoleMessage(message));
        }

        void DisplayConsoleMessage(string message)
        {
            ConsoleOutput = DateTime.Now.ToShortTimeString() + " - " + message + "\r\n" + ConsoleOutput;
        }

        void ClearConsole()
        {
            ConsoleOutput = string.Empty;
        }
    }
}