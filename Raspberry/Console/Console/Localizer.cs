using Cell = JsonClasses.Cell;
using LocalizingMethod = JsonClasses.LocalizingMethod;

namespace Localizers
{
    public class Localizer
    {
        public static string? beaconScannerData;
        public static string[] beacon, beaconData;
        public static string[] beaconName;

        public static double[] beaconRssi;

        //NE/NW/SE/SW stand for northeast, southeast, northwest, southwest
        public static string enBeaconName, esBeaconName, wnBeaconName, wsBeaconName;

        public static double neDistance,
            seDistance,
            nwDistance,
            swDistance,
            neBeaconRssi,
            seBeaconRssi,
            nwBeaconRssi,
            swBeaconRssi;

        public static bool isLocationCenter, isLocationInside, isCheckLocationDone;
        public static string? workingMap;
        public static Cell currentPosition;

        public Localizer(LocalizingMethod method)
        {
            currentPosition = new Cell();
            currentPosition.x = 0;
            currentPosition.y = 0;
            beaconName = new string[6];
            beaconRssi = new double[6];
            currentPosition = new Cell
            {
                x = 0,
                y = 0
            };
            if (method == LocalizingMethod.Camera)
            {
                Console.WriteLine("Localizer using camera init done");
            }
        }

        // public static void GetClosestDirection()
        // {
        // }
        //
        // public static int GetRssiRange(double rssi)
        // {
        //     int range;
        //
        //     switch ((int)rssi)
        //     {
        //         case int n when n >= -90 && n < -80:
        //             range = 9;
        //             break;
        //         case int n when n >= -80 && n < -70:
        //             range = 8;
        //             break;
        //         case int n when n >= -70 && n < -60:
        //             range = 7;
        //             break;
        //         case int n when n >= -60 && n < -50:
        //             range = 6;
        //             break;
        //         case int n when n >= -50 && n <= -40:
        //             range = 5;
        //             break;
        //         case int n when n >= -40 && n <= -30:
        //             range = 4;
        //             break;
        //         case int n when n >= -30 && n <= 0:
        //             range = 3;
        //             break;
        //         default:
        //             range = -1; // Out of range
        //             break;
        //     }
        //
        //     return range;
        // }
        //
        // public static bool IsLocationInside()
        // {
        //     if ((neBeaconRssi < 0) || (nwBeaconRssi < 0) || (swBeaconRssi < 0) || (seBeaconRssi < 0))
        //     {
        //         if ((neBeaconRssi >= -60) && (nwBeaconRssi >= -60) && (swBeaconRssi >= -60) && (seBeaconRssi >= -60))
        //         {
        //             return true;
        //         }
        //     }
        //
        //     return false;
        // }
        //
        // public static bool IsLocationCenter()
        // {
        //     if (GetRssiRange(nwBeaconRssi) <= 6)
        //     {
        //         return true;
        //     }
        //
        //     return false;
        // }
        //
        // public static double CalculateDistanceFromRssi(int rssi)
        // {
        //     // Constants used to calculate distance
        //     const int txPower = -59; // RSSI at 1 meter
        //     const double nValue = 3.0; // Path loss exponent
        //
        //     // Calculate distance using the log-normal shadowing model
        //     double ratioDb = txPower - rssi;
        //     double ratioLinear = Math.Pow(10, ratioDb / 10);
        //     double distance = Math.Pow(ratioLinear, 1 / nValue);
        //
        //     // Convert distance to centimeters
        //     distance *= 100;
        //
        //     return distance;
        // }
        //
        // public static void ScanBeacon()
        // {
        //     swBeaconRssi = 0;
        //     seBeaconRssi = 0;
        //     neBeaconRssi = 0;
        //     nwBeaconRssi = 0;
        //
        //     int i = 0;
        //     if ((GuidanceSystem.beaconScannerReading != "") && (GuidanceSystem.beaconScannerReading != null))
        //     {
        //         beaconScannerData = GuidanceSystem.beaconScannerReading.Trim('&', '*');
        //         beacon = beaconScannerData.Split('#');
        //         foreach (var b in beacon)
        //         {
        //             beaconData = b.Split('=');
        //             beaconName[i] = beaconData[0].Trim();
        //             beaconRssi[i] = Int32.Parse(beaconData[1]);
        //             Console.WriteLine("Name = " + beaconName[i] + " - RSSI = " + beaconRssi[i]);
        //             //Console.WriteLine($"{b}");
        //             i++;
        //         }
        //         //Console.WriteLine("Beacon Scanner Result = " + beaconScannerData);
        //         //Console.WriteLine("Filtered beacon scanner result. Start to build realtime map now");
        //     }
        // }
        //
        // public static bool IsBeaconExisting(string _beaconName, Direction direction)
        // {
        //     Console.WriteLine("Checking existing " + _beaconName);
        //     if (beaconName.Contains(_beaconName))
        //     {
        //         //Console.WriteLine("Contained");
        //         int index = Array.IndexOf(beaconName, _beaconName);
        //         if (index != -1)
        //         {
        //             if (beaconRssi[index] != 0)
        //             {
        //                 switch (direction)
        //                 {
        //                     case Direction.Southeast:
        //                         seBeaconRssi = beaconRssi[index];
        //                         break;
        //                     case Direction.Southwest:
        //                         swBeaconRssi = beaconRssi[index];
        //                         break;
        //                     case Direction.Northeast:
        //                         neBeaconRssi = beaconRssi[index];
        //                         break;
        //                     case Direction.Northwest:
        //                         nwBeaconRssi = beaconRssi[index];
        //                         break;
        //                 }
        //
        //                 //CalculateDistance(beaconRssi[index]);
        //                 return true;
        //             }
        //
        //             return false;
        //         }
        //     }
        //     else
        //     {
        //         Console.WriteLine("Not contained");
        //     }
        //
        //     return false;
        // }
        //
        // public static void FindNearbyBeacon()
        // {
        //     isLocationInside = false;
        //     //Check western south cell
        //     if ((Mapper.baseLayer[currentX - 1, currentY + 1] != null) &&
        //         (Mapper.baseLayer[currentX - 1, currentY + 1] != ""))
        //     {
        //         if (Mapper.baseLayer[currentX - 1, currentY + 1] == "*")
        //         {
        //             //Console.WriteLine("Checking " + Mapper.beaconIdLayer[currentX - 1, currentY + 1]);
        //             if (IsBeaconExisting(Mapper.beaconIdLayer[currentX - 1, currentY + 1], Direction.Southwest))
        //             {
        //                 Console.WriteLine("Southwest Beacon Found " + Mapper.beaconIdLayer[currentX - 1, currentY + 1]);
        //             }
        //         }
        //     }
        //
        //     //Check western north cell
        //     if ((Mapper.baseLayer[currentX - 1, currentY - 1] != null) &&
        //         (Mapper.baseLayer[currentX - 1, currentY - 1] != ""))
        //     {
        //         if (Mapper.baseLayer[currentX - 1, currentY - 1] == "*")
        //         {
        //             //Console.WriteLine("Checking " + Mapper.beaconIdLayer[currentX - 1, currentY - 1]);
        //             if (IsBeaconExisting(Mapper.beaconIdLayer[currentX - 1, currentY - 1], Direction.Northwest))
        //             {
        //                 Console.WriteLine("Northwest Beacon Found " + Mapper.beaconIdLayer[currentX - 1, currentY - 1]);
        //             }
        //         }
        //     }
        //
        //     //Check estern north cell
        //     if ((Mapper.baseLayer[currentX + 1, currentY - 1] != null) &&
        //         (Mapper.baseLayer[currentX + 1, currentY - 1] != ""))
        //     {
        //         if (Mapper.baseLayer[currentX + 1, currentY - 1] == "*")
        //         {
        //             //Console.WriteLine("Checking " + Mapper.beaconIdLayer[currentX + 1, currentY - 1]);
        //             if (IsBeaconExisting(Mapper.beaconIdLayer[currentX + 1, currentY - 1], Direction.Northeast))
        //             {
        //                 Console.WriteLine("Northeast Beacon Found " + Mapper.beaconIdLayer[currentX + 1, currentY - 1]);
        //             }
        //         }
        //     }
        //
        //     //Check eastern south cell
        //     if ((Mapper.baseLayer[currentX + 1, currentY + 1] != null) &&
        //         (Mapper.baseLayer[currentX + 1, currentY + 1] != ""))
        //     {
        //         if (Mapper.baseLayer[currentX + 1, currentY + 1] == "*")
        //         {
        //             //Console.WriteLine("Checking " + Mapper.beaconIdLayer[currentX + 1, currentY + 1]);
        //             if (IsBeaconExisting(Mapper.beaconIdLayer[currentX + 1, currentY + 1], Direction.Southeast))
        //             {
        //                 Console.WriteLine("Southeast Beacon Found " + Mapper.beaconIdLayer[currentX + 1, currentY + 1]);
        //             }
        //         }
        //     }
        // }
        //
        // public static void CheckLocation()
        // {
        //     Console.WriteLine("Range = " + GetRssiRange(nwBeaconRssi));
        //     //Console.WriteLine("Distance = " + CalculateDistanceFromRssi((int)nwBeaconRssi));
        //     Console.WriteLine("Checking if AGV location is valid");
        //     if (IsLocationInside())
        //     {
        //         Console.WriteLine("AGV is inside the cell");
        //         if (IsLocationCenter())
        //         {
        //             Console.WriteLine("AGV is center the cell");
        //         }
        //     }
        // }

        private void UpdatePosition(int x, int y)
        {
            currentPosition.x = x;
            currentPosition.y = y;
        }
        public void Positioning()
        {
            UpdatePosition(1, 1);
        }
    }
}