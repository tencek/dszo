module Dszo.Dumper.Snapshots

open Dszo.Domain
open Dszo.Tools
open System
open System.IO
open FSharp.Data

type Snapshot = { TimeStamp:DateTime ; Vehicles:seq<Vehicle> }

type Vehicles = JsonProvider<"http://www.dszo.cz/online/tabs2.php", Encoding="utf-8">

let CreateSnapShot logger =
    let loadCoordinates () = 
        let (timeStamp, coordinates, orientations) = 
            Http.RequestString("http://www.dszo.cz/online/pokus.php", responseEncodingOverride="utf-8").Split('\n')
            |> Seq.fold (fun (timestamp, coordinates, orientations) line -> 
                match line.Trim() with
                | Regex @"window\.epoint([0-9]+)=new google\.maps\.LatLng\((.+),(.+)\);" [vehicleNum ; lat ; lng ] -> 
                    (timestamp, ((int vehicleNum, {Lat=float lat ; Lng = float lng})::coordinates), orientations)
                | Regex @"image([0-9]+) = \{[^\}]*rotation: ([0-9]+),[^\}]}*" [vehicleNum ; orientation] ->
                    (timestamp, coordinates, (int vehicleNum, Orientation (int orientation))::orientations)
                | Regex @"Data aktualizována: ([0-9:\. ]+)&nbsp;" [dateTimeStr] -> 
                    try
                        (DateTime.Parse(dateTimeStr) |> Some, coordinates, orientations)
                    with
                        _exn -> 
                            logger <| sprintf "Failed to parse %A as date time!" dateTimeStr
                            (timestamp, coordinates, orientations)
                | _ -> 
                    (timestamp, coordinates, orientations)
            ) (None, List.empty, List.Empty)
        match timeStamp with
        | Some timeStamp -> 
            (timeStamp, Map.ofList coordinates, Map.ofList orientations)
        | None -> 
            logger <| "Timestamp not loaded! Using current time..."
            (DateTime.Now, Map.ofList coordinates, Map.ofList orientations)

    let (timeStamp, coordinates, orientations) = loadCoordinates ()
    let vehicles = 
        Vehicles.Load("http://www.dszo.cz/online/tabs2.php").Data
        |> Seq.map (fun item -> 
            try
                {
                    Number = int item.Strings.[0]
                    LineNumber = int item.Strings.[1]
                    Delay = TimeSpan.Parse("00:"+item.Strings.[2])
                    Station = item.Strings.[3]
                    Direction = item.Strings.[4]
                    Shift = item.Strings.[5]
                    Driver = int item.Strings.[6]
                    Coordinates = coordinates.Item (int item.Strings.[0])
                    Orientation = orientations.Item (int item.Strings.[0])
                } |> Some
            with
                exn -> 
                    logger <| sprintf "Data error: %A" exn
                    logger <| sprintf "Item not parsed: %A" item
                    None
        )
        |> Seq.choose id
        |> Seq.sortBy (fun vehicle -> vehicle.Number)
    { TimeStamp=timeStamp ; Vehicles=vehicles }

let SaveSnapshot logger outFilePath snapshot = 
    if not <| File.Exists(outFilePath) then
        let line = sprintf "%A;%A;%A;%A;%A;%A;%A;%A;%A;%A;%A;%A;%A" "DayOfWeek" "Date" "Time" "Number" "LineNumber" "Delay" "Station" "Direction" "Shift" "Driver" "Latitude" "Longitude" "Orientation"
        File.AppendAllLines (outFilePath, Seq.singleton line) 

    let linesOut = 
        snapshot.Vehicles
        |> Seq.map (fun v -> 
            let (Orientation orientation) = v.Orientation
            sprintf "%A;%s;%s;%d;%d;%A;%A;%A;%A;%d;%f;%f;%d"
                snapshot.TimeStamp.DayOfWeek 
                (snapshot.TimeStamp.ToShortDateString()) 
                (snapshot.TimeStamp.ToLongTimeString()) 
                v.Number 
                v.LineNumber 
                v.Delay 
                v.Station 
                v.Direction 
                v.Shift 
                v.Driver 
                v.Coordinates.Lat 
                v.Coordinates.Lng 
                orientation)
    File.AppendAllLines(outFilePath, linesOut)
