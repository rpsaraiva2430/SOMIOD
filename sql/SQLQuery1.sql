-- 1. APAGAR TABELAS ANTIGAS (Se existirem)
-- A ordem é importante por causa das chaves estrangeiras
IF OBJECT_ID('dbo.Subscriptions', 'U') IS NOT NULL DROP TABLE dbo.Subscriptions;
IF OBJECT_ID('dbo.ContentInstances', 'U') IS NOT NULL DROP TABLE dbo.ContentInstances;
IF OBJECT_ID('dbo.Containers', 'U') IS NOT NULL DROP TABLE dbo.Containers;
IF OBJECT_ID('dbo.Applications', 'U') IS NOT NULL DROP TABLE dbo.Applications;

-- Verifica se ficaram tabelas singulares antigas e limpa também, para garantir
IF OBJECT_ID('dbo.Subscription', 'U') IS NOT NULL DROP TABLE dbo.Subscription;
IF OBJECT_ID('dbo.ContentInstance', 'U') IS NOT NULL DROP TABLE dbo.ContentInstance;
IF OBJECT_ID('dbo.Container', 'U') IS NOT NULL DROP TABLE dbo.Container;
IF OBJECT_ID('dbo.Application', 'U') IS NOT NULL DROP TABLE dbo.Application;

-- 2. CRIAR TABELAS NOVAS (Nomes no Singular e colunas com Underscore)

-- Tabela Application
CREATE TABLE Application (
    id INT IDENTITY(1,1) PRIMARY KEY,
    resource_name NVARCHAR(50) UNIQUE NOT NULL,
    creation_datetime NVARCHAR(50) NOT NULL
);

-- Tabela Container
CREATE TABLE Container (
    id INT IDENTITY(1,1) PRIMARY KEY,
    resource_name NVARCHAR(50) NOT NULL,
    creation_datetime NVARCHAR(50) NOT NULL,
    application_id INT NOT NULL,
    FOREIGN KEY (application_id) REFERENCES Application(id) ON DELETE CASCADE,
    UNIQUE(resource_name, application_id)
);

-- Tabela ContentInstance
CREATE TABLE ContentInstance (
    id INT IDENTITY(1,1) PRIMARY KEY,
    resource_name NVARCHAR(50) NOT NULL,
    creation_datetime NVARCHAR(50) NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    content_type NVARCHAR(50) NOT NULL,
    container_id INT NOT NULL,
    FOREIGN KEY (container_id) REFERENCES Container(id) ON DELETE CASCADE
);

-- Tabela Subscription
CREATE TABLE Subscription (
    id INT IDENTITY(1,1) PRIMARY KEY,
    resource_name NVARCHAR(50) NOT NULL,
    creation_datetime NVARCHAR(50) NOT NULL,
    evt INT NOT NULL,
    endpoint NVARCHAR(200) NOT NULL,
    container_id INT NOT NULL,
    FOREIGN KEY (container_id) REFERENCES Container(id) ON DELETE CASCADE
);