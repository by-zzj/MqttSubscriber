namespace MqttSubscriber.Config
{
    public class MySqlConfig
    {
        public string Server { get; set; } = "localhost";
        public int Port { get; set; } = 3306;
        public string Database { get; set; } = "mqttpublisher";
        public string UserId { get; set; } = "root";
        public string Password { get; set; } = "root";
        public int ConnectionTimeout { get; set; } = 30;

        public string ConnectionString =>
            $"Server={Server};Port={Port};Database={Database};Uid={UserId};Pwd={Password};Connection Timeout={ConnectionTimeout};";
    }
}