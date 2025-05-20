using Microsoft.Extensions.Configuration;

namespace CommonHelper
{
    public class ConfigSetting : IConfigSetting
    {
        private readonly IConfigurationRoot _root;
        public ConfigSetting()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            configurationBuilder.AddJsonFile(path, false, true);
            _root = configurationBuilder.Build();
        }

        public string GetAppSettings(string key, string section)
        {
            var appSetting = _root.GetSection(section);
            return appSetting[key];
        }
        public string GetAppSettings(string key)
        {
            return _root[key];
        }
    }
}
