using System.Text;
using System.Text.Json;
using MQTTClients;
using GuidanceSystems;
using Items = JsonClasses.Items;
using Destinations = JsonClasses.Destinations;
using DeliveryPath = JsonClasses.DeliveryPath;
using DeliveryRequest = JsonClasses.DeliveryRequest;

namespace Schedulers
{
    public enum BusyLevel
    {
        Free,
        Low,
        Full
    }

    public enum SchedulerState
    {
        NewDeliveryRequest,
        KeepDelivering,
    }
    
    public class Scheduler
    {
        private BusyLevel busyLevel;
        private Queue<string> deliveryRequests = new Queue<string>();
        private Queue<string> waitingDeliveryRequests = new Queue<string>();
        private Queue<Destinations> deliveryPoints = new Queue<Destinations>();
        private Queue<Items> deliveryItems = new Queue<Items>();
        public Queue<DeliveryPath> deliveryPaths = new Queue<DeliveryPath>();
        public Destinations? currentDeliveryPoint;
        public Items? currentDeliveryItem;
        private DeliveryPath? currentDeliveryPath;
        private Dictionary<string, int> deliveriedItemCount = new Dictionary<string, int>();
        private int deliveriedPointCount;
        private bool isInDeliveryRequestProcess;
        private bool isInPickupProcess;

        private bool isInDeliveryProcess;
//         public string json = @"{""timestamp"":123,""items"":[{""id"":""xyz123"",""location"":{""x"":0,""y"":0},
// ""quantity"":4},{""id"":""abc456"",""location"":{""x"":0,""y"":1},""quantity"":6}],
// ""destinations"":[{""x"":1,""y"":2},{""x"":2,""y"":3},{""x"":3,""y"":3}]}";

        public Scheduler()
        {
            isInDeliveryRequestProcess = false;
            isInDeliveryProcess = false;
            isInPickupProcess = false;
            busyLevel = BusyLevel.Free;
            Console.WriteLine("Scheduler Init Done");
        }

        private void DeserializeDeliveryRequest(string request)
        {
            // Check if request is UTF-8 encoded, if not, convert it
            if (!Encoding.UTF8.GetChars(Encoding.UTF8.GetBytes(request)).Equals(request))
            {
                request = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(request));
            }

            DeliveryRequest? deliveryRequest = JsonSerializer.Deserialize<DeliveryRequest>(request);

            Console.WriteLine($"Delivery request timestamp: {deliveryRequest?.timestamp}");
            Console.WriteLine("Delivery request items:");
            if (deliveryRequest?.items != null)
                foreach (var item in deliveryRequest.items)
                {
                    // Add item to deliveryItems queue
                    deliveryItems.Enqueue(item);
                    // Add item id and quantity to dictionary
                    deliveriedItemCount.Add(item.id, item.quantity);
                    Console.WriteLine($"Item ID: {item.id}");
                    if (item.location != null)
                        Console.WriteLine($"Item location: ({item.location.x}, {item.location.y})");
                    Console.WriteLine($"Item quantity: {item.quantity}");
                }

            Console.WriteLine("Delivery request destinations:");
            if (deliveryRequest?.destinations != null)
                foreach (var destination in deliveryRequest.destinations)
                {
                    // Add destination to deliveryPoints queue
                    deliveryPoints.Enqueue(destination);
                    Console.WriteLine($"Destination: ({destination.x}, {destination.y})");
                }
        }

        private void AddDeliveryRequest(string request)
        {
            Console.WriteLine("Adding delivery request to queue: " + request);
            Console.WriteLine("Queue count: " + deliveryRequests.Count);
            Console.WriteLine("Waiting queue count: " + waitingDeliveryRequests.Count);

            if (deliveryRequests.Count < 8 & (busyLevel == BusyLevel.Free | busyLevel == BusyLevel.Low))
            {
                deliveryRequests.Enqueue(request);
                busyLevel = BusyLevel.Low;
            }
            else
            {
                waitingDeliveryRequests.Enqueue(request);
                busyLevel = BusyLevel.Full;
            }
        }

