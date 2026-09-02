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
    id INT IDENTITY(1,1) PRIMARY KEY,
    usuario VARCHAR(50) NOT NULL,
    correo VARCHAR(150) NOT NULL,
    passwordHash VARCHAR(500) NOT NULL,
    nombreCompleto VARCHAR(200) NOT NULL,
    activo BIT NOT NULL DEFAULT 1,
    fechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    fechaActualizacion DATETIME NULL,
    ultimoAcceso DATETIME NULL
);

ALTER TABLE [Seguridad].[Usuarios]
ADD CONSTRAINT UQ_Usuarios_Usuario
UNIQUE(usuario);

ALTER TABLE [Seguridad].[Usuarios]
ADD CONSTRAINT UQ_Usuarios_Correo
UNIQUE(correo);


-- ============================================================
-- ROLES
-- ============================================================
CREATE TABLE [Seguridad].[Roles]
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    descripcion VARCHAR(250),
    activo BIT NOT NULL DEFAULT 1,
    fechaCreacion DATETIME NOT NULL DEFAULT GETDATE()
);

ALTER TABLE [Seguridad].[Roles]
ADD CONSTRAINT UQ_Roles_Nombre
UNIQUE(nombre);


-- ============================================================
-- USUARIO - ROL
-- ============================================================
CREATE TABLE [Seguridad].[UsuarioRol]
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    idUsuario INT NOT NULL,
    idRol INT NOT NULL,
    fechaAsignacion DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY(idUsuario) REFERENCES [Seguridad].[Usuarios](id),
    CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY(idRol) REFERENCES [Seguridad].[Roles](id)
);


-- ============================================================
-- MODULOS
-- ============================================================
CREATE TABLE [Seguridad].[Modulos]
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    codigo VARCHAR(50) NOT NULL,
    descripcion VARCHAR(250) NULL,
    ruta VARCHAR(250) NULL,
    icono VARCHAR(100) NULL,
    ordenMenu INT NOT NULL DEFAULT 0,
    visibleMenu BIT NOT NULL DEFAULT 1,
    activo BIT NOT NULL DEFAULT 1
);

ALTER TABLE [Seguridad].[Modulos]
ADD CONSTRAINT UQ_Modulos_Codigo
UNIQUE(codigo);



-- ============================================================
-- OPCIONES
-- Pantallas o funcionalidades dentro de un módulo
-- ============================================================
CREATE TABLE [Seguridad].[Opciones]
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    idModulo INT NOT NULL,
    nombre VARCHAR(150) NOT NULL,
    codigo VARCHAR(100) NOT NULL,
    descripcion VARCHAR(250) NULL,
    ruta VARCHAR(250) NULL,
    icono VARCHAR(100) NULL,
    ordenMenu INT NOT NULL DEFAULT 0,
    visibleMenu BIT NOT NULL DEFAULT 1,
    activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Opciones_Modulos FOREIGN KEY(idModulo) REFERENCES [Seguridad].[Modulos](id)
);

ALTER TABLE [Seguridad].[Opciones]
ADD CONSTRAINT UQ_Opciones_Codigo
UNIQUE(codigo);


-- ============================================================
-- PERMISOS
-- ============================================================
CREATE TABLE [Seguridad].[Permisos]
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    idOpcion INT NOT NULL,
    codigo VARCHAR(150) NOT NULL,
    nombre VARCHAR(150) NOT NULL,
    descripcion VARCHAR(250) NULL,
    activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Permisos_Opciones FOREIGN KEY(idOpcion)  REFERENCES [Seguridad].[Opciones](id)
);

ALTER TABLE [Seguridad].[Permisos]
ADD CONSTRAINT UQ_Permisos_Codigo
UNIQUE(codigo);


-- ============================================================
-- ROL - PERMISO
-- ============================================================
CREATE TABLE [Seguridad].[RolPermiso]
(
    id INT IDENTITY(1,1) PRIMARY KEY,
    idRol INT NOT NULL,
    idPermiso INT NOT NULL,
    fechaAsignacion DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_RolPermiso_Rol FOREIGN KEY(idRol) REFERENCES [Seguridad].[Roles](id),
    CONSTRAINT FK_RolPermiso_Permiso FOREIGN KEY(idPermiso) REFERENCES [Seguridad].[Permisos](id)
);


