--DROP DATABASE dbFutbolData;
-- 1. Crear la Base de Datos
CREATE DATABASE dbFutbolData;
GO

USE dbFutbolData;
GO
CREATE SCHEMA [Base];
GO
CREATE SCHEMA [Metrica];
GO
CREATE SCHEMA [Seguridad];
GO

/*==============================================================
    MODULO DE SEGURIDAD
==============================================================*/

-- ============================================================
-- USUARIOS
-- ============================================================
CREATE TABLE [Seguridad].[Usuarios]
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Usuario VARCHAR(50) NOT NULL,
    Correo VARCHAR(150) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    NombreCompleto VARCHAR(200) NOT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    FechaActualizacion DATETIME NULL,
    UltimoAcceso DATETIME NULL
);

ALTER TABLE [Seguridad].[Usuarios]
ADD CONSTRAINT UQ_Usuarios_Usuario
UNIQUE(Usuario);

ALTER TABLE [Seguridad].[Usuarios]
ADD CONSTRAINT UQ_Usuarios_Correo
UNIQUE(Correo);


-- ============================================================
-- ROLES
-- ============================================================
CREATE TABLE [Seguridad].[Roles]
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(250),
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE()
);

ALTER TABLE [Seguridad].[Roles]
ADD CONSTRAINT UQ_Roles_Nombre
UNIQUE(Nombre);


-- ============================================================
-- USUARIO - ROL
-- ============================================================
CREATE TABLE [Seguridad].[UsuarioRol]
(
    IdUsuario INT NOT NULL,
    IdRol INT NOT NULL,
    FechaAsignacion DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_UsuarioRol PRIMARY KEY(IdUsuario, IdRol),
    CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY(IdUsuario) REFERENCES [Seguridad].[Usuarios](Id),
    CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY(IdRol) REFERENCES [Seguridad].[Roles](Id)
);


-- ============================================================
-- MODULOS
-- ============================================================
CREATE TABLE [Seguridad].[Modulos]
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Codigo VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(250) NULL,
    Ruta VARCHAR(250) NULL,
    Icono VARCHAR(100) NULL,
    OrdenMenu INT NOT NULL DEFAULT 0,
    VisibleMenu BIT NOT NULL DEFAULT 1,
    Activo BIT NOT NULL DEFAULT 1
);

ALTER TABLE [Seguridad].[Modulos]
ADD CONSTRAINT UQ_Modulos_Codigo
UNIQUE(Codigo);



-- ============================================================
-- OPCIONES
-- Pantallas o funcionalidades dentro de un módulo
-- ============================================================
CREATE TABLE [Seguridad].[Opciones]
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdModulo INT NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    Codigo VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(250) NULL,
    Ruta VARCHAR(250) NULL,
    Icono VARCHAR(100) NULL,
    OrdenMenu INT NOT NULL DEFAULT 0,
    VisibleMenu BIT NOT NULL DEFAULT 1,
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Opciones_Modulos FOREIGN KEY(IdModulo) REFERENCES [Seguridad].[Modulos](Id)
);

ALTER TABLE [Seguridad].[Opciones]
ADD CONSTRAINT UQ_Opciones_Codigo
UNIQUE(Codigo);


-- ============================================================
-- PERMISOS
-- ============================================================
CREATE TABLE [Seguridad].[Permisos]
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdOpcion INT NOT NULL,
    Codigo VARCHAR(150) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(250) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Permisos_Opciones FOREIGN KEY(IdOpcion)  REFERENCES [Seguridad].[Opciones](Id)
);

ALTER TABLE [Seguridad].[Permisos]
ADD CONSTRAINT UQ_Permisos_Codigo
UNIQUE(Codigo);


-- ============================================================
-- ROL - PERMISO
-- ============================================================
CREATE TABLE [Seguridad].[RolPermiso]
(
    IdRol INT NOT NULL,
    IdPermiso INT NOT NULL,
    FechaAsignacion DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_RolPermiso PRIMARY KEY(IdRol, IdPermiso),
    CONSTRAINT FK_RolPermiso_Rol FOREIGN KEY(IdRol) REFERENCES [Seguridad].[Roles](Id),
    CONSTRAINT FK_RolPermiso_Permiso FOREIGN KEY(IdPermiso) REFERENCES [Seguridad].[Permisos](Id)
);


-- ============================================================
-- AUDITORIA
-- ============================================================
CREATE TABLE [Seguridad].[Auditoria]
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL,
    Tabla VARCHAR(100) NOT NULL,
    Accion VARCHAR(50) NOT NULL,
    RegistroId VARCHAR(100) NOT NULL,
    DatosAnteriores NVARCHAR(MAX) NULL,
    DatosNuevos NVARCHAR(MAX) NULL,
    Fecha DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY(IdUsuario) REFERENCES [Seguridad].[Usuarios](Id)
);


