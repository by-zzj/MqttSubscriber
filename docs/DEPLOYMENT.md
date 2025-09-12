# 部署指南

## 概述

本指南详细说明了如何在不同环境中部署MQTT订阅者系统，包括开发环境、测试环境和生产环境的配置。

## 环境要求

### 系统要求

- **操作系统**: Windows 10/11, Linux (Ubuntu 18.04+), macOS 10.15+
- **.NET Runtime**: .NET 6.0 或更高版本
- **内存**: 最少 512MB RAM
- **磁盘空间**: 最少 100MB 可用空间

### 依赖服务

- **MQTT代理服务器**: Eclipse Mosquitto, EMQX, 或其他MQTT 3.1.1兼容的代理
- **MySQL数据库**: MySQL 8.0 或更高版本

## 开发环境部署

### 1. 安装开发工具

#### Windows
```powershell
# 安装 .NET 6.0 SDK
winget install Microsoft.DotNet.SDK.6

# 验证安装
dotnet --version
```

#### Linux (Ubuntu)
```bash
# 添加 Microsoft 包存储库
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

# 安装 .NET 6.0 SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-6.0

# 验证安装
dotnet --version
```

#### macOS
```bash
# 使用 Homebrew 安装
brew install --cask dotnet

# 验证安装
dotnet --version
```

### 2. 安装MQTT代理

#### 使用Docker (推荐)
```bash
# 拉取并运行 Mosquitto
docker run -it -p 1883:1883 -p 9001:9001 eclipse-mosquitto
```

