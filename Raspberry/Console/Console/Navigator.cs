using GuidanceSystems;
using Boards;
using MQTTClients;
using Localizers;
using Mappers;
using Cell = JsonClasses.Cell;
using CellType = JsonClasses.CellType;
using TurnDirection = JsonClasses.TurnDirection;
using Direction = JsonClasses.Direction;
using DeliveryPath = JsonClasses.DeliveryPath;
using LayerName = JsonClasses.LayerName;
using Map = JsonClasses.Map;

namespace Navigators
{
    public class Navigator
    {
        //private Localizer localizer;
        private readonly Mapper mapper;
        public string? navCommand, navRoute;
        private Cell nextCell;
        private DeliveryPath? deliveryPath;
        private readonly Queue<Cell>? cells = new Queue<Cell>();
        private bool isNavigating;
        public bool isArrivedAtNextCell;
        private Direction headDirection;
        private int debug;
        public Navigator()
        {
            //localizer = new Localizer();
            mapper = new Mapper();
            mapper.CreateMap("Warehouse-1", 4, 4);
            // Print map to console
            mapper.PrintMap("Warehouse-1", LayerName.Base);
            // Update the last cell of the map to be a Lane using UpdateMap method
            var lastCell = new Cell
            {
                x = 0,
                y = 0,
                type = CellType.Lane
            };
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 1;
            lastCell.y = 0;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 1;
            lastCell.y = 1;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 1;
            lastCell.y = 2;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 2;
            lastCell.y = 2;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 3;
            lastCell.y = 2;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            // Path 2
            // lastCell.x = 0;
            // lastCell.y = 1;
            // lastCell.type = (Mappers.CellType)CellType.Lane;
            // mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 0;
            lastCell.y = 2;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 0;
            lastCell.y = 3;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 1;
            lastCell.y = 3;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            lastCell.x = 2;
            lastCell.y = 3;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            // Other path
            lastCell.x = 3;
            lastCell.y = 1;
            lastCell.type = CellType.Lane;
            mapper.UpdateMap("Warehouse-1", LayerName.Base, lastCell);
            // Print updated map to console
            Console.WriteLine("Updated map:");
            mapper.PrintMap("Warehouse-1", LayerName.Base);
            headDirection = Direction.South;
            Console.WriteLine("Navigator Init Done");
        }

