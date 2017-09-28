// Learn more about F# at http://fsharp.org

open System
open Commons

[<EntryPoint>]
let main argv =
    let vehicle = { 
        VehicleNumber = 766 ; 
        LineNumber = 12 ; 
        Delay = System.TimeSpan.FromSeconds(30.0) ;
        Station = "Pančava" ;
        Direction ="Sportovní hala" ;
        Shift = "12/3" ;
        Driver = 1234
    }
    printfn "%A" vehicle
    0 // return an integer exit code