#### Windows 手动安装
1. 下载 [Eclipse Mosquitto](https://mosquitto.org/download/)
2. 安装并启动服务
3. 配置用户认证（可选）

#### Linux 手动安装
```bash
# Ubuntu/Debian
sudo apt-get install mosquitto mosquitto-clients

# 启动服务
sudo systemctl start mosquitto
sudo systemctl enable mosquitto
```

### 3. 安装MySQL数据库

#### 使用Docker (推荐)
```bash
# 拉取并运行 MySQL
docker run --name mysql-mqtt -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=mqttpublisher -p 3306:3306 -d mysql:8.0
```

#### Windows 手动安装
1. 下载 [MySQL Community Server](https://dev.mysql.com/downloads/mysql/)
2. 安装并配置root密码
3. 创建数据库和用户

#### Linux 手动安装
```bash
# Ubuntu/Debian
sudo apt-get install mysql-server

# 启动服务
sudo systemctl start mysql
sudo systemctl enable mysql

# 安全配置
sudo mysql_secure_installation
```

### 4. 配置数据库

```sql
-- 连接到MySQL
mysql -u root -p

-- 创建数据库
CREATE DATABASE mqttpublisher;
USE mqttpublisher;

-- 创建表
CREATE TABLE ReceivedSensorData (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    OriginalId INT NOT NULL,
    SensorId VARCHAR(100) NOT NULL,
    Value DECIMAL(10,2) NOT NULL,
    Timestamp DATETIME NOT NULL,
    Status VARCHAR(50),
    ReceivedAt DATETIME NOT NULL,
    ProcessStatus VARCHAR(50) DEFAULT 'pending',
    INDEX idx_sensor_id (SensorId),
    INDEX idx_timestamp (Timestamp),
    INDEX idx_received_at (ReceivedAt)
);

-- 创建专用用户（可选）
CREATE USER 'mqttuser'@'localhost' IDENTIFIED BY 'mqttpassword';
GRANT ALL PRIVILEGES ON mqttpublisher.* TO 'mqttuser'@'localhost';
FLUSH PRIVILEGES;
```

### 5. 配置应用程序

编辑 `appsettings.json`:

```json
{
  "MqttConfig": {
    "Server": "localhost",
    "Port": 1883,
    "Username": "",
    "Password": "",
    "ClientId": "DatabaseSubscriber_Dev",
    "SubscribeTopics": [ "sensors/data" ]
  },
  "MySqlConfig": {
    "Server": "localhost",
    "Port": 3306,
    "Database": "mqttpublisher",
    "UserId": "root",
    "Password": "root",
    "ConnectionTimeout": 30
  }
}
```

### 6. 运行应用程序

```bash
# 克隆项目
git clone <repository-url>
cd MqttSubscriber

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行应用程序
dotnet run
```

## 测试环境部署

### 1. 使用Docker Compose

创建 `docker-compose.yml`:

```yaml
version: '3.8'

services:
  mysql:
    image: mysql:8.0
    container_name: mqtt-mysql
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: mqttpublisher
      MYSQL_USER: mqttuser
      MYSQL_PASSWORD: mqttpassword
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    networks:
      - mqtt-network

  mosquitto:
    image: eclipse-mosquitto:2.0
    container_name: mqtt-broker
    ports:
      - "1883:1883"
      - "9001:9001"
    volumes:
      - ./mosquitto.conf:/mosquitto/config/mosquitto.conf
    networks:
      - mqtt-network

  mqtt-subscriber:
    build: .
    container_name: mqtt-subscriber
    depends_on:
      - mysql
      - mosquitto
    environment:
      - MqttConfig__Server=mosquitto
      - MqttConfig__Port=1883
      - MySqlConfig__Server=mysql
      - MySqlConfig__Port=3306
      - MySqlConfig__Database=mqttpublisher
      - MySqlConfig__UserId=mqttuser
      - MySqlConfig__Password=mqttpassword
    networks:
      - mqtt-network

volumes:
  mysql_data:

networks:
  mqtt-network:
    driver: bridge
```

创建 `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MqttSubscriber.csproj", "."]
RUN dotnet restore "./MqttSubscriber.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "MqttSubscriber.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MqttSubscriber.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MqttSubscriber.dll"]
```

创建 `init.sql`:

```sql
USE mqttpublisher;

CREATE TABLE IF NOT EXISTS ReceivedSensorData (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    OriginalId INT NOT NULL,
    SensorId VARCHAR(100) NOT NULL,
    Value DECIMAL(10,2) NOT NULL,
    Timestamp DATETIME NOT NULL,
    Status VARCHAR(50),
    ReceivedAt DATETIME NOT NULL,
    ProcessStatus VARCHAR(50) DEFAULT 'pending',
    INDEX idx_sensor_id (SensorId),
    INDEX idx_timestamp (Timestamp),
    INDEX idx_received_at (ReceivedAt)
);
```

创建 `mosquitto.conf`:

```conf
listener 1883
allow_anonymous true

listener 9001
protocol websockets
allow_anonymous true
```

### 2. 启动测试环境

```bash
# 启动所有服务
docker-compose up -d

# 查看日志
docker-compose logs -f mqtt-subscriber

# 停止服务
docker-compose down
```

## 生产环境部署

### 1. 安全配置

#### MQTT代理安全配置

创建 `mosquitto.conf`:

```conf
# 基本配置
listener 1883
max_connections 1000
max_inflight_messages 100
max_queued_messages 1000

# 认证配置
allow_anonymous false
password_file /etc/mosquitto/passwd
acl_file /etc/mosquitto/acl

# SSL/TLS配置
listener 8883
cafile /etc/mosquitto/ca.crt
certfile /etc/mosquitto/server.crt
keyfile /etc/mosquitto/server.key
require_certificate false

# 日志配置
log_dest file /var/log/mosquitto/mosquitto.log
log_type error
log_type warning
log_type notice
log_type information
```

创建用户密码文件:

```bash
# 安装 mosquitto_passwd 工具
sudo apt-get install mosquitto-clients

# 创建密码文件
sudo mosquitto_passwd -c /etc/mosquitto/passwd mqttuser
```

创建ACL文件 `/etc/mosquitto/acl`:

```
# 允许用户订阅传感器数据主题
user mqttuser
topic read sensors/+
topic write sensors/+

# 允许系统用户管理
user admin
topic readwrite #
```

#### MySQL安全配置

```sql
-- 创建专用数据库用户
CREATE USER 'mqttuser'@'%' IDENTIFIED BY 'StrongPassword123!';
GRANT SELECT, INSERT, UPDATE, DELETE ON mqttpublisher.* TO 'mqttuser'@'%';

-- 限制连接数
ALTER USER 'mqttuser'@'%' WITH MAX_CONNECTIONS_PER_HOUR 1000;

-- 刷新权限
FLUSH PRIVILEGES;
```

### 2. 应用程序配置

生产环境 `appsettings.Production.json`:

```json
{
  "MqttConfig": {
    "Server": "mqtt.yourdomain.com",
    "Port": 8883,
    "Username": "mqttuser",
    "Password": "StrongPassword123!",
    "ClientId": "DatabaseSubscriber_Prod",
    "SubscribeTopics": [ 
      "sensors/temperature",
      "sensors/humidity",
      "sensors/pressure"
    ]
  },
  "MySqlConfig": {
    "Server": "mysql.yourdomain.com",
    "Port": 3306,
    "Database": "mqttpublisher",
    "UserId": "mqttuser",
    "Password": "StrongPassword123!",
    "ConnectionTimeout": 30
  }
}
```

### 3. 使用Systemd服务 (Linux)

创建服务文件 `/etc/systemd/system/mqtt-subscriber.service`:

```ini
[Unit]
Description=MQTT Subscriber Service
After=network.target mysql.service

[Service]
Type=simple
User=mqttuser
WorkingDirectory=/opt/mqtt-subscriber
ExecStart=/usr/bin/dotnet /opt/mqtt-subscriber/MqttSubscriber.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# 安全设置
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/mqtt-subscriber/logs

[Install]
WantedBy=multi-user.target
```

启用并启动服务:

```bash
# 创建用户
sudo useradd -r -s /bin/false mqttuser

# 创建应用目录
sudo mkdir -p /opt/mqtt-subscriber
sudo mkdir -p /opt/mqtt-subscriber/logs

# 复制应用程序文件
sudo cp -r /path/to/published/app/* /opt/mqtt-subscriber/
sudo chown -R mqttuser:mqttuser /opt/mqtt-subscriber

# 启用并启动服务
sudo systemctl enable mqtt-subscriber
sudo systemctl start mqtt-subscriber

# 查看状态
sudo systemctl status mqtt-subscriber
```

### 4. 使用Windows服务

创建 `install-service.ps1`:

```powershell
# 以管理员身份运行
$serviceName = "MQTTSubscriber"
$serviceDisplayName = "MQTT Subscriber Service"
$serviceDescription = "MQTT消息订阅者服务"
$servicePath = "C:\Services\MqttSubscriber\MqttSubscriber.exe"

# 创建服务
New-Service -Name $serviceName -BinaryPathName $servicePath -DisplayName $serviceDisplayName -Description $serviceDescription -StartupType Automatic

# 启动服务
Start-Service -Name $serviceName
```

### 5. 监控和日志

#### 日志配置

创建 `NLog.config`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

  <targets>
    <target xsi:type="File" name="fileTarget"
            fileName="logs/mqtt-subscriber-${shortdate}.log"
            layout="${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}" />
    
    <target xsi:type="Console" name="consoleTarget"
            layout="${time} ${level:uppercase=true} ${message}" />
  </targets>

  <rules>
    <logger name="*" minlevel="Info" writeTo="fileTarget" />
    <logger name="*" minlevel="Error" writeTo="consoleTarget" />
  </rules>
</nlog>
```

#### 健康检查

创建健康检查端点:

```csharp
// 在Program.cs中添加
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    MqttConnected = _mqttService.IsConnected 
});
```

## 性能优化

### 1. 数据库优化

```sql
-- 创建索引
CREATE INDEX idx_sensor_timestamp ON ReceivedSensorData(SensorId, Timestamp);
CREATE INDEX idx_process_status ON ReceivedSensorData(ProcessStatus);

