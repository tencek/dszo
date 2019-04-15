module Dszo.Logging

type Timestamp = Timestamp of System.DateTime

type Severity = Error | Warning | Info | Debug

type SimpleLogger = string -> Async<unit>

type Logger = Severity -> SimpleLogger

type TimestampProvider = unit -> Timestamp

type TimeStampFormatter = Timestamp -> string

let DefaultTimestampProvider () = 
    Timestamp System.DateTime.Now

let DefaultTimestampFormatter (timestamp:Timestamp) = 
    let (Timestamp dateTime) = timestamp
    sprintf "%A" dateTime

let FileSimpleLogger (timestampProvider:TimestampProvider) (timestampFormatter:TimeStampFormatter) filePath  text = 
    async {
        do! 
            let ts = timestampProvider () |> timestampFormatter
            let output = sprintf "%s: %s" ts text
            System.IO.File.AppendAllTextAsync(filePath, output) 
            |> Async.AwaitTask 
            |> Async.StartChild 
            |> Async.Ignore
        }

let ConsoleSimpleLogger (timestampProvider:TimestampProvider) (timestampFormatter:TimeStampFormatter) text =
    async {
        let ts = timestampProvider () |> timestampFormatter
        printfn "%s: %s" ts text
        return ()
        }
//let DefaultLogger severity text = 
//    async {
//        printfn "%A: %s" severity text
//        return ()
//        }

//let logger:Logger = DefaultLogger

