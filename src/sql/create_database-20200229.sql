BEGIN;
CREATE TABLE IF NOT EXISTS usuarios (
  id SERIAL PRIMARY KEY,
  nombre VARCHAR(100) NOT NULL CHECK (nombre <> ''),
  apellidos VARCHAR(100) NOT NULL CHECK (apellidos <> ''),
  correo VARCHAR(100) NOT NULL CHECK (correo <> ''),
  usuario VARCHAR(100) NOT NULL CHECK (usuario <> ''),
  contrasena VARCHAR(100) NOT NULL CHECK (contrasena <> ''),
  UNIQUE(correo, usuario)
);
CREATE TABLE IF NOT EXISTS perfiles (
  id SERIAL PRIMARY KEY,
  nombre VARCHAR(50) NOT NULL CHECK (nombre <> ''),
  usuario_id INTEGER REFERENCES usuarios,
  UNIQUE(nombre)
);
CREATE TABLE IF NOT EXISTS palabras (
  id SERIAL PRIMARY KEY,
  palabra VARCHAR(100) NOT NULL CHECK (palabra <> ''),
  UNIQUE(palabra)
);
CREATE TABLE IF NOT EXISTS palabras_perfiles (
  perfil_id INTEGER REFERENCES perfiles ON DELETE CASCADE,
  palabras_id INTEGER REFERENCES palabras ON DELETE RESTRICT,
  PRIMARY KEY (perfil_id, palabras_id)
);
COMMIT;