-- 分区表（大量数据时）
ALTER TABLE ReceivedSensorData 
PARTITION BY RANGE (YEAR(Timestamp)) (
    PARTITION p2024 VALUES LESS THAN (2025),
    PARTITION p2025 VALUES LESS THAN (2026),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);
```

### 2. 连接池配置

```json
{
  "MySqlConfig": {
    "ConnectionString": "Server=localhost;Database=mqttpublisher;Uid=root;Pwd=root;Connection Timeout=30;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;"
  }
}
```

### 3. 内存优化

```csharp
// 在Program.cs中配置GC
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
```

## 故障排除

### 常见问题

1. **MQTT连接失败**
   - 检查网络连接
   - 验证MQTT代理配置
   - 检查防火墙设置

2. **数据库连接失败**
   - 验证数据库服务状态
   - 检查连接字符串
   - 确认用户权限

3. **消息解析失败**
   - 检查JSON格式
   - 验证数据模型匹配
   - 查看详细错误日志

### 日志分析

```bash
# 查看实时日志
tail -f /var/log/mqtt-subscriber.log

# 搜索错误日志
grep "ERROR" /var/log/mqtt-subscriber.log

# 统计消息处理数量
grep "Sensor data saved" /var/log/mqtt-subscriber.log | wc -l
```

## 备份和恢复

### 数据库备份

```bash
# 创建备份脚本
#!/bin/bash
BACKUP_DIR="/backup/mysql"
DATE=$(date +%Y%m%d_%H%M%S)
mysqldump -u root -p mqttpublisher > $BACKUP_DIR/mqttpublisher_$DATE.sql

# 设置定时任务
0 2 * * * /path/to/backup_script.sh
```

### 配置备份

```bash
# 备份配置文件
cp appsettings.json appsettings.json.backup
cp mosquitto.conf mosquitto.conf.backup
```

## 更新和维护

### 应用程序更新

```bash
# 停止服务
sudo systemctl stop mqtt-subscriber

# 备份当前版本
cp -r /opt/mqtt-subscriber /opt/mqtt-subscriber.backup

# 部署新版本
cp -r /path/to/new/version/* /opt/mqtt-subscriber/

# 启动服务
sudo systemctl start mqtt-subscriber
```

### 定期维护任务

```bash
# 清理旧日志文件
find /var/log -name "*.log" -mtime +30 -delete

# 数据库维护
mysql -u root -p -e "OPTIMIZE TABLE mqttpublisher.ReceivedSensorData;"

# 检查磁盘空间
df -h
```
