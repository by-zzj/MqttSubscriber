# API文档

## 概述

本文档描述了MQTT订阅者系统中各个组件的API接口和用法。

## 核心服务接口

### IMqttService 接口

MQTT服务的核心接口，定义了MQTT连接和消息处理的基本操作。

```csharp
public interface IMqttService : IDisposable
{
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> SubscribeAsync(string topic);
    bool IsConnected { get; }
    event EventHandler<MqttMessage> MessageReceived;
    event EventHandler<bool> ConnectionStatusChanged;
}
```

#### 方法说明

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `ConnectAsync()` | `Task<bool>` | 异步连接到MQTT代理服务器 |
| `DisconnectAsync()` | `Task` | 异步断开MQTT连接 |
| `SubscribeAsync(string topic)` | `Task<bool>` | 异步订阅指定主题 |
| `IsConnected` | `bool` | 获取当前连接状态 |

#### 事件说明

| 事件 | 类型 | 说明 |
|------|------|------|
| `MessageReceived` | `EventHandler<MqttMessage>` | 接收到MQTT消息时触发 |
| `ConnectionStatusChanged` | `EventHandler<bool>` | 连接状态改变时触发 |

#### 使用示例

```csharp
// 创建MQTT服务实例
var mqttService = new MqttService(mqttConfig);

// 注册事件处理器
mqttService.MessageReceived += OnMessageReceived;
mqttService.ConnectionStatusChanged += OnConnectionStatusChanged;

// 连接到MQTT代理
bool connected = await mqttService.ConnectAsync();

if (connected)
{
    // 订阅主题
    await mqttService.SubscribeAsync("sensors/data");
}
```

### MqttService 实现类

IMqttService接口的具体实现，使用MQTTnet库提供MQTT功能。

#### 构造函数

```csharp
public MqttService(MqttConfig config)
```

**参数:**
- `config` (MqttConfig): MQTT配置对象

**异常:**
- `ArgumentNullException`: 当config参数为null时抛出

#### 主要方法实现

##### ConnectAsync()

```csharp
public async Task<bool> ConnectAsync()
```

连接到MQTT代理服务器。

**返回值:**
- `Task<bool>`: 连接成功返回true，失败返回false

**异常处理:**
- 捕获所有异常并记录错误日志
- 连接失败时触发ConnectionStatusChanged事件

##### SubscribeAsync(string topic)

```csharp
public async Task<bool> SubscribeAsync(string topic)
```

订阅指定的MQTT主题。

**参数:**
- `topic` (string): 要订阅的主题名称

**返回值:**
- `Task<bool>`: 订阅成功返回true，失败返回false

**前置条件:**
- 必须已连接到MQTT代理

**异常处理:**
- 未连接时返回false并记录错误
- 订阅失败时记录错误日志

## 数据库服务

### DatabaseService 类

处理MySQL数据库操作的服务类。

```csharp
public class DatabaseService : IDisposable
{
    public DatabaseService(MySqlConfig config);
    public async Task<bool> SaveSensorDataAsync(ReceivedSensorData data);
    public void Dispose();
}
```

#### 构造函数

```csharp
public DatabaseService(MySqlConfig config)
```

**参数:**
- `config` (MySqlConfig): MySQL数据库配置对象

#### 主要方法

##### SaveSensorDataAsync(ReceivedSensorData data)

```csharp
public async Task<bool> SaveSensorDataAsync(ReceivedSensorData data)
```

将传感器数据保存到MySQL数据库。

**参数:**
- `data` (ReceivedSensorData): 要保存的传感器数据对象

**返回值:**
- `Task<bool>`: 保存成功返回true，失败返回false

**数据库操作:**
```sql
INSERT INTO ReceivedSensorData 
(OriginalId, SensorId, Value, Timestamp, Status, ReceivedAt, ProcessStatus) 
VALUES (@OriginalId, @SensorId, @Value, @Timestamp, @Status, @ReceivedAt, @ProcessStatus)
```

**异常处理:**
- 捕获所有异常并记录错误日志
- 数据库连接失败时返回false

## 配置管理

### ConfigLoader 静态类

提供配置文件的加载功能。

