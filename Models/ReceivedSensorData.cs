using System;

namespace MqttSubscriber.Models
{
    public class ReceivedSensorData
    {
        public int OriginalId { get; set; }
        public string SensorId { get; set; }
        public decimal Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string ProcessStatus { get; set; } = "pending";
    }
}