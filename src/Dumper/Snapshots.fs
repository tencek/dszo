module Dszo.Dumper.Snapshots

open Dszo.Domain
open Dszo.Tools
open System
open System.Globalization
open System.IO
open FSharp.Data

type Snapshot = { TimeStamp:DateTime ; Vehicles:seq<Vehicle> }

type Vehicles = JsonProvider<"http://www.dszo.cz/online/tabs2.php", Encoding="utf-8">

[<Literal>]
let OutputCulture = "cs-CZ"
type Output = CsvProvider<"samples/vehicles.csv", Separators=";", Encoding="utf-8", Culture=OutputCulture>

let AsyncGetLatestTimestamp logger filepath = 
    async {
        try
            let! snapshots = Output.AsyncLoad(filepath)
            return 
                snapshots.Rows
                |> Seq.last
                |> ( fun lastRow ->
                    lastRow.Date + lastRow.Time
                    |> Some)
        with
            exn ->
                logger <| sprintf "Failed to get last snapshot time from %s: %s" filepath exn.Message
                return None
        }

let AsyncCreateSnapshot logger = 
    async {
        let! geospatialResponse = 
            Http.AsyncRequestString("http://www.dszo.cz/online/pokus.php", responseEncodingOverride="utf-8") 
            |> Async.StartChild
        let! vehiclesResponse = 
            Vehicles.AsyncLoad("http://www.dszo.cz/online/tabs2.php")
            |> Async.StartChild

        let! geospatials = geospatialResponse
        let (timeStamp, coordinates, orientations) = 
            geospatials.Split('\n')
            |> Seq.fold (fun (timestamp, coordinates, orientations) line -> 
                match line.Trim() with
                | Regex @"window\.epoint([0-9]+)=new google\.maps\.LatLng\((.+),(.+)\);" [vehicleNum ; lat ; lng ] -> 
                    (timestamp, ((int vehicleNum, {Lat=float lat ; Lng = float lng})::coordinates), orientations)
                | Regex @"image([0-9]+) = \{[^\}]*rotation: ([0-9]+),[^\}]}*" [vehicleNum ; orientation] ->
                    (timestamp, coordinates, (int vehicleNum, Orientation (int orientation))::orientations)
                | Regex @"Data aktualizována: ([0-9:\. ]+)&nbsp;" [dateTimeStr] -> 
                    try
                        (DateTime.Parse(dateTimeStr, new CultureInfo("cs-CZ")) |> Some, coordinates, orientations)
                    with
                        _exn -> 
                            logger <| sprintf "Failed to parse %s as date time!" dateTimeStr
                            (timestamp, coordinates, orientations)
                | _ -> 
                    (timestamp, coordinates, orientations)
            ) (None, List.empty, List.Empty)
            |> function
            | (Some timeStamp, coordinates, orientations) -> 
                (timeStamp, Map.ofList coordinates, Map.ofList orientations)
            | (None, coordinates, orientations) -> 
                logger <| "Timestamp not loaded! Using current time..."
                (DateTime.Now, Map.ofList coordinates, Map.ofList orientations)

        let! vehicles = vehiclesResponse
        return 
            vehicles.Data
            |> Seq.map (fun item -> 
                try
                    {
                        Number = int item.Strings.[0]
                        LineNumber = int item.Strings.[1]
                        Delay = TimeSpan.Parse("00:"+item.Strings.[2], new CultureInfo("cs-CZ"))
                        Station = item.Strings.[3]
                        Direction = item.Strings.[4]
                        Shift = item.Strings.[5]
                        Driver = int item.Strings.[6]
                        Coordinates = coordinates.Item (int item.Strings.[0])
                        Orientation = orientations.Item (int item.Strings.[0])
                    } |> Some
                with
                    exn -> 
                        logger <| sprintf "Data error: %s" exn.Message
                        logger <| sprintf "Item not parsed: %A" item
                        None
            )
            |> Seq.choose id
            |> Seq.sortBy (fun vehicle -> vehicle.Number)
            |> ( fun vehicles -> 
                    { TimeStamp=timeStamp ; Vehicles=vehicles })
    }

let AsyncSaveSnapshot logger outFilePath snapshot = 
    async {
        if not <| File.Exists(outFilePath) then
            let line = sprintf "%A;%A;%A;%A;%A;%A;%A;%A;%A;%A;%A;%A;%A" "DayOfWeek" "Date" "Time" "Number" "LineNumber" "Delay" "Station" "Direction" "Shift" "Driver" "Latitude" "Longitude" "Orientation"
            do! File.AppendAllLinesAsync (outFilePath, Seq.singleton line) |> Async.AwaitTask

        let linesOut = 
            snapshot.Vehicles
            |> Seq.map (fun v -> 
                let outputFormatProvider = new CultureInfo(OutputCulture)
                let (Orientation orientation) = v.Orientation
                sprintf "%s;%s;%s;%d;%d;%A;%A;%A;%A;%d;%f;%f;%d"
                    (snapshot.TimeStamp.ToString("ddd", outputFormatProvider))
                    (snapshot.TimeStamp.ToString(outputFormatProvider.DateTimeFormat.ShortDatePattern))
                    (snapshot.TimeStamp.ToString(outputFormatProvider.DateTimeFormat.LongTimePattern))
                    v.Number 
                    v.LineNumber 
                    (v.Delay.ToString("c", outputFormatProvider))
                    v.Station 
                    v.Direction 
                    v.Shift 
                    v.Driver 
                    v.Coordinates.Lat 
                    v.Coordinates.Lng 
                    orientation)
        do! File.AppendAllLinesAsync(outFilePath, linesOut) |> Async.AwaitTask
        }
