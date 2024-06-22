using Boards;
//using SocketClients;
using MQTTClients;
using GuidanceSystems;
using APIs;
using Localizers;
using System.IO.Ports;
using IPCs;
using Schedulers;
using PowerSupplies;
using ComputerVisions;

//SocketClient socketClient;
Board beacon_scanner, motor_controller;
GuidanceSystem guider;
API api;
Ipc mqttIpc;
PowerSupply powerSupply;
ComputerVision bottomCamera, frontCamera;

bool systemCheck = true;
bool isDebuggingState = false;
string agvId = "001";
long previousUnixTimestamp = 0;
DateTime previousDateTime = DateTime.Now;
long loopCounter = 0;

//socketClient = new SocketClient();
//motor_controller = new Board("/dev/ttyUSB1");
//beacon_scanner = new Board("COM6", BoardName.Beacon_Scanner);
//motor_controller = new Board("COM3", BoardName.Motor_Controller);
//motor_controller = new Board("/dev/ttyUSB0", BoardName.Motor_Controller);
motor_controller = new Board();
beacon_scanner = new Board();
guider = new GuidanceSystem();
api = new API();
mqttIpc = new Ipc();
powerSupply = new PowerSupply();
bottomCamera = new ComputerVision("Bottom Camera");
frontCamera = new ComputerVision("Front Camera");

//Choose release or debug mode
Console.WriteLine("Press key D/S to select release/debug mode");

if (Console.ReadKey().Key == ConsoleKey.D)
{
    Console.WriteLine("Debugging mode has been chosen");
    isDebuggingState = true;
}
else if (Console.ReadKey().Key == ConsoleKey.S)
{
    Console.WriteLine("Release mode has been chosen");
    isDebuggingState = false;
}

//Give user to choose the port if release mode is chosen
if (!isDebuggingState)
{
    Console.WriteLine("Press key to select port");

    if (Console.ReadKey().Key == ConsoleKey.Enter)
    {
        Console.WriteLine("Windows port");
        //motor_controller.Init("COM3", BoardName.Motor_Controller);
        //beacon_scanner.Init("COM3", BoardName.Beacon_Scanner);
    }
    else if (Console.ReadKey().Key == ConsoleKey.Spacebar)
    {
        Console.WriteLine("Linux port");
        motor_controller.Init("/dev/ttyUSB0", BoardName.Motor_Controller);
        //beacon_scanner.Init("/dev/ttyUSB1", BoardName.Beacon_Scanner);
    }
}
else
{
    Console.WriteLine("Available serial ports: ");
    string[] ports = SerialPort.GetPortNames();
    foreach (var p in ports)
    {
        Console.WriteLine(p);
    }
}

//socketClient.Init();
MQTTClient.agvId = agvId;
MQTTClient.Init("pirover.xyz");
mqttIpc.Init(IpcType.Mqtt, "127.0.0.1");

// Subscribe to MQTT Brokers
MQTTClient.Subscribe_Handle();
mqttIpc.Subscribe_Handle();

// Wait for MQTTClient to connect
while (!MQTTClient.isConnected)
{
    Console.WriteLine("Waiting for MQTT Client to connect");
    Thread.Sleep(1000);
}

// Wait for mqttIpc to connect
while (!mqttIpc.isConnected)
{
    Console.WriteLine("Waiting for MQTT IPC to connect");
    Thread.Sleep(1000);
}

//Check if MQTT Client is connected
if (MQTTClient.isConnected)
{
    Console.WriteLine("MQTT Client connection init done");
}
else
{
    Console.WriteLine("MQTT Client connection init failed");
    systemCheck = false;
}

//Check if mqttIpc is connected
if (mqttIpc.isConnected)
{
    Console.WriteLine("MQTT IPC connection init done");
}
else
{
    Console.WriteLine("MQTT IPC connection init failed");
    systemCheck = false;
}

//System status JSON format
//{"id":"001","timestamp":0,"workingMap":"Warehouse-1","location":{"x":0,"y":0},"hardware-status":
//{"motor-controller":"connected","beacon-scanner":"connected","bottom-camera":"connected","front-camera":"connected"
//"power-supply-manager":"connected","lidar":"connected","rfid-reader":"connected"}}

// systemStatus =
//     @"{""id"":""001"",""timestamp"":0,""workingMap"":""" + Localizer.workingMap + @""",""currentX"":""" +
//     Localizer.currentX.ToString() + @""",""currentY"":""" + Localizer.currentY.ToString() +
//     @""",""motor-controller"":""connected"",""beacon-scanner"":""connected""}";

if (systemCheck)
{
    Console.WriteLine("System check done. Switch to idle mode");
    guider.mode = Mode.Idle;
    //guider.mode = Mode.Direct;
    //guider.mode = Mode.Delivery;
    Console.WriteLine("Mode: " + guider.mode);
    while (true)
    {   
        // Check if counter exceeds max value of long
        if (loopCounter > long.MaxValue - 1)
        {
            loopCounter = 0;
        }
        else
        {
            //Console.WriteLine("Loop counter: " + loopCounter);
            loopCounter++;
        }
        // Get current date time
        var currentDateTime = DateTime.Now;
        // Get current Unix timestamp
        var currentUnixTimestamp = ((DateTimeOffset)currentDateTime).ToUnixTimeSeconds();
        // Check if 1 second has passed
        if (currentUnixTimestamp - previousUnixTimestamp >= 1)
        {
            // Update previousUnixTimestamp
            previousUnixTimestamp = currentUnixTimestamp;
            // Update previousDateTime
            previousDateTime = currentDateTime;
           
            if (MQTTClient.agvStatusObj != null)
            {
                MQTTClient.agvStatusObj.id = agvId;
                MQTTClient.agvStatusObj.timestamp = currentUnixTimestamp;
                MQTTClient.agvStatusObj.workingMap = Localizer.workingMap;
                if (MQTTClient.agvStatusObj.location != null)
                {
                    MQTTClient.agvStatusObj.location.x = Localizer.currentPosition.x;
                    MQTTClient.agvStatusObj.location.y = Localizer.currentPosition.y;
                }

                if (MQTTClient.agvStatusObj.hardwareStatus != null)
                {
                    MQTTClient.agvStatusObj.hardwareStatus["motor-controller"] = "connected";
                    MQTTClient.agvStatusObj.hardwareStatus["beacon-scanner"] = "connected";
                    MQTTClient.agvStatusObj.hardwareStatus["bottom-camera"] = "connected";
                    MQTTClient.agvStatusObj.hardwareStatus["front-camera"] = "connected";
                    MQTTClient.agvStatusObj.hardwareStatus["power-supply-manager"] = "connected";
                    MQTTClient.agvStatusObj.hardwareStatus["lidar"] = "connected";
                    MQTTClient.agvStatusObj.hardwareStatus["rfid-reader"] = "connected";
                }
            }

            MQTTClient.Publish_Message("agv/status", null);
        }
        //Boards.Board.SendSerial(SerialReceiver.Motor_Controller, "forward");
        guider.Guide();
        // Check if user presses ESC without blocking the program
        if (Console.KeyAvailable)
        {
            if (Console.ReadKey(true).Key == ConsoleKey.Escape)
            {
                Console.WriteLine("ESC key pressed. System shutdown");
                // Wait for 2 seconds before shutting down
                Thread.Sleep(2000);
                break;
            }
        }
    }
}
else
{
    Console.WriteLine("System check failed. Press any key to exit");
    Console.ReadKey();
}