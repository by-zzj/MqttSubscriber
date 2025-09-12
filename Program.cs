using MqttPublisher.Models;
using MqttSubscriber.Config;
using MqttSubscriber.Models;
using MqttSubscriber.Services;
using MqttSubscriber.Utilities;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MqttSubscriber
{
    class Program
    {
        private static IMqttService _mqttService;
        private static DatabaseService _dbService;
        private static MqttConfig _mqttConfig;
        private static MySqlConfig _mySqlConfig;
        private static bool _isRunning = true;

        static async Task Main(string[] args)
        {
            Logger.Log("MQTT Database Subscriber Starting...");

            try
            {
                // 加载配置
                _mqttConfig = ConfigLoader.LoadMqttConfig();
                _mySqlConfig = ConfigLoader.LoadMySqlConfig();

                Logger.LogSuccess("Configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load configuration: {ex.Message}");
                Logger.LogWarning("Using default configuration as fallback.");
                _mqttConfig = new MqttConfig();
                _mySqlConfig = new MySqlConfig();
            }

            // 初始化数据库服务（不再需要连接）
            _dbService = new DatabaseService(_mySqlConfig);

            // 初始化MQTT服务
            _mqttService = new MqttService(_mqttConfig);

            // 注册消息接收事件
            _mqttService.MessageReceived += OnMessageReceived;
            _mqttService.ConnectionStatusChanged += OnConnectionStatusChanged;

            // 设置Ctrl+C事件处理
            Console.CancelKeyPress += OnCancelKeyPress;

            // 连接到MQTT代理
            Logger.Log("Connecting to MQTT broker...");
            var mqttConnected = await _mqttService.ConnectAsync();

            if (!mqttConnected)
            {
                Logger.LogError("Failed to connect to MQTT broker. Exiting...");
                return;
            }

            // 订阅主题
            Logger.Log($"Subscribing to topics: {string.Join(", ", _mqttConfig.SubscribeTopics)}");
            foreach (var topic in _mqttConfig.SubscribeTopics)
            {
                var subscribed = await _mqttService.SubscribeAsync(topic);
                if (!subscribed)
                {
                    Logger.LogError($"Failed to subscribe to topic: {topic}");
                }
            }

            Logger.Log("MQTT Database Subscriber started. Press Ctrl+C to exit.");

            // 保持程序运行
            while (_isRunning)
            {
                await Task.Delay(1000);
            }
        }

        private static async void OnMessageReceived(object sender, MqttMessage message)
        {
            try
            {
                Logger.Log($"Received message on topic: {message.Topic}");
                Logger.Log($"Payload: {message.Payload}");

                // 解析JSON数据
                var sensorData = JsonSerializer.Deserialize<ReceivedSensorData>(message.Payload);

                if (sensorData != null)
                {
                    // 设置接收时间
                    sensorData.ReceivedAt = DateTime.Now;
                    sensorData.ProcessStatus = "pending";

                    Logger.Log($"Parsed sensor data: SensorId={sensorData.SensorId}, Value={sensorData.Value}");

                    // 保存到数据库
                    var saved = await _dbService.SaveSensorDataAsync(sensorData);
                    if (saved)
                    {
                        Logger.LogSuccess($"Sensor data saved to database. OriginalId={sensorData.OriginalId}");
                    }
                    else
                    {
                        Logger.LogError($"Failed to save sensor data to database. OriginalId={sensorData.OriginalId}");
                    }
                }
                else
                {
                    Logger.LogError("Failed to parse sensor data from message payload.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error processing message: {ex.Message}");
            }
        }

        private static void OnConnectionStatusChanged(object sender, bool isConnected)
        {
            if (isConnected)
            {
                Logger.LogSuccess("Connected to MQTT broker.");
            }
            else
            {
                Logger.LogWarning("Disconnected from MQTT broker.");
            }
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _isRunning = false;
            Logger.Log("Stopping MQTT Subscriber...");

            // 断开MQTT连接
            _mqttService?.DisconnectAsync().GetAwaiter().GetResult();

            // 释放资源
            _mqttService?.Dispose();
            _dbService?.Dispose();

            Logger.Log("MQTT Subscriber Stopped.");
            Environment.Exit(0);
        }
    }
}