-- ============================================================
-- AUDITORIA
-- ============================================================
CREATE TABLE [Seguridad].[Auditoria]
(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    idUsuario INT NOT NULL,
    tabla VARCHAR(100) NOT NULL,
    accion VARCHAR(50) NOT NULL,
    registroId VARCHAR(100) NOT NULL,
    datosAnteriores NVARCHAR(MAX) NULL,
    datosNuevos NVARCHAR(MAX) NULL,
    fecha DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY(idUsuario) REFERENCES [Seguridad].[Usuarios](id)
);


-- ============================================================
-- REFRESH TOKENS
-- ============================================================
CREATE TABLE [Seguridad].[RefreshTokens]
(
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    idUsuario INT NOT NULL,
    token NVARCHAR(500) NOT NULL,
    fechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    fechaExpiracion DATETIME NOT NULL,
    revocado BIT NOT NULL DEFAULT 0,
    fechaRevocacion DATETIME NULL,
    CONSTRAINT FK_RefreshTokens_Usuario FOREIGN KEY(idUsuario) REFERENCES [Seguridad].[Usuarios](id)
);
GO
CREATE TABLE [Base].[TipoParametros] (
   id INT IDENTITY(1,1) PRIMARY KEY,
   descripcion VARCHAR(300),
   orden INT,
   estado bit
);
GO
CREATE TABLE [Base].[Parametros](
	id INT IDENTITY(1,1) PRIMARY KEY,
	idTipoParametro INT NOT NULL,
	descripcion VARCHAR(300),
	abreviacion VARCHAR(50),
	codigo VARCHAR(10),
	orden INT,
	estado bit,
	CONSTRAINT FK_TipoParametro FOREIGN KEY (idTipoParametro) REFERENCES [Base].[TipoParametros](id)
)
GO
CREATE TABLE [Base].[Temporadas](
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(20),
    fechaInicio DATE,
    fechaFin DATE
)
GO
-- 2. Tabla de Ligas
CREATE TABLE [Base].[Ligas] (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombreLiga VARCHAR(100) NOT NULL ,
    pais VARCHAR(50) NOT NULL,
	tipo VARCHAR(50)
);

-- 3. Tabla de Equipos
CREATE TABLE [Base].[Equipos] (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombreEquipo VARCHAR(100) NOT NULL,
	abreviatura [varchar](20) NULL,
	pais VARCHAR(50) NOT NULL,
);

CREATE TABLE [Base].LigaEquipos(
    id INT IDENTITY(1,1) PRIMARY KEY,
    idLiga INT,
    idEquipo INT,
    idTemporada INT,
	CONSTRAINT FK_LE_Ligas FOREIGN KEY (idLiga) REFERENCES [Base].[Ligas](id),
	CONSTRAINT FK_LE_Equipos FOREIGN KEY (idEquipo) REFERENCES [Base].[Equipos](id),
	CONSTRAINT FK_LE_Temporadas FOREIGN KEY (idTemporada) REFERENCES [Base].[Temporadas](id),
)
-- 4. Tabla de Partidos
CREATE TABLE [Metrica].[Partidos] (
    id INT IDENTITY(1,1) PRIMARY KEY,
	jornada INT NOT NULL,
    idLiga INT NOT NULL,
    idLocal INT NOT NULL,
    idVisitante INT NOT NULL,
    fecha DATETIME NOT NULL,
    idTemporada INT NOT NULL,
	idpEstado INT NOT NULL,
    CONSTRAINT FK_Partidos_Ligas FOREIGN KEY (idLiga) REFERENCES [Base].[Ligas](id),
    CONSTRAINT FK_Partidos_Local FOREIGN KEY (idLocal) REFERENCES [Base].[Equipos](id),
    CONSTRAINT FK_Partidos_Visitante FOREIGN KEY (idVisitante) REFERENCES [Base].[Equipos](id),
	CONSTRAINT FK_Temporada FOREIGN KEY (idTemporada) REFERENCES [Base].[Temporadas](id),
	CONSTRAINT FK_EstadoPartido FOREIGN KEY (idpEstado) REFERENCES [Base].[Parametros](id)
);

