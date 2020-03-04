module Database

open Npgsql.FSharp
open System
open Types

let private connStr =
    "DATABASE_URL"
    |> Utils.tryGetEnv
    |> Option.map string
    |> Option.defaultValue "postgresql://admin:Admin123@localhost:5432/posgrito"

let private defaultConnection =
    match Utils.isProd with
    | true ->
        Sql.fromUriToConfig (Uri connStr)
        |> Sql.sslMode SslMode.Require
        |> Sql.trustServerCertificate true
        |> Sql.formatConnectionString
    | false -> Sql.fromUri (Uri connStr)


let prepareDefault query queryParams =
    defaultConnection
    |> Sql.connect
    |> Sql.query query
    |> Sql.parameters queryParams
