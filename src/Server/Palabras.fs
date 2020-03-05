[<RequireQualifiedAccess>]
module Palabras

open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Npgsql.FSharp
open Types

let private crearPalabra (palabra: string) =
    async {
        let insert = """
            INSERT INTO palabras
                (palabra)
            VALUES(@palabra) RETURNING id;
            """
        let insertParams = [ "@palabra", Sql.string (palabra.ToLowerInvariant()) ]
        return! Database.prepareDefault insert insertParams
                |> Sql.executeAsync (fun reader -> {| palabra = reader.int "id" |})
    }

let private asociarPalabraPerfil (perfilId: int) (palabraId: int) =
    async {
        let insert = """
            INSERT INTO palabras_perfiles (perfil_id, palabras_id)
            VALUES(@perfilid, @palabraid);
            """

        let insertParams =
            [ "@perfilid", Sql.int perfilId
              "@palabraid", Sql.int palabraId ]
        return! Database.prepareDefault insert insertParams |> Sql.executeNonQueryAsync
    }

let private disociarPalabraPerfil (perfilId: int) (palabraId: int) =
    async {
        let insert = """
            DELETE FROM palabras_perfiles WHERE perfil_id = @perfilId AND palabras_id = @palabraId;
            """

        let insertParams =
            [ "@perfilId", Sql.int perfilId
              "@palabraId", Sql.int palabraId ]
        return! Database.prepareDefault insert insertParams |> Sql.executeNonQueryAsync
    }

let palabrasPorPerfil (perfilid: int) =
    async {
        let select = """
            SELECT palabra.id AS "palabra_id", palabra.palabra from perfiles AS perfil
            INNER JOIN palabras_perfiles AS pp
                ON pp.perfil_id = perfil.id
                AND perfil.id = @perfilid
            INNER JOIN palabras AS palabra
                ON pp.palabras_id  = palabra.id
            """
        let selectParams = [ "@perfilid", Sql.int perfilid ]
        return! Database.prepareDefault select selectParams
                |> Sql.prepare
                |> Sql.executeAsync (fun reader ->
                    {| palabraId = reader.int "palabra_id"
                       palabra = reader.string "palabra" |})
    }

let private nuevaPalabra (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! payload = ctx.BindJsonAsync<{| palabra: string |}>()
        let! resultados = crearPalabra payload.palabra
        match resultados with
        | Ok palabra -> return! (json palabra >=> setStatusCode (201)) next ctx
        | Error err ->
            printf "%O" err
            return! setStatusCode (400) next ctx
    }

let private agregarPalabraPerfil (palabraId: int, perfilId: int) (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! result = asociarPalabraPerfil perfilId palabraId
        return! match result with
                | Ok _ -> next ctx
                | Error err ->
                    printfn "%O" err
                    setStatusCode (400) next ctx
    }

let private removerPalabraPerfil (palabraId: int, perfilId: int) (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! result = disociarPalabraPerfil perfilId palabraId
        return! match result with
                | Ok _ -> next ctx
                | Error err ->
                    printfn "%O" err
                    setStatusCode (400) next ctx
    }

let private obtenerPalabrasPorPerfil (perfilId: int) (next: HttpFunc) (ctx: HttpContext) =
    task {
        let! result = palabrasPorPerfil perfilId
        return! match result with
                | Ok palabras -> json palabras next ctx
                | Error err ->
                    printfn "%O" err
                    setStatusCode (422) next ctx
    }


let rutas =
    router {
        pipe_through (Auth.requireAuthentication JWT)
        post "/" nuevaPalabra
        putf "/%i/perfil/%i" agregarPalabraPerfil
        deletef "/%i/perfil/%i" removerPalabraPerfil
        getf "/perfil/%i" obtenerPalabrasPorPerfil
    }
