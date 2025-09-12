namespace MqttSubscriber.Config
{
    public class MqttConfig
    {
        public string Server { get; set; } = "localhost";
        public int Port { get; set; } = 1883;
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = "admin";
        public string ClientId { get; set; } = "DatabaseSubscriber";
        public string[] SubscribeTopics { get; set; } = new[] { "sensors/data" };
    }
}