        public DeliveryPath PlanPath(string mapName, Cell startingPoint, Cell endPoint)
        {
            // Declare a temporary path with using var keyword
            var _path = new DeliveryPath
            {
                path = new List<Cell>()
            };

            Console.Write("Start planning path with ");
            Console.Write("Starting point: " + startingPoint.x + ";" + startingPoint.y);
            Console.WriteLine(" & End point: " + endPoint.x + ";" + endPoint.y);
            if (mapName == null)
            {
                Console.WriteLine("Navigator - Map name is null");
            }
            else
            {
                Console.WriteLine("Navigator - Map name is " + mapName);
                var map = mapper.LoadMap(mapName);
                if (map != null)
                {
                    Console.WriteLine("Navigator - Map loaded. Starting planning path now");
                    // Implement Dijkstra algorithm to find the shortest path
                    
                    // Create visited array for storing adjacent cells and queue for storing neighbor cells
                    var visited = new bool[map.width, map.height];
                    var neighborQueue = new Queue<Cell>();
                    // Create an array to store weight of each cell/vertex
                    var v = new int[map.width, map.height];
                    // Loop over cells in the map and set weight of each cell to infinity if cell type is Lane
                    for (var i = 0; i < map.height; i++)
                    {
                        for (var j = 0; j < map.width; j++)
                        {
                            if (map.layers[0].cells[((i + 1) * map.width) - (map.width - j)].type ==
                                CellType.Lane)
                            {
                                v[i, j] = int.MaxValue;
                            }
                        }
                    }
                    Console.WriteLine("Vertex weight array:");
                    for (var i = 0; i < map.height; i++)
                    {
                        for (var j = 0; j < map.width; j++)
                        {
                            Console.Write(v[i, j] + " ");
                        }

                        Console.WriteLine();
                    }
                    // Push starting point to the queue and mark it as visited
                    neighborQueue.Enqueue(startingPoint);
                    visited[startingPoint.y, startingPoint.x] = true;
                    // Origin cell has weight of 0
                    v[startingPoint.y, startingPoint.x] = 0;
                    // Loop over the queue until it's empty, print the path and return it
                    
                    while (neighborQueue.Count != 0)
                    {
                        // Dequeue the first cell in the queue
                        var currentCell = neighborQueue.Dequeue();
                        // Get adjacent cells of the current cell
                        var adjacentCells = GetAdjacentCells(map, currentCell);
                        // Loop over adjacent cells
                        foreach (var cell in adjacentCells)
                        {
                            // If the cell is not visited and it's a Lane
                            if (!visited[cell.y, cell.x] &&
                                map.layers[0].cells[((cell.y + 1) * map.width) - (map.width - cell.x)].type ==
                                CellType.Lane)
                            {
                                // Mark the cell as visited
                                visited[cell.y, cell.x] = true;
                                // Push the cell to the queue
                                neighborQueue.Enqueue(cell);
                                // Calculate weight of the cell
                                var weight = v[currentCell.y, currentCell.x] + 1;
                                // If the weight of the cell is smaller than the current weight of the cell
                                if (weight < v[cell.y, cell.x])
                                {
                                    // Update the weight of the cell
                                    v[cell.y, cell.x] = weight;
                                }
                            }
                        }
                    }
                    // Print weight of each cell/vertex to console
                    Console.WriteLine("Vertex weight array after:");
                    for (var i = 0; i < map.height; i++)
                    {
                        for (var j = 0; j < map.width; j++)
                        {
                            Console.Write(v[i, j] + " ");
                        }

                        Console.WriteLine();
                    }
                    // Using recursion to trace back the path from end point to starting point, print to 
                    // console and return it
                    var shortestPath = TraceBackPath(map, v, endPoint, startingPoint);
                    _path.deliveryDistance = 50;
                    _path.deliveryTime = 10;
                    _path.path = shortestPath;
                    Console.WriteLine("Navigator - Done planning path");
                }
                else
                {
                    Console.WriteLine("Navigator - Map load failed");
                }
            }

            /*Localizer.ScanBeacon();
            Localizer.FindNearbyBeacon();
            Check if the AGV is inside and center the cell
            Localizer.CheckLocation();

            If checking step is done, then create a new path or continue the planned one
             if (Localizer.isCheckLocationDone)
             {
                 Console.WriteLine("AGV is center the cell. Start navigating now");
             }
             else
             {
                 if (Localizer.isLocationInside)
                 {
                     Console.WriteLine("AGV is not in center of the cell. Start calibrating now");
                 }
                 else
                 {
                     Console.WriteLine("Location check done but AGV position is not correct. End navigating");
                 }
             }*/
            return _path;
        }

        private static List<Cell> TraceBackPath(Map map, int[,] ints, Cell endPoint, Cell startingPoint)
        {
            // Create a list to store the path
            var path = new List<Cell> {
                // Add the end point to the path
                endPoint };
            // Create a cell to store the current cell
            var currentCell = endPoint;
            // Loop over the map until the current cell is the starting point
            while (currentCell.x != startingPoint.x || currentCell.y != startingPoint.y)
            {
                // Get adjacent cells of the current cell
                var adjacentCells = GetAdjacentCells(map, currentCell);
                // Loop over adjacent cells
                foreach (var cell in adjacentCells)
                {
                    // Skip if cell type is not Lane
                    if (map.layers[0].cells[((cell.y + 1) * map.width) - (map.width - cell.x)].type !=
                        CellType.Lane) continue;
                    // If the weight of the cell is smaller than the current cell
                    if (ints[cell.y, cell.x] >= ints[currentCell.y, currentCell.x]) continue;
                    // Update the current cell
                    currentCell = cell;
                    // Add the cell to the path
                    path.Add(cell);
                    // Break the loop
                    break;
                }
            }
            
            // Reverse the path
            path.Reverse();
            // Remove the starting point from the path
            path.RemoveAt(0);
            // Print the path to console
            Console.WriteLine("Shortest path found");
            foreach (var cell in path)
            {
                Console.Write(cell.x + ";" + cell.y + " > ");
            }
            Console.WriteLine();
            // Return the path
            return path;
        }

