using MQTTnet;
using MQTTnet.Client;
using System.Text;

namespace IPCs
{
    public enum IpcType
    {
        MessageQueue,
        Mqtt,
        WebSocket
    }

    public class Ipc
    {
        private string? mqttBrokerUrl;
        public bool isConnected;
        public string? dataMessage, controlMessage;
        static MqttFactory? mqttFactory;

        public void Init(IpcType ipcType, string _mqttBrokerUrl)
        {
            if (ipcType == IpcType.Mqtt)
            {
                InitMQTT(_mqttBrokerUrl);
            }
        }

        private void InitMQTT(string _mqttBrokerUrl)
        {
            mqttBrokerUrl = _mqttBrokerUrl;
            mqttFactory = new MqttFactory();
            Console.WriteLine("MQTT Starting");
            controlMessage = "";
        }

        public async Task Publish_Message(string topic, string message)
        {
            using var mqttClient = mqttFactory?.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttBrokerUrl)
                .Build();

            await mqttClient?.ConnectAsync(mqttClientOptions, CancellationToken.None);

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .Build();

            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            await mqttClient.DisconnectAsync();

            Console.WriteLine("Message was published.");
        }

        public async Task Subscribe_Handle()
        {
            if (!isConnected)
            {
                var mqttClient = mqttFactory?.CreateMqttClient();

                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(mqttBrokerUrl).Build();

                if (mqttClient != null)
                {
                    mqttClient.ApplicationMessageReceivedAsync += e =>
                    {
                        switch (e.ApplicationMessage.Topic)
                        {
                            case "ipc/camera/data":
                                dataMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                                Console.Write("Received IPC data message: ");
                                Console.WriteLine(dataMessage);
                                break;
                            case "ipc/camera/command":
                                controlMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                                Console.Write("Received IPC control message: ");
                                Console.WriteLine(controlMessage);
                                break;
                        }
                        
                        return Task.CompletedTask;
                    };

                    await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                    var mqttSubscribeOptions = mqttFactory?.CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(
                            f => { f.WithTopic("ipc/camera/command"); })
                        .WithTopicFilter(
                            f => { f.WithTopic("ipc/camera/data"); })
                        .Build();

                    await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
                }

                Console.WriteLine("MQTT client for IPC subscribed to topics.");
                isConnected = true;
                return;
            }
        }
    }
}