-- ============================================================
-- REFRESH TOKENS
-- ============================================================
CREATE TABLE [Seguridad].[RefreshTokens]
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL,
    Token NVARCHAR(500) NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    FechaExpiracion DATETIME NOT NULL,
    Revocado BIT NOT NULL DEFAULT 0,
    FechaRevocacion DATETIME NULL,
    CONSTRAINT FK_RefreshTokens_Usuario FOREIGN KEY(IdUsuario) REFERENCES [Seguridad].[Usuarios](Id)
);
GO
CREATE TABLE [Base].[TipoClasificadores] (
   Id INT IDENTITY(1,1) PRIMARY KEY,
   Descripcion VARCHAR(300),
   Estado bit
);
GO
CREATE TABLE [Base].[Clasificadores](
	Id INT IDENTITY(1,1) PRIMARY KEY,
	IdTipoClasificador INT NOT NULL,
	Descripcion VARCHAR(300),
	Abreviacion VARCHAR(50),
	Estado bit,
	CONSTRAINT FK_TipoClasificador FOREIGN KEY (IdTipoClasificador) REFERENCES [Base].[TipoClasificadores](Id)
)
GO
CREATE TABLE [Base].[Temporadas](
    Id INT IDENTITY PRIMARY KEY,
    Nombre VARCHAR(20),
    FechaInicio DATE,
    FechaFin DATE
)
GO
-- 2. Tabla de Ligas
CREATE TABLE [Base].[Ligas] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NombreLiga VARCHAR(100) NOT NULL ,
    Pais VARCHAR(50) NOT NULL,
	IdTemporada INT NOT NULL,
	Tipo VARCHAR(50),
	CONSTRAINT FK_Temporada FOREIGN KEY (IdTemporada) REFERENCES [Base].[Temporadas](Id)
);

-- 3. Tabla de Equipos
CREATE TABLE [Base].[Equipos] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NombreEquipo VARCHAR(100) NOT NULL UNIQUE,
	Pais VARCHAR(50) NOT NULL,
);

CREATE TABLE LigaEquipos(
    Id INT IDENTITY PRIMARY KEY,
    IdLiga INT,
    IdEquipo INT,
    IdTemporada INT
)
-- 4. Tabla de Partidos
CREATE TABLE [Metrica].[Partidos] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdLiga INT NOT NULL,
    IdLocal INT NOT NULL,
    IdVisitante INT NOT NULL,
    Fecha DATETIME NOT NULL,
    IdTemporada INT NOT NULL,
	-- Marcadores Finales y Descanso
    goles_local INT,
    goles_visitante INT,
    goles_local_PrimerTiempo INT,
    goles_visitante_PrimerTiempo INT,
	goles_local_SegundoTiempo INT,
    goles_visitante_SegundoTiempo INT,
    CONSTRAINT FK_Partidos_Ligas FOREIGN KEY (IdLiga) REFERENCES [Base].[Ligas](Id),
    CONSTRAINT FK_Partidos_Local FOREIGN KEY (IdLocal) REFERENCES [Base].[Equipos](Id),
    CONSTRAINT FK_Partidos_Visitante FOREIGN KEY (IdVisitante) REFERENCES [Base].[Equipos](Id),
	CONSTRAINT FK_Temporada FOREIGN KEY (IdTemporada) REFERENCES [Base].[Temporadas](Id)
);

-- 5. Tabla de Estadísticas de Partido (Totales por equipo)
CREATE TABLE [Metrica].[EstadisticasPartido] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdPartido INT NOT NULL,
    IdEquipo INT NOT NULL,
    Condicion VARCHAR(9) NOT NULL CHECK(Condicion IN ('Local', 'Visitante')),
    Posesion DECIMAL(5,2), -- Permite guardar porcentajes con decimales ej: 55.40
	TirosTotales INT DEFAULT 0,
    TirosArco INT DEFAULT 0,
    Corners INT DEFAULT 0,
	offsides INT DEFAULT 0,
	faltas INT,
    tarjetas_amarillas INT,
    tarjetas_rojas INT,
    CONSTRAINT FK_Estadisticas_Partidos FOREIGN KEY (IdPartido) REFERENCES [Metrica].[Partidos](Id) ON DELETE CASCADE,
    CONSTRAINT FK_Estadisticas_Equipos FOREIGN KEY (IdEquipo) REFERENCES [Base].[Equipos](Id)
);

-- 6. Tabla de Eventos Detallados (Goles y Tarjetas por tiempos)
CREATE TABLE [Metrica].[EventosDetalle] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IdPartido INT NOT NULL,
    IdEquipo INT NOT NULL,
    IdcTipoEvento INT NOT NULL, ----(TipoEvento IN ('Gol', 'Tarjeta Amarilla', 'Tarjeta Roja')),
    Minuto INT NOT NULL,
    Periodo VARCHAR(15) NOT NULL CHECK(Periodo IN ('Primer Tiempo', 'Segundo Tiempo')),
    CONSTRAINT FK_Eventos_Partidos FOREIGN KEY (IdPartido) REFERENCES [Metrica].[Partidos](Id) ON DELETE CASCADE,
    CONSTRAINT FK_Eventos_Equipos FOREIGN KEY (IdEquipo) REFERENCES [Base].[Equipos](Id),
	CONSTRAINT FK_Clasificador FOREIGN KEY (IdcTipoEvento) REFERENCES [Base].[Clasificadores](Id)
);


