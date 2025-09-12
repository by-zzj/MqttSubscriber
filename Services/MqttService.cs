using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MqttPublisher.Models;
using MqttSubscriber.Config;
using MqttSubscriber.Models;
using MqttSubscriber.Utilities;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MqttSubscriber.Services
{
    public class MqttService : IMqttService, IDisposable
    {
        private readonly MqttConfig _config;
        private readonly IMqttClient _mqttClient;
        private bool _isConnected = false;
        private bool _disposed = false;

        public bool IsConnected => _isConnected;

        public event EventHandler<MqttMessage> MessageReceived;
        public event EventHandler<bool> ConnectionStatusChanged;

        public MqttService(MqttConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // 设置消息接收处理
            _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceived;
        }

        private Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var message = new MqttMessage
            {
                Topic = e.ApplicationMessage.Topic,
                Payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload),
                Timestamp = DateTime.Now
            };

            MessageReceived?.Invoke(this, message);
            return Task.CompletedTask;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_config.Server, _config.Port)
                    .WithCredentials(_config.Username, _config.Password)
                    .WithClientId(_config.ClientId)
                    .WithCleanSession()
                    .Build();

                await _mqttClient.ConnectAsync(options);
                _isConnected = true;

                ConnectionStatusChanged?.Invoke(this, true);
                Logger.LogSuccess("Connected to MQTT broker successfully.");

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Connection failed: {ex.Message}");
                ConnectionStatusChanged?.Invoke(this, false);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_isConnected)
            {
                await _mqttClient.DisconnectAsync();
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
                Logger.Log("Disconnected from MQTT broker.");
            }
        }

        public async Task<bool> SubscribeAsync(string topic)
        {
            if (!_isConnected)
            {
                Logger.LogError("Cannot subscribe: Not connected to broker.");
                return false;
            }

            try
            {
                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.SubscribeAsync(topicFilter);
                Logger.Log($"Subscribed to topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to subscribe to topic {topic}: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _mqttClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}