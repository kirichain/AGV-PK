namespace ComputerVisions
{
    public class ComputerVision
    {
        public enum CameraConnectionType
        {
            Rstp,
            Usb,
            Ip,
            Module
        }
        public ComputerVision(string cameraModuleName)
        {
            Console.WriteLine("Computer Vision: " + cameraModuleName + " Init Done");
        }
        
        public bool ConnectCamera(CameraConnectionType cameraConnectionType)
        {
            Console.WriteLine("Camera with connection type: " + cameraConnectionType + " is connected");
            return true;
        }
    }
}

