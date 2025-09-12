using MqttPublisher.Models;
using MqttSubscriber.Models;
using System;
using System.Threading.Tasks;

namespace MqttSubscriber.Services
{
    public interface IMqttService : IDisposable
    {
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        Task<bool> SubscribeAsync(string topic);
        bool IsConnected { get; }
        event EventHandler<MqttMessage> MessageReceived;
        event EventHandler<bool> ConnectionStatusChanged;
    }
}