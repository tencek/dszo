
#r "paket: nuget FSharp.Data"

open FSharp.Data

[<Literal>]
let OutputCultureV1 = "en-US"
type Output = CsvProvider<"../samples/vehicles-v1.csv", Separators=";", Encoding="utf-8", Culture=OutputCulture,
                            Schema="Latitude=float,Longitude=float">

