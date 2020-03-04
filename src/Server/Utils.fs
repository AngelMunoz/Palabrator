[<RequireQualifiedAccess>]
module Utils

let tryGetEnv =
    System.Environment.GetEnvironmentVariable
    >> function
    | null
    | "" -> None
    | x -> Some x


let isProd =
    let env =
        "PALABRATOR_ENV"
        |> tryGetEnv
        |> Option.map string
        |> Option.defaultValue "development"
    match env with
    | "production"
    | "Production" -> true
    | _ -> false

let port =
    "PORT"
    |> tryGetEnv
    |> Option.map uint16
    |> Option.defaultValue 4500us

let secretKey =
    "JWT_SECRET_KEY"
    |> tryGetEnv
    |> Option.map string
    |> Option.defaultValue "Change Me Please :( I'm quite insecure"

let issuer = "apps.tunaxor.me"