        private static IEnumerable<Cell> GetAdjacentCells(Map map, Cell currentCell)
        {
            var adjacentCells = new List<Cell>();
            // Check if the current cell is not on the left edge of the map
            if (currentCell.x > 0)
            {
                // Get the cell on the left of the current cell
                var leftCell = new Cell
                {
                    x = currentCell.x - 1,
                    y = currentCell.y
                };
                // Add the cell to the list
                adjacentCells.Add(leftCell);
            }
            // Check if the current cell is not on the right edge of the map
            if (currentCell.x < map.width - 1)
            {
                // Get the cell on the right of the current cell
                var rightCell = new Cell
                {
                    x = currentCell.x + 1,
                    y = currentCell.y
                };
                // Add the cell to the list
                adjacentCells.Add(rightCell);
            }
            // Check if the current cell is not on the top edge of the map
            if (currentCell.y > 0)
            {
                // Get the cell on the top of the current cell
                var topCell = new Cell
                {
                    x = currentCell.x,
                    y = currentCell.y - 1
                };
                // Add the cell to the list
                adjacentCells.Add(topCell);
            }
            // Check if the current cell is not on the bottom edge of the map
            if (currentCell.y < map.height - 1)
            {
                // Get the cell on the bottom of the current cell
                var bottomCell = new Cell
                {
                    x = currentCell.x,
                    y = currentCell.y + 1
                };
                // Add the cell to the list
                adjacentCells.Add(bottomCell);
            }

            return adjacentCells;
        }
        
        private Direction GetDirection(Cell target)
        {
            var direction = Direction.None;
            if (target.x > Localizer.currentPosition.x)
            {
                direction = Direction.East;
            }
            else if (target.x < Localizer.currentPosition.x)
            {
                direction = Direction.West;
            }
            else if (target.y < Localizer.currentPosition.y)
            {
                direction = Direction.North;
            }
            else if (target.y > Localizer.currentPosition.y)
            {
                direction = Direction.South;
            }

            return direction;
        }
        
        public void DetectObstacle()
        {
        }

        public void DetectHuman()
        {
        }
        
        private void Turn(TurnDirection direction)
        {
            Console.WriteLine("Turning robot by direction " + direction + " completes.");
            switch (direction)
            {
                case TurnDirection.Rear:
                    Console.WriteLine("TurnDirection.Rear");
                    break;
                case TurnDirection.Right:
                    Console.WriteLine("TurnDirection.Right");
                    Move("turn-right");
                    break;
                case TurnDirection.Left:
                    Console.WriteLine("TurnDirection.Left");
                    Move("turn-left");
                    break;
            }
        }
        
        private void Move(string direction)
        {
            navCommand = direction;
            Board.SendSerial(BoardName.Motor_Controller, navCommand);
        }

