using AppKit;

namespace NetduinoDeploy.macOS
{
    static class MainClass
    {
        static void Main(string[] args)
        {
            NSApplication.Init();

            NSApplication.SharedApplication.Delegate = new AppDelegate();

            NSApplication.Main(args);
        }
    }
}
