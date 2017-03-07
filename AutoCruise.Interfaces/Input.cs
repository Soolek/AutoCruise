namespace AutoCruise.Interfaces
{
    public interface Input
    {
        void GetFrontView(); //image
        void GetStatus(); //steer, [acc, brake, clutch], gear, speed, rpm
        void GetRadar(); //other cars, obstacles, ??
    }
}
