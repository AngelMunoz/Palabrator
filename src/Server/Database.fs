module Database

open Utils
open Npgsql.FSharp
open System
open Types

let private connStr =
    "DATABASE_URL"
    |> tryGetEnv
    |> Option.map string
    |> Option.defaultValue "postgresql://admin:Admin123@localhost:5432/posgrito"

let private defaultConnection =
    match isProd with
    | true ->
        Sql.fromUriToConfig (Uri connStr)
        |> Sql.sslMode SslMode.Require
        |> Sql.trustServerCertificate true
        |> Sql.formatConnectionString
    | false -> Sql.fromUri (Uri connStr)


[<RequireQualifiedAccess>]
module AuthQueries =

    let private puedeCrearUsuario (usuario: SignupPayload) =
        async {
            let! correoExiste = defaultConnection
                                |> Sql.connect
                                |> Sql.query "SELECT count(1) as total FROM usuarios WHERE correo = @correo"
                                |> Sql.parameters [ "@correo", Sql.string usuario.correo ]
                                |> Sql.executeAsync (fun reader -> reader.int "total")

            let! usuarioExiste = defaultConnection
                                 |> Sql.connect
                                 |> Sql.query "SELECT count(1) as total FROM usuarios WHERE usuario = @usuario"
                                 |> Sql.parameters [ "@usuario", Sql.string usuario.usuario ]
                                 |> Sql.executeAsync (fun reader -> reader.int "total")
            return match correoExiste, usuarioExiste with
                   | (Ok correo, Ok usuario) -> correo.Head + usuario.Head = 0
                   | _ -> false
        }

    let crearUsuario (usuario: SignupPayload) =
        async {
            let query = """
                INSERT INTO usuarios
                    (nombre, apellidos, correo, usuario, contrasena)
                VALUES(@nombre, @apellidos, @correo, @usuario, @contrasena);
                """
            let! puedeCrear = puedeCrearUsuario usuario
            match puedeCrear with
            | false -> return Error(UsuarioExisteException("El usuario ya existe"))
            | true ->
                return! defaultConnection
                        |> Sql.connect
                        |> Sql.query query
                        |> Sql.parameters
                            [ "@nombre", Sql.string usuario.nombre
                              "@apellidos", Sql.string usuario.apellidos
                              "@correo", Sql.string usuario.correo
                              "@usuario", Sql.string usuario.usuario
                              "@contrasena", Sql.string usuario.contrasena ]
                        |> Sql.executeNonQueryAsync
        }

    let buscarUsuario (criterio: BusquedaUsuario): Result<Usuario list, exn> Async =
        let statement =
            match criterio with
            | Correo correo ->
                let query = "SELECT * FROM usuarios WHERE correo = @correo"
                defaultConnection
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters [ "@correo", Sql.string correo ]
            | Usuario usuario ->
                let query = "SELECT * FROM usuarios WHERE usuario = @usuario"
                defaultConnection
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters [ "@usuario", Sql.string usuario ]
        statement
        |> Sql.executeAsync (fun reader ->
            { id = reader.int "id"
              nombre = reader.string "nombre"
              apellidos = reader.string "apellidos"
              correo = reader.string "correo"
              usuario = reader.string "usuario"
              contrasena = reader.string "contrasena" })
