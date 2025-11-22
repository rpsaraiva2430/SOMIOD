-- =============================================
-- 1. FORÇAR A REMOÇÃO DAS TABELAS ANTIGAS
-- A ordem é crítica: Filhos primeiro, Pais depois
-- =============================================

-- Remove tabela Subscription se existir
IF OBJECT_ID('dbo.Subscription', 'U') IS NOT NULL 
   DROP TABLE dbo.Subscription;

-- Remove tabela ContentInstance se existir
IF OBJECT_ID('dbo.ContentInstance', 'U') IS NOT NULL 
   DROP TABLE dbo.ContentInstance;

-- Remove tabela Container se existir
IF OBJECT_ID('dbo.Container', 'U') IS NOT NULL 
   DROP TABLE dbo.Container;

-- Remove tabela Application se existir
IF OBJECT_ID('dbo.Application', 'U') IS NOT NULL 
   DROP TABLE dbo.Application;

-- =============================================
-- 2. CRIAR A NOVA ESTRUTURA "PURE NAMES" (SEM IDs)
-- =============================================

-- Tabela Application
-- Chave Primária: resource_name
CREATE TABLE Application (
    resource_name NVARCHAR(50) NOT NULL PRIMARY KEY,
    creation_datetime NVARCHAR(50) NOT NULL
);

-- Tabela Container
-- Chave Primária Composta: (resource_name + parent_app_name)
CREATE TABLE Container (
    resource_name NVARCHAR(50) NOT NULL,
    creation_datetime NVARCHAR(50) NOT NULL,
    parent_app_name NVARCHAR(50) NOT NULL,
    
    -- Define que a chave única deste contentor é o Nome + App Pai
    PRIMARY KEY (resource_name, parent_app_name),
    
    -- Liga ao nome da aplicação
    FOREIGN KEY (parent_app_name) 
        REFERENCES Application(resource_name) ON DELETE CASCADE
);

-- Tabela ContentInstance
-- Chave Primária Composta: (resource_name + parent_container + parent_app)
CREATE TABLE ContentInstance (
    resource_name NVARCHAR(50) NOT NULL,
    creation_datetime NVARCHAR(50) NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    content_type NVARCHAR(50) NOT NULL,
    parent_container_name NVARCHAR(50) NOT NULL,
    parent_app_name NVARCHAR(50) NOT NULL,

    PRIMARY KEY (resource_name, parent_container_name, parent_app_name),
    
    -- A chave estrangeira tem de incluir AMBOS os campos da chave primária do Container
    FOREIGN KEY (parent_container_name, parent_app_name) 
        REFERENCES Container(resource_name, parent_app_name) ON DELETE CASCADE
);

-- Tabela Subscription
-- Chave Primária Composta: (resource_name + parent_container + parent_app)
CREATE TABLE Subscription (
    resource_name NVARCHAR(50) NOT NULL,
    creation_datetime NVARCHAR(50) NOT NULL,
    evt INT NOT NULL,
    endpoint NVARCHAR(200) NOT NULL,
    parent_container_name NVARCHAR(50) NOT NULL,
    parent_app_name NVARCHAR(50) NOT NULL,

    PRIMARY KEY (resource_name, parent_container_name, parent_app_name),
    
    FOREIGN KEY (parent_container_name, parent_app_name) 
        REFERENCES Container(resource_name, parent_app_name) ON DELETE CASCADE
);

-- =============================================
-- 3. INSERIR DADOS DE TESTE (Seed Data)
-- =============================================

-- 1. Criar App
INSERT INTO Application (resource_name, creation_datetime) 
VALUES ('smart-home', '2025-11-22T20:00:00');

-- 2. Criar Contentor (ligado à app 'smart-home')
INSERT INTO Container (resource_name, creation_datetime, parent_app_name) 
VALUES ('kitchen', '2025-11-22T20:05:00', 'smart-home');

-- 3. Criar ContentInstance (ligada ao contentor 'kitchen' da app 'smart-home')
INSERT INTO ContentInstance (resource_name, creation_datetime, content, content_type, parent_container_name, parent_app_name)
VALUES ('temp-reading', '2025-11-22T20:10:00', '25C', 'text/plain', 'kitchen', 'smart-home');

-- 4. Criar Subscription
INSERT INTO Subscription (resource_name, creation_datetime, evt, endpoint, parent_container_name, parent_app_name)
VALUES ('sub1', '2025-11-22T20:15:00', 1, 'mqtt://127.0.0.1', 'kitchen', 'smart-home');