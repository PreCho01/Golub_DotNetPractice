namespace CommonHelper
{
    public interface IConfigSetting
    {
        string GetAppSettings(string key);
        string GetAppSettings(string key, string section);
    }
}
