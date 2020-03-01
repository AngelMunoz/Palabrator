module Types

type ModelOrID<'t> =
    | Id of int
    | Model of 't

type Usuario =
    { id: int
      nombre: string
      apellidos: string
      correo: string
      usuario: string
      contrasena: string }

type SafeUser =
    { id: int
      nombre: string
      apellidos: string
      correo: string
      usuario: string }

type Perfil =
    { id: int
      nombre: string
      usuario: ModelOrID<Usuario> }

type Palabra =
    { id: int
      palabra: string }

type AuthResponse =
    { accessToken: string
      email: string }

type LoginPayload =
    { email: string
      password: string }

type SignupPayload =
    { nombre: string
      apellidos: string
      correo: string
      usuario: string
      contrasena: string }

type BusquedaUsuario =
    | Correo of string
    | Usuario of string


exception UsuarioExisteException of string