        private void ScheduleDeliveryPath(Items item, Destinations destination)
        {
            Console.WriteLine("Send pickup point and delivery point to guidance system to move");
            GuidanceSystem.isPickUp = true;
        }

        private void ProceedDeliveryPoint()
        {
            if (isInDeliveryProcess)
            {
                if (!GuidanceSystem.isArrivedAtDeliveryPoint)
                {
                    //Console.WriteLine("Guidance system has not arrived at delivery point yet");
                }
                else
                {
                    Console.WriteLine("Guidance system has arrived at delivery point");
                    GuidanceSystem.isArrivedAtDeliveryPoint = false;
                    // Set current delivery point and delivery item to null
                    currentDeliveryPoint = null;
                    currentDeliveryItem = null;
                }
            }
        }

        public void Schedule(SchedulerState state)
        {
            if (state == SchedulerState.NewDeliveryRequest)
            {
                if (busyLevel == BusyLevel.Free)
                {
                    Console.WriteLine("Scheduler is free. Request will be added to queue");
                }
                else if (busyLevel == BusyLevel.Low)
                {
                    Console.WriteLine("Scheduler is low. Request will be added to queue");
                }
                else if (busyLevel == BusyLevel.Full)
                {
                    Console.WriteLine("Scheduler is full. Request will be added to waiting list");
                }

                AddDeliveryRequest(MQTTClient.packageDeliveryMessage);

                MQTTClient.packageDeliveryMessage = "";
            }
            else if (state == SchedulerState.KeepDelivering)
            {
                // Check if there is a delivery request process going on
                if (isInDeliveryRequestProcess)
                {
                    // Check if delivery points queue or delivery items queue is empty, if yes, end delivery request process
                    if (currentDeliveryPoint == null & currentDeliveryItem == null)
                    {
                        if (deliveryPoints.Count == 0 & deliveryItems.Count == 0)
                        {
                            isInDeliveryRequestProcess = false;
                            isInDeliveryProcess = false;
                            deliveriedPointCount = 0;
                            Console.WriteLine("Scheduler has finished delivery process. Starting the next one");
                        }
                        else
                        {
                            // Take the first delivery point and item from the queue
                            if (deliveryPoints.Count > 0)
                            {
                                currentDeliveryPoint = deliveryPoints.Dequeue();
                            }

                            if (deliveryItems.Count > 0)
                            {
                                currentDeliveryItem = deliveryItems.Dequeue();
                            }

                            // Start scheduling path
                            ScheduleDeliveryPath(currentDeliveryItem, currentDeliveryPoint);
                            isInDeliveryProcess = true;
                        }
                    }
                    else
                    {
                        // If not, keep delivering
                        //Console.WriteLine("Scheduler still be in delivery process");
                        ProceedDeliveryPoint();
                    }
                }
                else
                {
                    // If not, check if there is a request in the queue
                    if (deliveryRequests.Count > 0)
                    {
                        // If yes, start a new delivery process
                        isInDeliveryRequestProcess = true;
                        // Deserialize delivery request
                        Console.WriteLine("Scheduler is starting a new delivery process");
                        DeserializeDeliveryRequest(deliveryRequests.Dequeue());
                    }
                    else
                    {
                        // If not, check if there is a request in the waiting list
                        if (waitingDeliveryRequests.Count > 0)
                        {
                            // If yes, start a new delivery process
                            isInDeliveryRequestProcess = true;
                            // Add request to queue
                            deliveryRequests.Enqueue(waitingDeliveryRequests.Dequeue());
                            // Deserialize delivery request
                            DeserializeDeliveryRequest(deliveryRequests.Dequeue());
                            Console.WriteLine("Scheduler is starting a new delivery process from waiting list");
                        }
                        else
                        {
                            // If not, do nothing
                            //Console.WriteLine("Waiting list is empty. Scheduler is fully free");
                        }
                    }
                }
            }
        }
    }
}