```csharp
public static class ConfigLoader
{
    public static MqttConfig LoadMqttConfig();
    public static MySqlConfig LoadMySqlConfig();
}
```

#### 方法说明

##### LoadMqttConfig()

```csharp
public static MqttConfig LoadMqttConfig()
```

从appsettings.json文件加载MQTT配置。

**返回值:**
- `MqttConfig`: 加载的MQTT配置对象，失败时返回默认配置

**异常处理:**
- 捕获所有异常并返回默认配置对象

##### LoadMySqlConfig()

```csharp
public static MySqlConfig LoadMySqlConfig()
```

从appsettings.json文件加载MySQL配置。

**返回值:**
- `MySqlConfig`: 加载的MySQL配置对象，失败时返回默认配置

**异常处理:**
- 捕获所有异常并返回默认配置对象

## 数据模型

### MqttMessage 类

表示接收到的MQTT消息。

```csharp
public class MqttMessage
{
    public string Topic { get; set; }
    public string Payload { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
```

**属性说明:**
- `Topic` (string): MQTT主题
- `Payload` (string): 消息内容
- `Timestamp` (DateTime): 接收时间戳

### ReceivedSensorData 类

表示接收到的传感器数据。

```csharp
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
```

**属性说明:**
- `OriginalId` (int): 原始消息ID
- `SensorId` (string): 传感器ID
- `Value` (decimal): 传感器数值
- `Timestamp` (DateTime): 数据时间戳
- `Status` (string): 状态信息
- `ReceivedAt` (DateTime): 接收时间
- `ProcessStatus` (string): 处理状态，默认为"pending"

## 配置类

### MqttConfig 类

MQTT连接配置。

```csharp
public class MqttConfig
{
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public string ClientId { get; set; } = "DatabaseSubscriber";
    public string[] SubscribeTopics { get; set; } = new[] { "sensors/data" };
}
```

### MySqlConfig 类

MySQL数据库配置。

```csharp
public class MySqlConfig
{
    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "mqttpublisher";
    public string UserId { get; set; } = "root";
    public string Password { get; set; } = "root";
    public int ConnectionTimeout { get; set; } = 30;
    
    public string ConnectionString { get; }
}
```

**特殊属性:**
- `ConnectionString` (string): 自动生成的MySQL连接字符串

## 工具类

### Logger 静态类

提供控制台日志输出功能。

```csharp
public static class Logger
{
    public static void Log(string message);
    public static void LogError(string message);
    public static void LogSuccess(string message);
    public static void LogWarning(string message);
}
```

#### 方法说明

| 方法 | 说明 | 输出颜色 |
|------|------|----------|
| `Log(string message)` | 输出一般信息日志 | 白色 |
| `LogError(string message)` | 输出错误日志 | 红色 |
| `LogSuccess(string message)` | 输出成功日志 | 绿色 |
| `LogWarning(string message)` | 输出警告日志 | 黄色 |

**日志格式:**
```
[yyyy-MM-dd HH:mm:ss] 级别: 消息内容
```

## 错误处理策略

### 异常处理原则

1. **配置加载异常**: 使用默认配置继续运行
2. **MQTT连接异常**: 记录错误并退出程序
3. **消息解析异常**: 记录错误并继续处理下一条消息
4. **数据库操作异常**: 记录错误并继续运行

### 错误日志示例

```
[2024-01-01 10:00:00] ERROR: Failed to connect to MQTT broker: Connection refused
[2024-01-01 10:00:01] WARN: Using default configuration as fallback
[2024-01-01 10:00:02] SUCCESS: Connected to MQTT broker successfully
[2024-01-01 10:00:03] ERROR: Failed to parse sensor data from message payload
```

## 性能考虑

### 连接管理

- 使用连接池管理数据库连接
- 每次数据库操作创建新连接，操作完成后自动释放
- MQTT连接保持长连接，避免频繁重连

### 内存管理

- 实现IDisposable接口，确保资源正确释放
- 使用using语句管理数据库连接
- 及时释放不再使用的对象引用

### 异步操作

- 所有I/O操作都使用异步方法
- 避免阻塞主线程
- 使用ConfigureAwait(false)避免死锁
