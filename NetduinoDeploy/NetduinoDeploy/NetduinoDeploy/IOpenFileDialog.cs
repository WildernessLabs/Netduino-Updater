namespace NetduinoDeploy
{
    public interface IOpenFileDialog
    {
        string[] FileNames { get; }

        bool ShowDialog(string filter);
    }
}