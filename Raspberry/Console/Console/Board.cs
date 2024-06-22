using System;
using System.IO.Ports;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Boards
{
    public enum BoardName
    {
        Motor_Controller,
        Beacon_Scanner,
        Laser_Collector,
        RFID_Scanner
    }
    /// <summary>
    /// 
    /// </summary>
    public class Board
    {
        private static SerialPort motorControllerPort,
            beaconScannerPort,
            laserCollectorPort,
            RFIDScannerPort,
            serialPort;

        public static bool isMotorControllerPortReady, isBeaconScannerPortReady;

        public bool isPortReady, isReading, isDisconnected;

        //LF character used for determining if data from serial port reading contains break line character 
        private static char LF = (char)10;
        private bool isNewMotorControllerReading, isNewBeaconReading, isCheckingName, isNewReading;
        public string portName, serialReading, buffer;
        public BoardName boardName;
        private int waitingCount;
        private static bool isSerialReady;
        public static string beaconScannerReading;

        public Board()
        {
        }

        public void Init(string _portName, BoardName _boardName)
        {
            portName = _portName;
            boardName = _boardName;

            if (boardName == BoardName.Motor_Controller)
            {
                isMotorControllerPortReady = false;
                isNewMotorControllerReading = false;
            }

            if (boardName == BoardName.Beacon_Scanner)
            {
                isBeaconScannerPortReady = false;
                isNewBeaconReading = false;
            }

            isPortReady = false;
            isReading = false;
            isDisconnected = false;
            serialReading = "";
            isNewReading = false;

            Console.WriteLine("Checking board");

            if (boardName == BoardName.Motor_Controller)
            {
                motorControllerPort = new SerialPort(portName, 115200);
                motorControllerPort.RtsEnable = true;
                motorControllerPort.DtrEnable = true;
                motorControllerPort.Parity = Parity.None;
                motorControllerPort.StopBits = StopBits.One;
                motorControllerPort.DataBits = 8;
                motorControllerPort.Handshake = Handshake.None;

                motorControllerPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                if (!motorControllerPort.IsOpen)
                {
                    Console.WriteLine("Opening port");
                    motorControllerPort.Open();
                }

                while (!motorControllerPort.IsOpen)
                {
                    //Console.Write(".");
                }

                Console.WriteLine("Port " + portName + " opened successfully");
                while (!isMotorControllerPortReady)
                {
                    //Console.Write(".");
                }

                Console.WriteLine("Motor controller serial port is ready");
                checkBoardName(BoardName.Motor_Controller);
                //Console.WriteLine("Board Init Done");
            }

            if (boardName == BoardName.Beacon_Scanner)
            {
                beaconScannerPort = new SerialPort(portName, 115200);
                beaconScannerPort.RtsEnable = true;
                beaconScannerPort.DtrEnable = true;
                beaconScannerPort.Parity = Parity.None;
                beaconScannerPort.StopBits = StopBits.One;
                beaconScannerPort.DataBits = 8;
                beaconScannerPort.Handshake = Handshake.None;

                beaconScannerPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                if (!beaconScannerPort.IsOpen)
                {
                    Console.WriteLine("Opening port");
                    beaconScannerPort.Open();
                }

                //while (!beaconScannerPort.IsOpen)
                //{
                //    //Console.Write(".");
                //}

                Console.WriteLine("Port " + portName + " opened successfully");
                //while (!isBeaconScannerPortReady)
                //{
                //    Console.WriteLine(".");
                //}
                //checkBoardName(BoardName.Beacon);
            }
        }

        public void checkBoardName(BoardName receiver)
        {
            Console.WriteLine("Checking board name");
            if (receiver == BoardName.Motor_Controller)
            {
                motorControllerPort.WriteLine("checkName");
            }

            if (receiver == BoardName.Beacon_Scanner)
            {
                beaconScannerPort.WriteLine("checkName");
            }

            if (receiver == BoardName.Laser_Collector)
            {
                laserCollectorPort.WriteLine("checkName");
            }

            if (receiver == BoardName.RFID_Scanner)
            {
                RFIDScannerPort.WriteLine("checkName");
            }
        }

        public static void SendSerial(BoardName receiver, string data)
        {
            if (receiver == BoardName.Motor_Controller)
            {
                if (isMotorControllerPortReady)
                {
                    motorControllerPort.WriteLine(data);
                }
                else
                {
                    Console.WriteLine("Port is not opened");
                }
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            foreach (char c in indata)
            {
                if (isNewReading)
                {
                    if (c == '*')
                    {
                        serialReading = buffer.Trim();
                        if ((serialReading != "") && (serialReading != " "))
                        {
                            if (serialReading == "Ready")
                            {
                                isMotorControllerPortReady = true;
                            }

                            if (boardName == BoardName.Beacon_Scanner)
                            {
                                beaconScannerReading = serialReading;
                            }

                            //if (boardName != BoardName.Beacon_Scanner) {
                            //    Console.WriteLine(serialReading);
                            //}
                            Console.WriteLine(serialReading);
                        }

                        buffer = "";
                        isNewReading = false;
                        break;
                    }
                    else if ((c != '&') && (c != LF))
                    {
                        buffer += c;
                    }
                }
                else if (c == '&')
                {
                    isNewReading = true;
                }
            }
            //Console.WriteLine("Data Received:");
        }

        private void BoardNameDataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            string __boardName;
            foreach (char c in indata)
            {
                if (c != LF)
                {
                    buffer += c;
                }
                else if (c == LF)
                {
                    __boardName = buffer;
                    buffer = "";
                    __boardName = __boardName.Trim();
                    if (__boardName == "motor-controller")
                    {
                        Console.WriteLine();
                        Console.WriteLine("Board name: " + __boardName);
                        isCheckingName = false;
                        return;
                    }
                }
            }
        }

        private static void ConfirmDataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            string buff = "";
            Console.WriteLine(indata);
            foreach (char c in indata)
            {
                if (!isSerialReady)
                {
                    if (c == '*')
                    {
                        buff = buff.Trim();
                        if (buff == "Ready")
                        {
                            isSerialReady = true;
                            Console.WriteLine("Serial is ready");
                            return;
                        }
                    }
                    else if (c == '&')
                    {
                        buff = "";
                    }
                    else if (c != LF)
                    {
                        buff += c;
                    }
                }
            }
        }
    }
}