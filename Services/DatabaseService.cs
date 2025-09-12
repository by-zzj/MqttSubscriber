using MqttSubscriber.Config;
using MqttSubscriber.Models;
using MqttSubscriber.Utilities;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace MqttSubscriber.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly MySqlConfig _config;

        public DatabaseService(MySqlConfig config)
        {
            _config = config;
        }

        public async Task<bool> SaveSensorDataAsync(ReceivedSensorData data)
        {
            try
            {
                // 每次操作创建新连接
                using var connection = new MySqlConnection(_config.ConnectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO ReceivedSensorData 
                                (OriginalId, SensorId, Value, Timestamp, Status, ReceivedAt, ProcessStatus) 
                                VALUES (@OriginalId, @SensorId, @Value, @Timestamp, @Status, @ReceivedAt, @ProcessStatus)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@OriginalId", data.OriginalId);
                cmd.Parameters.AddWithValue("@SensorId", data.SensorId);
                cmd.Parameters.AddWithValue("@Value", data.Value);
                cmd.Parameters.AddWithValue("@Timestamp", data.Timestamp);
                cmd.Parameters.AddWithValue("@Status", data.Status);
                cmd.Parameters.AddWithValue("@ReceivedAt", data.ReceivedAt);
                cmd.Parameters.AddWithValue("@ProcessStatus", data.ProcessStatus);

                await cmd.ExecuteNonQueryAsync();
                Logger.Log($"Saved sensor data for {data.SensorId} to database.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to save sensor data: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            // 不再需要关闭连接
        }
    }
}