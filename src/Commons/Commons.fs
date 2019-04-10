namespace dszo.Commons

type Coordinates = { Lat:float ; Lng:float }

type Orientation = Orientation of int

type Vehicle = 
    {
        Number : int
        LineNumber : int
        Delay : System.TimeSpan
        Station : string
        Direction : string
        Shift : string
        Driver : int
        Coordinates : Coordinates
        Orientation : Orientation
    }