-- 5. Tabla de Estadísticas de Partido (Totales por equipo)
CREATE TABLE [Metrica].[EstadisticasPartidos] (
    id INT IDENTITY(1,1) PRIMARY KEY,
    idPartido INT NOT NULL,
    idEquipo INT NOT NULL,
    idpCondicion INT NOT NULL, -- 'Local', 'Visitante'
    posesion DECIMAL(5,2), -- Permite guardar porcentajes con decimales ej: 55.40
	golesPrimerTiempo INT DEFAULT 0,
	golesSegundoTiempo INT DEFAULT 0,
	totalGoles INT DEFAULT 0,
    tirosArco INT DEFAULT 0,
    corners INT DEFAULT 0,
	offsides INT DEFAULT 0,
	faltas INT,
    tarjetasAmarillas INT,
    tarjetasRojas INT,
    idpResultado INT NOT NULL, -- 'Ganador','Perdido', 'Empate'	
    CONSTRAINT FK_Estadisticas_Partidos FOREIGN KEY (idPartido) REFERENCES [Metrica].[Partidos](id) ON DELETE CASCADE,
    CONSTRAINT FK_Estadisticas_Equipos FOREIGN KEY (idEquipo) REFERENCES [Base].[Equipos](id),
	CONSTRAINT FK_Condicion_Equipo FOREIGN KEY (idpCondicion) REFERENCES [Base].[Parametros](id),
	CONSTRAINT FK_Resultado_Equipo FOREIGN KEY (idpResultado) REFERENCES [Base].[Parametros](id),
);



go
--- DATOS BASES
INSERT INTO [Seguridad].[Usuarios] VALUES ('gnoto','georgenoto@gmail.com','gnoto1986*','George Noto Issa',1,GETDATE(),GETDATE(),GETDATE())
INSERT INTO [Seguridad].[Usuarios] VALUES ('tenoto','tenoto@gmail.com','gnoto1986*','Thiago Emiliano Noto Vasquez',1,GETDATE(),GETDATE(),GETDATE())
GO
INSERT INTO [Seguridad].[Roles] VALUES ('Admin','Administrador total del sistema',1,GETDATE())
INSERT INTO [Seguridad].[Roles] VALUES ('Seguridad','Administrador de app ',1,GETDATE())
INSERT INTO [Seguridad].[Roles] VALUES ('UserFutbol','Usuario para ingresar datos futbol ',1,GETDATE())
GO

INSERT INTO [Seguridad].[UsuarioRol] VALUES (1,1,GETDATE())
INSERT INTO [Seguridad].[UsuarioRol] VALUES (2,2,GETDATE())

GO

