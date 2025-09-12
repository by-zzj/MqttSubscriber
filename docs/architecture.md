# MQTT订阅者系统架构

## 系统架构图

```mermaid
graph TB
    subgraph "MQTT Broker"
        MB[MQTT Broker<br/>localhost:1883]
    end
    
    subgraph "MQTT Subscriber Application"
        subgraph "Main Program"
            P[Program.cs<br/>主程序入口]
        end
        
        subgraph "Services Layer"
            MS[MqttService<br/>MQTT连接管理]
            DS[DatabaseService<br/>数据库操作]
        end
        
        subgraph "Configuration Layer"
            CL[ConfigLoader<br/>配置加载器]
            MC[MqttConfig<br/>MQTT配置]
            MyC[MySqlConfig<br/>数据库配置]
        end
        
        subgraph "Models"
            MM[MqttMessage<br/>MQTT消息模型]
            RSD[ReceivedSensorData<br/>传感器数据模型]
        end
        
        subgraph "Utilities"
            L[Logger<br/>日志工具]
        end
    end
    
    subgraph "Database"
        DB[(MySQL Database<br/>mqttpublisher)]
        TBL[ReceivedSensorData Table]
    end
    
    subgraph "External Publishers"
        EP[MQTT Publishers<br/>传感器数据发布者]
    end
    
    %% 连接关系
    EP -->|发布消息| MB
    MB -->|订阅消息| MS
    MS -->|消息事件| P
    P -->|保存数据| DS
    DS -->|插入数据| DB
    DB --> TBL
    
    %% 配置关系
    CL --> MC
    CL --> MyC
    MC --> MS
    MyC --> DS
    
    %% 模型关系
    MS --> MM
    P --> RSD
    DS --> RSD
    
    %% 日志关系
    P --> L
    MS --> L
    DS --> L
```

## 组件说明

### 1. 主程序 (Program.cs)
- **职责**: 应用程序入口点，协调各个组件
- **功能**:
  - 加载配置文件
  - 初始化MQTT和数据库服务
  - 处理消息接收事件
  - 管理应用程序生命周期

### 2. MQTT服务 (MqttService)
- **职责**: 管理MQTT连接和消息订阅
- **功能**:
  - 连接到MQTT代理
  - 订阅指定主题
  - 处理接收到的消息
  - 管理连接状态

### 3. 数据库服务 (DatabaseService)
- **职责**: 处理数据库操作
- **功能**:
  - 保存传感器数据到MySQL数据库
  - 管理数据库连接
  - 处理数据插入异常

### 4. 配置管理
- **MqttConfig**: MQTT连接配置
- **MySqlConfig**: MySQL数据库配置
- **ConfigLoader**: 配置文件加载器

### 5. 数据模型
- **MqttMessage**: MQTT消息结构
- **ReceivedSensorData**: 接收到的传感器数据结构

### 6. 工具类
- **Logger**: 控制台日志输出工具

## 数据流

1. **消息接收**: MQTT发布者发送传感器数据到MQTT代理
2. **消息订阅**: MqttService订阅指定主题并接收消息
3. **消息处理**: Program.cs处理接收到的消息，解析JSON数据
4. **数据保存**: DatabaseService将解析后的数据保存到MySQL数据库
5. **状态反馈**: 通过Logger输出处理状态和结果
