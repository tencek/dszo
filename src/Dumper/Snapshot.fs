module Dszo.Dumper.Snapshots

open FSharp.Data
open Dszo.Domain
open Dszo.Dumper.Tools
open System

type Snapshot = { TimeStamp:DateTime ; Vehicles:seq<Vehicle> }

type Vehicles = JsonProvider<"http://www.dszo.cz/online/tabs2.php", Encoding="utf-8">

