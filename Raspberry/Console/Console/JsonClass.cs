namespace JsonClasses
{
    public enum LocalizingMethod
    {
        Beacon,
        Camera
    }

    public enum TurnDirection
    {
        Rear,
        Right,
        Left
    }

    public enum Movement
    {
        Forward,
        Backward,
        Stop,
        TurnLeft,
        TurnRight
    }

    public enum Direction
    {
        North,
        South,
        West,
        East,
        Northeast,
        Southeast,
        Northwest,
        Southwest,
        None
    }

    public enum CellType
    {
        Blank,
        Beacon,
        Agv,
        Rfid,
        Package,
        Lane,
        Charger,
        Obstacle
    }

    public class Cell
    {
        public int x { get; set; }
        public int y { get; set; }
        public CellType type { get; set; }
        public string? value { get; set; }
    }
    
    public enum LayerName
    {
        Base,
        Beacon,
        Package,
        Rfid,
        Lane,
        Obstacle
    }
    
    public class Layer
    {
        public LayerName name { get; set; }
        public List<Cell>? cells { get; set; }
    }

    public class Map
    {
        public string? name { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public List<Layer>? layers { get; set; }
    }
    
    public class Location
    {
        public int x { get; set; }
        public int y { get; set; }
    }
    
    public class Destinations
    {
        public int x { get; set; }
        public int y { get; set; }
    }
    
    public class Items
    {
        public string? id { get; set; }
        public Location? location { get; set; }
        public int quantity { get; set; }
    }
    
    public class DeliveryRequest
    {
        public int timestamp { get; set; }
        public List<Items>? items { get; set; }
        public List<Destinations>? destinations { get; set; }
    }
    
    public class DeliveryPath
    {
        public List<Cell>? path { get; set; }
        public int deliveryTime { get; set; }
        public int deliveryDistance { get; set; }
    }

    public class JsonClass
    {
    }
}