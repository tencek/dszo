module Dszo.Dumper.Dumper
// Learn more about F# at http://fsharp.org

[<Literal>]
let OutputFile = @"C:\temp\vehicles2.csv"
let saveSnapshot = Snapshots.SaveSnapshot OutputFile

[<EntryPoint>]
let main argv =
    printfn "Hello World from dumper!"
    Seq.initInfinite ( fun _x -> ())
    |> Seq.fold (fun lastTimeStamp _elm -> 
        try
            let snapshot = Snapshots.CreateSnapShot ()
            if snapshot.TimeStamp <> lastTimeStamp then
                saveSnapshot snapshot
            System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(30000.0))
            snapshot.TimeStamp
        with 
            exn -> 
                printfn "%A: Some error occured: %A" System.DateTime.Now exn.Message
                System.Threading.Thread.Sleep(System.TimeSpan.FromMilliseconds(10000.0))
                lastTimeStamp) ( System.DateTime.MinValue )
    |> ignore
    0
