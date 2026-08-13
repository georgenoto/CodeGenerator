Sistema
│
├── Seguridad
│   ├── Usuarios
│   ├── Roles
│   └── Permisos
│
├── Ventas
│   ├── Facturación
│   ├── Cotizaciones
│   └── Clientes
│
├── Compras
│   ├── Proveedores
│   └── Órdenes de Compra
│
└── Inventario
    ├── Productos
    └── Almacenes


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

ALTER TABLE Usuarios
ADD CONSTRAINT UQ_Usuarios_Usuario
UNIQUE(Usuario);

ALTER TABLE Usuarios
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

ALTER TABLE Roles
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
    CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY(IdUsuario) REFERENCES Usuarios(Id),
    CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY(IdRol) REFERENCES Roles(Id)
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

ALTER TABLE Modulos
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
    CONSTRAINT FK_Opciones_Modulos FOREIGN KEY(IdModulo) REFERENCES Modulos(Id)
);

ALTER TABLE Opciones
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
    CONSTRAINT FK_Permisos_Opciones FOREIGN KEY(IdOpcion)  REFERENCES Opciones(Id)
);

ALTER TABLE Permisos
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
    CONSTRAINT FK_RolPermiso_Rol FOREIGN KEY(IdRol) REFERENCES Roles(Id),
    CONSTRAINT FK_RolPermiso_Permiso FOREIGN KEY(IdPermiso) REFERENCES Permisos(Id)
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
    CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY(IdUsuario) REFERENCES Usuarios(Id)
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
    CONSTRAINT FK_RefreshTokens_Usuario FOREIGN KEY(IdUsuario) REFERENCES Usuarios(Id)
);
GO