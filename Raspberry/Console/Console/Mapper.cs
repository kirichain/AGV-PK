using Cell = JsonClasses.Cell;
using Map = JsonClasses.Map;
using Layer = JsonClasses.Layer;
using LayerName = JsonClasses.LayerName;
using CellType = JsonClasses.CellType;

namespace Mappers
{
    public class Mapper
    {
        public static string[,] baseLayer, beaconIdLayer, packageIdLayer;
        public string? baseLayerData, beaconIdLayerData;
        private readonly Dictionary<string, Map> maps = new Dictionary<string, Map>();

        public Mapper()
        {
            Console.WriteLine("Mapper Init Done");
        }

        public void PrintMap(string name, LayerName layer)
        {
            var map = maps[name];
            Console.WriteLine("Map Info:");
            Console.WriteLine("Map Name: " + map.name);
            Console.WriteLine("Map Width: " + map.width);
            Console.WriteLine("Map Height: " + map.height);
            // Print Base Layer Cell Type
            if (layer == LayerName.Base)
            {
                var _baseLayer = map.layers?.Find(l => l.name == LayerName.Base);
                if (_baseLayer != null)
                {
                    Console.WriteLine("Base Layer Cell Type:");
                    for (int i = 0; i < map.height; i++)
                    {
                        for (int j = 0; j < map.width; j++)
                        {
                            Console.Write(_baseLayer.cells?[i * map.width + j].type + " ");
                        }

                        Console.WriteLine();
                    }
                }
            }
        }

        public Map LoadMap(string name)
        {
            return maps[name];
        }

        private Layer CreateLayer(LayerName layerName, int length, int width)
        {
            var layer = new Layer()
            {
                name = layerName,
                cells = new List<Cell>()
            };
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    layer.cells.Add(new Cell()
                    {
                        x = j,
                        y = i,
                        type = CellType.Blank,
                        value = "0"
                    });
                }
            }

            return layer;
        }

        public Map CreateMap(string name, int length, int width)
        {
            var map = new Map()
            {
                name = name,
                width = width,
                height = length,
                layers = new List<Layer>()
            };
            // Create layers with initial data set to 0
            var baseLayer = CreateLayer(LayerName.Base, length, width);
            var beaconLayer = CreateLayer(LayerName.Beacon, length, width);
            var packageLayer = CreateLayer(LayerName.Package, length, width);
            var rfidLayer = CreateLayer(LayerName.Rfid, length, width);
            var laneLayer = CreateLayer(LayerName.Lane, length, width);
            var obstacleLayer = CreateLayer(LayerName.Obstacle, length, width);
            map.layers.Add(baseLayer);
            map.layers.Add(beaconLayer);
            map.layers.Add(packageLayer);
            map.layers.Add(rfidLayer);
            map.layers.Add(laneLayer);
            map.layers.Add(obstacleLayer);
            maps.Add(name, map);
            Console.WriteLine($"Map with name {name} is created");
            return map;
        }

        public Map DeleteMap(string name, LayerName layer)
        {
            var map = new Map();
            return map;
        }

        public Map UpdateMap(string name, LayerName layer, Cell cell)
        {
            var mapLength = maps[name].height;
            var mapWidth = maps[name].width;
            maps[name].layers[(int)layer].cells[((cell.y + 1) * mapWidth) - (mapWidth - cell.x)] = cell;
            return maps[name];
        }

        public static void InitMap(LayerName layerName)
        {
            //JsonNode document = JsonNode.Parse(layerName)!;

            //JsonNode root = document.Root;

            //JsonArray coords = root["coordinates"]!.AsArray();

            // foreach (JsonNode? coord in coords)
            // {
            //     int x = Convert.ToInt32(coord["x"].ToString());
            //     int y = Convert.ToInt32(coord["y"].ToString());
            //
            //     if (layerName == LayerName.Base)
            //     {
            //         baseLayer[x, y] = coord["type"].ToString();
            //     }
            //     else if (layerName == LayerName.Beacon)
            //     {
            //         beaconIdLayer[x, y] = coord["name"].ToString();
            //     }
            //else if (layerName == LayerName.BeaconData)
            //{
            //layer[x, y] = coord["data"].ToString();
            //}
            //Console.WriteLine("Cell type = " + coord["type"] + " coord = " + x + " " + y);
            //}
        }
    }
}