[<RequireQualifiedAccess>]
module Perfiles

open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Npgsql.FSharp
open Types

type private PerfilPayload = {| nombre: string; usuario: int |}

let private crearPerfil (perfil: PerfilPayload) =
    async {
        let insert = """
                INSERT INTO perfiles
                    (nombre, usuario_id)
                VALUES(@nombre, @usuarioid) RETURNING id;
                """

        let insertParams =
            [ "@nombre", Sql.string perfil.nombre
              "@usuarioid", Sql.int perfil.usuario ]
        return! Database.prepareDefault insert insertParams
                |> Sql.executeAsync (fun reader -> {| perfil = reader.int "id" |})
    }

let private obtenerPerfiles (userid: int) =
    async {
        let select = """
            SELECT * FROM perfiles WHERE usuario_id = @userid
            """
        let selectParams = [ "@userid", Sql.int userid ]
        return! Database.prepareDefault select selectParams
                |> Sql.executeAsync (fun reader ->
                    {| id = reader.int "id"
                       nombre = reader.string "nombre"
                       usuario = reader.int "usuario_id" |})
    }

let private obtenerPerfil (perfilid: int) =
    async {
        let select = """
            SELECT * FROM perfiles WHERE id = @perfilid
            """
        let selectParams = [ "@perfilid", Sql.int perfilid ]
        return! Database.prepareDefault select selectParams
                |> Sql.executeAsync (fun reader ->
                    {| id = reader.int "id"
                       nombre = reader.string "nombre"
                       usuario = reader.int "usuario_id" |})
    }


let private perfiles (userid: int) (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! resultados = obtenerPerfiles userid
        return! match resultados with
                | Ok perfiles -> json perfiles next ctx
                | Error err ->
                    printfn "%O" err
                    setStatusCode (422) next ctx
    }

let private perfil (perfilid: int) (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! resultados = obtenerPerfil perfilid
        return! match resultados with
                | Ok perfiles ->
                    match perfiles |> List.tryHead with
                    | Some perfil -> json perfil next ctx
                    | None -> setStatusCode (404) next ctx
                | Error err ->
                    printfn "%O" err
                    setStatusCode (422) next ctx
    }

let nuevoPerfil (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! payload = ctx.BindJsonAsync<PerfilPayload>()
        let! resultado = crearPerfil payload
        match resultado with
        | Ok perfiles -> return! (json perfiles.Head >=> setStatusCode (201)) next ctx
        | Error err ->
            printfn "%O" err
            return! setStatusCode (400) next ctx
    }

let rutas =
    router {
        pipe_through (Auth.requireAuthentication JWT)
        post "/" nuevoPerfil
        getf "/%i" perfil
        getf "/usuario/%i" perfiles
    }
