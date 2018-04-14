using System.Collections.Generic;
using Xamarin.Forms;

namespace NetduinoDeploy
{
	public partial class App : Application
	{
        static List<string> consoleMessageQueue = new List<string>();
        static bool isUiReady = false;

		public App ()
		{
    		InitializeComponent();
            
			MainPage = new NetduinoDeploy.MainPage();
		}

        public static void SendConsoleMessage(string consoleMessage)
        {
            if (isUiReady)
            {
                if(consoleMessageQueue.Count > 0)
                {
                    foreach (var msg in consoleMessageQueue)
                        MessagingCenter.Send(Current, "Console", msg);

                    consoleMessageQueue.Clear();
                }

                MessagingCenter.Send(Current, "Console", consoleMessage);
            }
            else
            {
                consoleMessageQueue.Add(consoleMessage);
            }
        }

        protected override void OnStart ()
		{
            isUiReady = true;
		}

		protected override void OnSleep ()
		{
            // Handle when your app sleeps
            isUiReady = false;
        }

		protected override void OnResume ()
		{
            // Handle when your app resumes
            isUiReady = true;
        }
	}
}
