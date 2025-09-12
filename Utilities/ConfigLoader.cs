using Microsoft.Extensions.Configuration;
using MqttSubscriber.Config;
using System.IO;

namespace MqttSubscriber.Utilities
{
    public static class ConfigLoader
    {
        public static MqttConfig LoadMqttConfig()
        {
            try
            {
                var configuration = BuildConfiguration();
                return configuration.GetSection("MqttConfig").Get<MqttConfig>();
            }
            catch
            {
                return new MqttConfig();
            }
        }

        public static MySqlConfig LoadMySqlConfig()
        {
            try
            {
                var configuration = BuildConfiguration();
                return configuration.GetSection("MySqlConfig").Get<MySqlConfig>();
            }
            catch
            {
                return new MySqlConfig();
            }
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }
    }
}