        public void Navigate(Mode mode, DeliveryPath? path)
        {
            if (mode == Mode.Direct)
            {
                Console.WriteLine("Navigating in direct mode");
                Move(MQTTClient.controlMessage);
            }

            else if (mode == Mode.Delivery)
            {
                //Console.WriteLine("Navigating in delivery mode");
                if (path != null)
                {
                    if (!isNavigating)
                    {
                        Console.WriteLine("Navigator - Path is not null. Not in navigating. Start navigating now");
                        isNavigating = true;
                        isArrivedAtNextCell = false;
                        deliveryPath = path;
                        // Take path from deliveryPath and put it into cells
                        if (deliveryPath.path != null)
                            foreach (var cell in deliveryPath.path)
                            {
                                cells.Enqueue(cell);
                            }
                        // Get the next cell
                        nextCell = cells.Dequeue();
                        // Print next cell to console
                        Console.WriteLine($"Navigator - Next cell is {nextCell.x};{nextCell.y}");
                    }
                    else
                    {
                        Console.WriteLine("Navigator - Path is not null but AGV is navigating. Please try later");
                    }
                }
                else
                {
                    //Console.WriteLine("Navigator - Path is null. Keep navigating if possible");
                    if (isNavigating)
                    {
                        Console.WriteLine("Navigator - In navigating");
                        if (Localizer.currentPosition != nextCell)
                        {
                            Console.WriteLine("Navigator - In navigating to next point");
                            var direction = GetDirection(nextCell);
                            if (debug == 1)
                            {
                                
                            }
                            else
                            {
                                Console.WriteLine($"Navigator - Next cell direction is {direction}");
                                debug = 1;
                            }
                            // Compare direction with headDirection, if equal then move forward, else turn
                            if (direction == headDirection)
                            {
                                //Console.WriteLine("Navigator - Next cell direction is equal to head direction");
                                //Move("forward");
                                //Console.WriteLine("Navigator - Move forward");
                            }
                            else
                            {
                                Console.WriteLine("Navigator - Next cell direction is not equal to head direction");
                                if (direction == Direction.East)
                                {
                                    if (headDirection == Direction.North)
                                    {
                                        Turn(TurnDirection.Right);
                                    }
                                    else
                                    {
                                        Turn(TurnDirection.Left);
                                    }
                                }
                                else if (direction == Direction.West)
                                {
                                    if (headDirection == Direction.North)
                                    {
                                        Turn(TurnDirection.Left);
                                    }
                                    else
                                    {
                                        Turn(TurnDirection.Right);
                                    }
                                }
                                else if (direction == Direction.North)
                                {
                                    if (headDirection == Direction.East)
                                    {
                                        Turn(TurnDirection.Left);
                                    }
                                    else if (headDirection == Direction.West)
                                    {
                                        Turn(TurnDirection.Right);
                                    }
                                    else if (headDirection == Direction.South)
                                    {
                                        Turn(TurnDirection.Right);
                                        Turn(TurnDirection.Right);
                                    }
                                }
                                else if (direction == Direction.South)
                                {
                                    if (headDirection == Direction.East)
                                    {
                                        Turn(TurnDirection.Right);
                                    }
                                    else if (headDirection == Direction.West)
                                    {
                                        Turn(TurnDirection.Left);
                                    }
                                    else if (headDirection == Direction.North)
                                    {
                                        Turn(TurnDirection.Right);
                                        Turn(TurnDirection.Right);
                                    }
                                }
                            }

                            // Update headDirection
                            headDirection = direction;
                        }
                        else
                        {
                            Console.WriteLine("Navigator - Start navigating to next cell");
                            Localizer.currentPosition = nextCell;
                            isArrivedAtNextCell = true;
                            // Check if there is any cell in cells
                            if (cells.Count > 0)
                            {
                                Console.WriteLine("Navigator - There is remaining cell in cells");
                                nextCell = cells.Dequeue();
                                Console.WriteLine("Navigator - Next cell is " + nextCell.x + ";" + nextCell.y);
                                isArrivedAtNextCell = false;
                            }
                            else
                            {
                                Console.WriteLine("Navigator - There is no cell in cells");
                                isNavigating = false;
                                isArrivedAtNextCell = false;
                                Console.WriteLine("Navigator - End navigating");
                            }
                        }
                    }
                }
            }
        }
    }
}