INSERT INTO [Seguridad].[Modulos] VALUES ('Seguridad','SEGURIDAD','','/Seguridad','',1,1,1)
INSERT INTO [Seguridad].[Modulos] VALUES ('Base','BASE','','/Base','',2,1,1)
INSERT INTO [Seguridad].[Modulos] VALUES ('Metrica','METRICA','','/Metrica','',3,1,1)
GO
INSERT INTO [Seguridad].[Opciones] VALUES (1,'Usuarios','USUARIOS','','/Usuarios','bi bi-table',1,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (1,'Roles','ROLES','','/Roles','bi bi-table',2,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (1,'Rol Por Usuario','USUARIOSROL','','/UsuarioRol','bi bi-table',3,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (1,'Modulos del Sistema','MODULOS','','/Modulos','bi bi-table',4,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (1,'Permisos','PERMISOS','','/Permisos','bi bi-table',6,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (1,'Permisos Por Rol','ROLPERMISO','','/RolPermiso','bi bi-table',7,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (1,'Opciones Por Modulo','OPCIONES','Tabla sirve para el menu dinamico','/Opciones','bi bi-table',5,1,1)

INSERT INTO [Seguridad].[Opciones] VALUES (2,'Tipo Parametros','TIPOPARAMETROS','','/TipoParametros','bi bi-table',1,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (2,'Parametros','PARAMETROS','','/Parametros','bi bi-table',2,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (2,'Equipos','EQUIPOS','','/Equipos','bi bi-table',3,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (2,'Liga','LIGAS','','/Ligas','bi bi-table',4,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (2,'Liga y Equipo','LIGAEQUIPO','','/LigaEquipos','bi bi-table',5,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (2,'Temporada','TEMPORADAS','','/Temporadas','bi bi-table',6,1,1)

INSERT INTO [Seguridad].[Opciones] VALUES (3,'Partidos','PARTIDOS','','/Partidos','bi bi-table',1,1,1)
INSERT INTO [Seguridad].[Opciones] VALUES (3,'Estadisticas Por Partido','ESTADISTICASPARTIDO','','/EstadisticasPartido','bi bi-table',2,1,1)

GO
INSERT INTO [Seguridad].[Permisos] VALUES (1,'/Usuarios/Index','','USUARIOS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (1,'/Usuarios/Create','','USUARIOS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (1,'/Usuarios/Edit','','USUARIOS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (1,'/Usuarios/Delete','','USUARIOS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (1,'/Usuarios/Details','','USUARIOS_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (2,'/Roles/Index','','ROLES_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (2,'/Roles/Create','','ROLES_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (2,'/Roles/Edit','','ROLES_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (2,'/Roles/Delete','','ROLES_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (2,'/Roles/Details','','ROLES_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (3,'/UsuarioRol/Index','','USUARIOROL_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (3,'/UsuarioRol/Create','','USUARIOROL_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (3,'/UsuarioRol/Edit','','USUARIOROL_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (3,'/UsuarioRol/Delete','','USUARIOROL_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (3,'/UsuarioRol/Details','','USUARIOROL_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (4,'/Modulos/Index','','MODULOS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (4,'/Modulos/Create','','MODULOS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (4,'/Modulos/Edit','','MODULOS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (4,'/Modulos/Delete','','MODULOS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (4,'/Modulos/Details','','MODULOS_DETALLE',1)


INSERT INTO [Seguridad].[Permisos] VALUES (5,'/Permisos/Index','','PERMISOS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (5,'/Permisos/Create','','PERMISOS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (5,'/Permisos/Edit','','PERMISOS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (5,'/Permisos/Delete','','PERMISOS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (5,'/Permisos/Details','','PERMISOS_DETALLE',1)


INSERT INTO [Seguridad].[Permisos] VALUES (6,'/RolPermiso/Index','','ROLPERMISO_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (6,'/RolPermiso/Create','','ROLPERMISO_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (6,'/RolPermiso/Edit','','ROLPERMISO_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (6,'/RolPermiso/Delete','','ROLPERMISO_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (6,'/RolPermiso/Details','','ROLPERMISO_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (7,'/Opciones/Index','','OPCIONES_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (7,'/Opciones/Create','','OPCIONES_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (7,'/Opciones/Edit','','OPCIONES_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (7,'/Opciones/Delete','','OPCIONES_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (7,'/Opciones/Details','','OPCIONES_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (8,'/TipoParametros/Index','','TIPOPARAMETROS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (8,'/TipoParametros/Create','','TIPOPARAMETROS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (8,'/TipoParametros/Edit','','TIPOPARAMETROS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (8,'/TipoParametros/Delete','','TIPOPARAMETROS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (8,'/TipoParametros/Details','','TIPOPARAMETROS_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (9,'/Parametros/Index','','PARAMETROS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (9,'/Parametros/Create','','PARAMETROS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (9,'/Parametros/Edit','','PARAMETROS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (9,'/Parametros/Delete','','PARAMETROS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (9,'/Parametros/Details','','PARAMETROS_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (10,'/Equipos/Index','','EQUIPOS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (10,'/Equipos/Create','','EQUIPOS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (10,'/Equipos/Edit','','EQUIPOS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (10,'/Equipos/Delete','','EQUIPOS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (10,'/Equipos/Details','','EQUIPOS_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (11,'/Ligas/Index','','LIGAS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (11,'/Ligas/Create','','LIGAS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (11,'/Ligas/Edit','','LIGAS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (11,'/Ligas/Delete','','LIGAS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (11,'/Ligas/Details','','LIGAS_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (12,'/LigaEquipos/Index','','LIGAEQUIPOS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (12,'/LigaEquipos/Create','','LIGAEQUIPOS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (12,'/LigaEquipos/Edit','','LIGAEQUIPOS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (12,'/LigaEquipos/Delete','','LIGAEQUIPOS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (12,'/LigaEquipos/Details','','LIGAEQUIPOS_DETALLE',1)


INSERT INTO [Seguridad].[Permisos] VALUES (13,'/Temporadas/Index','','TEMPORADAS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (13,'/Temporadas/Create','','TEMPORADAS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (13,'/Temporadas/Edit','','TEMPORADAS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (13,'/Temporadas/Delete','','TEMPORADAS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (13,'/Temporadas/Details','','TEMPORADAS_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (14,'/Partidos/Index','','PARTIDOS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (14,'/Partidos/Create','','PARTIDOS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (14,'/Partidos/Edit','','PARTIDOS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (14,'/Partidos/Delete','','PARTIDOS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (14,'/Partidos/Details','','PARTIDOS_DETALLE',1)

INSERT INTO [Seguridad].[Permisos] VALUES (15,'/EstadisticasPartidos/Index','','ESTADISTICASPARTIDOS_VER',1)
INSERT INTO [Seguridad].[Permisos] VALUES (15,'/EstadisticasPartidos/Create','','ESTADISTICASPARTIDOS_CREAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (15,'/EstadisticasPartidos/Edit','','ESTADISTICASPARTIDOS_EDITAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (15,'/EstadisticasPartidos/Delete','','ESTADISTICASPARTIDOS_ELIMINAR',1)
INSERT INTO [Seguridad].[Permisos] VALUES (15,'/EstadisticasPartidos/Details','','ESTADISTICASPARTIDOS_DETALLE',1)

GO

INSERT INTO [Seguridad].[RolPermiso] VALUES(1,1,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,2,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,3,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,4,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,5,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,6,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,7,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,8,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,9,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,10,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,11,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,12,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,13,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,14,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,15,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,16,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,17,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,18,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,19,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,20,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,21,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,22,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,23,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,24,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,25,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,26,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,27,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,28,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,29,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,30,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,31,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,32,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,33,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,34,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,35,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,36,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,37,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,38,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,39,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,40,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,41,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,42,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,43,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,44,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,45,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,46,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,47,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,48,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,49,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,50,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,51,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,52,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,53,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,54,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,55,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,56,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,57,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,58,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,59,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,60,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,61,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,62,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,63,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,64,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,65,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,66,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,67,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,68,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,69,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,70,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,71,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,72,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,73,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,74,GETDATE())
INSERT INTO [Seguridad].[RolPermiso] VALUES(1,75,GETDATE())

GO

INSERT INTO [Base].[TipoParametros] VALUES ('Condicion Equipo Partido',1,1)
INSERT INTO [Base].[TipoParametros] VALUES ('Resultado Equipo Partido',2,1)

GO

INSERT INTO [Base].[Parametros] VALUES (1,'Local','Juega de Local','LOC',1,1)
INSERT INTO [Base].[Parametros] VALUES (1,'Visitante','Juega de Visitante','VIS',2,1)

INSERT INTO [Base].[Parametros] VALUES (2,'Ganador','Ganador','GAN',1,1)
INSERT INTO [Base].[Parametros] VALUES (2,'Perdido','Perdido','PER',1,1)
INSERT INTO [Base].[Parametros] VALUES (2,'Empate','Empate','EMP',1,1)
