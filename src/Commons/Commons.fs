namespace Commons

open System

type VehicleInfo = 
    {
        VehicleNumber : int
        LineNumber : int
        Delay : TimeSpan
        Station : string
        Direction : string
        Shift : string
        Driver : int
    }