using Navigators;
using Localizers;
using MQTTClients;
using Boards;
using Schedulers;
using Cell = JsonClasses.Cell;
using Location = JsonClasses.Location;
using LocalizingMethod = JsonClasses.LocalizingMethod;

namespace GuidanceSystems
{
    public enum Mode
    {
        Direct,
        Idle,
        Delivery
    }

    public class GuidanceSystem
    {
        private readonly Navigator navigator;
        private readonly Scheduler scheduler;
        private readonly Localizer localizer;
        public Mode mode;
        public static string? beaconScannerReading;
        public static bool isArrivedAtDeliveryPoint;
        public static bool isArrivedAtPickUpPoint;
        public static bool isPickUp, isDeliver;

        public GuidanceSystem()
        {
            mode = Mode.Idle;

            navigator = new Navigator();
            scheduler = new Scheduler();
            localizer = new Localizer(LocalizingMethod.Camera);
            beaconScannerReading = "";

            isArrivedAtDeliveryPoint = false;
            isArrivedAtPickUpPoint = false;
            isPickUp = false;
            Console.WriteLine("Guidance System Init Done");
        }

        private void PickUp(Location pickUpPoint)
        {
            var pickUpCell = new Cell
            {
                x = pickUpPoint.x,
                y = pickUpPoint.y
            };
            var pickupPath = navigator.PlanPath("Warehouse-1", Localizer.currentPosition, pickUpCell);
            Console.WriteLine("Guidance system - Planning pickup path is done. Start navigating to pick up point");
            //navigator.Navigate(mode, pickupPath);
        }

        public void Deliver(Cell deliveryPoint)
        {
            //navigator.nav_command = "deliver";
            //navigator.Navigate(mode);
        }

        public void Guide()
        {
            beaconScannerReading = Board.beaconScannerReading;
            if (mode == Mode.Direct)
            {
                if (MQTTClient.controlMessage != "" & MQTTClient.controlMessage != " " &
                    MQTTClient.packageDeliveryMessage != null)
                {
                    //Console.WriteLine("Operating in direct mode");
                    navigator.navCommand = MQTTClient.controlMessage;
                    navigator.Navigate(mode, null);
                    MQTTClient.controlMessage = "";
                }
            }
            else if (mode == Mode.Delivery)
            {
                if (MQTTClient.isNewPackageDeliveryRequestReceived)
                {
                    Console.WriteLine(
                        "Guidance system - New package delivery request received. Switching to delivery mode");
                    scheduler.Schedule(SchedulerState.NewDeliveryRequest);
                    MQTTClient.isNewPackageDeliveryRequestReceived = false;
                }
                else
                {
                    //Console.WriteLine("Guidance system - Operating in delivery mode");
                }

                if (isPickUp)
                {
                    PickUp(scheduler.currentDeliveryItem.location);
                    isArrivedAtPickUpPoint = false;
                    isPickUp = false;
                }

                navigator.Navigate(mode, null);

                scheduler.Schedule(SchedulerState.KeepDelivering);
            }
            else if (mode == Mode.Idle)
            {
                if (MQTTClient.isNewPackageDeliveryRequestReceived)
                {
                    Console.WriteLine(
                        "Guidance system - New package delivery request received. Switching to delivery mode");
                    scheduler.Schedule(SchedulerState.NewDeliveryRequest);
                    mode = Mode.Delivery;
                    MQTTClient.isNewPackageDeliveryRequestReceived = false;
                }
                else
                {
                    //Console.WriteLine("Guidance system - Operating in idle mode");
                }
            }
            else Console.WriteLine("Guidance system - No mode provided");
            // Update current position
            localizer.Positioning();
        }
    }
}