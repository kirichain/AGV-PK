using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace MQTTClients
{   
    public class Location
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    // Class for storing AGV status, serialized to JSON when sending to server
    public class AgvStatus
    {
        public string? id { get; set; }
        public long timestamp { get; set; }
        public string? workingMap { get; set; }
        public Location? location { get; set; }
        public Dictionary<string,string>? hardwareStatus { get; set; }
    }
    
    public static class MQTTClient
    {
        public static string? mqttBrokerUrl;
        public static bool isConnected;
        public static string? agvId, controlMessage, statusMessage, packageDeliveryMessage, packageLocationMessage;
        static MqttFactory mqttFactory;
        public static bool isNewPackageDeliveryRequestReceived;
        public static AgvStatus? agvStatusObj;
        public static void Init(string _mqttBrokerUrl)
        {
            mqttBrokerUrl = _mqttBrokerUrl;
            mqttFactory = new MqttFactory();
            Console.WriteLine("MQTT Client Starting");
            controlMessage = "";
            agvStatusObj = new AgvStatus
            {
                id = "",
                timestamp = 0,
                workingMap = "",
                location = new Location
                {
                    x = 0,
                    y = 0
                },
                hardwareStatus = new Dictionary<string, string>()
            };
            
            agvStatusObj.hardwareStatus.Add("motor-controller", "not connected");
            agvStatusObj.hardwareStatus.Add("beacon-scanner", "not connected");
            agvStatusObj.hardwareStatus.Add("bottom-camera", "not connected");
            agvStatusObj.hardwareStatus.Add("front-camera", "not connected");
            agvStatusObj.hardwareStatus.Add("lidar", "not connected");
            agvStatusObj.hardwareStatus.Add("rfid-reader", "not connected");
            agvStatusObj.hardwareStatus.Add("power-supply-manager", "not connected");
        }
        
        public static async Task Publish_Message(string topic, string? message)
        {
            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttBrokerUrl)
                    .Build();

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                
                //Message null means we are sending a status message
                if (message == null)
                {
                    //Console.WriteLine("Sending status message");
                    message = JsonSerializer.Serialize(agvStatusObj);
                }
                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .Build();

                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

                await mqttClient.DisconnectAsync();

                //Console.WriteLine("Message was published.");
            }
        }

        public static async Task Subscribe_Handle()
        {
            if (!isConnected)
            {
                var mqttClient = mqttFactory.CreateMqttClient();

                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(mqttBrokerUrl).Build();

                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    switch (e.ApplicationMessage.Topic)
                    {
                        case "agv/status":
                            break;
                        case "agv/control/001":
                            controlMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                            if (controlMessage != "" & controlMessage != " ")
                            {
                                Console.WriteLine("MQTT Client - Control message: " + controlMessage);
                            }
                            break;
                        case "agv/package/delivery":
                            packageDeliveryMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                            if (packageDeliveryMessage != "" & packageDeliveryMessage != " ")
                            {
                                isNewPackageDeliveryRequestReceived = true;
                                Console.WriteLine("MQTT Client - Package delivery message: " + packageDeliveryMessage);
                            }
                            break;
                        case "agv/pacakge/location":
                            packageLocationMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                            Console.WriteLine(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                            break;
                    }
                
                    //Console.WriteLine(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                    //Console.WriteLine("Message processing done");
                    return Task.CompletedTask;
                };

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(
                        f => { f.WithTopic("agv/status"); })
                    .WithTopicFilter(
                        f => { f.WithTopic("agv/control/001"); })
                    .WithTopicFilter(
                        f => { f.WithTopic("agv/package/delivery"); })
                    .WithTopicFilter(
                        f => { f.WithTopic("agv/pacakge/location"); })
                    .Build();

                await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
                Console.WriteLine("MQTT client subscribed to topics.");
                isConnected = true;
                return;
            }
        }
    }
}