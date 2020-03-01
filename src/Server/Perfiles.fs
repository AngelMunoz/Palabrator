[<RequireQualifiedAccess>]
module Perfiles

open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn

let private perfiles (userid: int) (next: HttpFunc) (ctx: HttpContext) =
    task { return! (text (sprintf "userid: %i" userid)) next ctx }

let private perfil (userid: int, perfilid: int) (next: HttpFunc) (ctx: HttpContext) =
    task { return! (text (sprintf "userid: %i - perfilid: %i" userid perfilid)) next ctx }

let rutas =
    router {
        pipe_through (Auth.requireAuthentication JWT)
        getf "/usuario/%i" perfiles
        getf "/usuario/%i/detalle/%i" perfil
    }
