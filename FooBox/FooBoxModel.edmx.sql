
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 09/20/2014 23:31:54
-- Generated from EDMX file: D:\projects\FooBox\FooBox\FooBoxModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [FooBoxDb];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_UserGroup_User]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[UserGroup] DROP CONSTRAINT [FK_UserGroup_User];
GO
IF OBJECT_ID(N'[dbo].[FK_UserGroup_Group]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[UserGroup] DROP CONSTRAINT [FK_UserGroup_Group];
GO
IF OBJECT_ID(N'[dbo].[FK_TokenUser]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Tokens] DROP CONSTRAINT [FK_TokenUser];
GO
IF OBJECT_ID(N'[dbo].[FK_FolderOwner]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Files_Folder] DROP CONSTRAINT [FK_FolderOwner];
GO
IF OBJECT_ID(N'[dbo].[FK_FolderEditor_Folder]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[FolderEditors] DROP CONSTRAINT [FK_FolderEditor_Folder];
GO
IF OBJECT_ID(N'[dbo].[FK_FolderEditor_Identity]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[FolderEditors] DROP CONSTRAINT [FK_FolderEditor_Identity];
GO
IF OBJECT_ID(N'[dbo].[FK_FolderFile_Folder]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[FolderFile] DROP CONSTRAINT [FK_FolderFile_Folder];
GO
IF OBJECT_ID(N'[dbo].[FK_FolderFile_File]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[FolderFile] DROP CONSTRAINT [FK_FolderFile_File];
GO
IF OBJECT_ID(N'[dbo].[FK_FolderShareFolder]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Files_Folder] DROP CONSTRAINT [FK_FolderShareFolder];
GO
IF OBJECT_ID(N'[dbo].[FK_UserClient]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Clients] DROP CONSTRAINT [FK_UserClient];
GO
IF OBJECT_ID(N'[dbo].[FK_ChangelistClient]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Changelists] DROP CONSTRAINT [FK_ChangelistClient];
GO
IF OBJECT_ID(N'[dbo].[FK_ChangeFile]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Changes] DROP CONSTRAINT [FK_ChangeFile];
GO
IF OBJECT_ID(N'[dbo].[FK_ChangelistChange]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Changes] DROP CONSTRAINT [FK_ChangelistChange];
GO
IF OBJECT_ID(N'[dbo].[FK_DocumentDocumentVersion]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[DocumentVersions] DROP CONSTRAINT [FK_DocumentDocumentVersion];
GO
IF OBJECT_ID(N'[dbo].[FK_DocumentVersionBlob]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[DocumentVersions] DROP CONSTRAINT [FK_DocumentVersionBlob];
GO
IF OBJECT_ID(N'[dbo].[FK_ChangeDocumentVersion]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Changes] DROP CONSTRAINT [FK_ChangeDocumentVersion];
GO
IF OBJECT_ID(N'[dbo].[FK_DocumentVersionClient]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[DocumentVersions] DROP CONSTRAINT [FK_DocumentVersionClient];
GO
IF OBJECT_ID(N'[dbo].[FK_User_inherits_Identity]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Identities_User] DROP CONSTRAINT [FK_User_inherits_Identity];
GO
IF OBJECT_ID(N'[dbo].[FK_Group_inherits_Identity]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Identities_Group] DROP CONSTRAINT [FK_Group_inherits_Identity];
GO
IF OBJECT_ID(N'[dbo].[FK_Folder_inherits_File]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Files_Folder] DROP CONSTRAINT [FK_Folder_inherits_File];
GO
IF OBJECT_ID(N'[dbo].[FK_Document_inherits_File]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Files_Document] DROP CONSTRAINT [FK_Document_inherits_File];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[Identities]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Identities];
GO
IF OBJECT_ID(N'[dbo].[Tokens]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Tokens];
GO
IF OBJECT_ID(N'[dbo].[Files]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Files];
GO
IF OBJECT_ID(N'[dbo].[Blobs]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Blobs];
GO
IF OBJECT_ID(N'[dbo].[Clients]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Clients];
GO
IF OBJECT_ID(N'[dbo].[Changelists]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Changelists];
GO
IF OBJECT_ID(N'[dbo].[Changes]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Changes];
GO
IF OBJECT_ID(N'[dbo].[DocumentVersions]', 'U') IS NOT NULL
    DROP TABLE [dbo].[DocumentVersions];
GO
IF OBJECT_ID(N'[dbo].[Identities_User]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Identities_User];
GO
IF OBJECT_ID(N'[dbo].[Identities_Group]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Identities_Group];
GO
IF OBJECT_ID(N'[dbo].[Files_Folder]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Files_Folder];
GO
IF OBJECT_ID(N'[dbo].[Files_Document]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Files_Document];
GO
IF OBJECT_ID(N'[dbo].[UserGroup]', 'U') IS NOT NULL
    DROP TABLE [dbo].[UserGroup];
GO
IF OBJECT_ID(N'[dbo].[FolderEditors]', 'U') IS NOT NULL
    DROP TABLE [dbo].[FolderEditors];
GO
IF OBJECT_ID(N'[dbo].[FolderFile]', 'U') IS NOT NULL
    DROP TABLE [dbo].[FolderFile];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Identities'
CREATE TABLE [dbo].[Identities] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [State] int  NOT NULL
);
GO

-- Creating table 'Tokens'
CREATE TABLE [dbo].[Tokens] (
    [Id] varchar(64)  NOT NULL,
    [ExpiryTime] datetime  NOT NULL,
    [UserId] bigint  NOT NULL
);
GO

-- Creating table 'Files'
CREATE TABLE [dbo].[Files] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [State] int  NOT NULL
);
GO

-- Creating table 'Blobs'
CREATE TABLE [dbo].[Blobs] (
    [Id] uniqueidentifier  NOT NULL,
    [Size] bigint  NOT NULL,
    [Hash] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'Clients'
CREATE TABLE [dbo].[Clients] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [State] int  NOT NULL,
    [UserId] bigint  NOT NULL
);
GO

-- Creating table 'Changelists'
CREATE TABLE [dbo].[Changelists] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [ClientId] bigint  NOT NULL
);
GO

-- Creating table 'Changes'
CREATE TABLE [dbo].[Changes] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [Type] int  NOT NULL,
    [ChangelistId] bigint  NOT NULL,
    [FileId] bigint  NOT NULL,
    [DocumentVersionId] bigint  NULL
);
GO

-- Creating table 'DocumentVersions'
CREATE TABLE [dbo].[DocumentVersions] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [TimeStamp] nvarchar(max)  NOT NULL,
    [DocumentId] bigint  NOT NULL,
    [BlobId] uniqueidentifier  NOT NULL,
    [ClientId] bigint  NOT NULL
);
GO

-- Creating table 'Identities_User'
CREATE TABLE [dbo].[Identities_User] (
    [PasswordHash] nvarchar(max)  NOT NULL,
    [PasswordSalt] nvarchar(max)  NOT NULL,
    [FirstName] nvarchar(max)  NOT NULL,
    [LastName] nvarchar(max)  NOT NULL,
    [QuotaLimit] bigint  NOT NULL,
    [QuotaCharged] bigint  NOT NULL,
    [Id] bigint  NOT NULL
);
GO

-- Creating table 'Identities_Group'
CREATE TABLE [dbo].[Identities_Group] (
    [Description] nvarchar(max)  NOT NULL,
    [IsAdmin] bit  NOT NULL,
    [Id] bigint  NOT NULL
);
GO

-- Creating table 'Files_Folder'
CREATE TABLE [dbo].[Files_Folder] (
    [Id] bigint  NOT NULL,
    [Owner_Id] bigint  NOT NULL,
    [ShareFolder_Id] bigint  NULL
);
GO

-- Creating table 'Files_Document'
CREATE TABLE [dbo].[Files_Document] (
    [Id] bigint  NOT NULL
);
GO

-- Creating table 'UserGroup'
CREATE TABLE [dbo].[UserGroup] (
    [Users_Id] bigint  NOT NULL,
    [Groups_Id] bigint  NOT NULL
);
GO

-- Creating table 'FolderEditors'
CREATE TABLE [dbo].[FolderEditors] (
    [FolderEditor_Identity_Id] bigint  NOT NULL,
    [Editors_Id] bigint  NOT NULL
);
GO

-- Creating table 'FolderFile'
CREATE TABLE [dbo].[FolderFile] (
    [ParentFolders_Id] bigint  NOT NULL,
    [Files_Id] bigint  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'Identities'
ALTER TABLE [dbo].[Identities]
ADD CONSTRAINT [PK_Identities]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Tokens'
ALTER TABLE [dbo].[Tokens]
ADD CONSTRAINT [PK_Tokens]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Files'
ALTER TABLE [dbo].[Files]
ADD CONSTRAINT [PK_Files]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Blobs'
ALTER TABLE [dbo].[Blobs]
ADD CONSTRAINT [PK_Blobs]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Clients'
ALTER TABLE [dbo].[Clients]
ADD CONSTRAINT [PK_Clients]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Changelists'
ALTER TABLE [dbo].[Changelists]
ADD CONSTRAINT [PK_Changelists]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Changes'
ALTER TABLE [dbo].[Changes]
ADD CONSTRAINT [PK_Changes]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'DocumentVersions'
ALTER TABLE [dbo].[DocumentVersions]
ADD CONSTRAINT [PK_DocumentVersions]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Identities_User'
ALTER TABLE [dbo].[Identities_User]
ADD CONSTRAINT [PK_Identities_User]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Identities_Group'
ALTER TABLE [dbo].[Identities_Group]
ADD CONSTRAINT [PK_Identities_Group]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Files_Folder'
ALTER TABLE [dbo].[Files_Folder]
ADD CONSTRAINT [PK_Files_Folder]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Files_Document'
ALTER TABLE [dbo].[Files_Document]
ADD CONSTRAINT [PK_Files_Document]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Users_Id], [Groups_Id] in table 'UserGroup'
ALTER TABLE [dbo].[UserGroup]
ADD CONSTRAINT [PK_UserGroup]
    PRIMARY KEY CLUSTERED ([Users_Id], [Groups_Id] ASC);
GO

-- Creating primary key on [FolderEditor_Identity_Id], [Editors_Id] in table 'FolderEditors'
ALTER TABLE [dbo].[FolderEditors]
ADD CONSTRAINT [PK_FolderEditors]
    PRIMARY KEY CLUSTERED ([FolderEditor_Identity_Id], [Editors_Id] ASC);
GO

-- Creating primary key on [ParentFolders_Id], [Files_Id] in table 'FolderFile'
ALTER TABLE [dbo].[FolderFile]
ADD CONSTRAINT [PK_FolderFile]
    PRIMARY KEY CLUSTERED ([ParentFolders_Id], [Files_Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [Users_Id] in table 'UserGroup'
ALTER TABLE [dbo].[UserGroup]
ADD CONSTRAINT [FK_UserGroup_User]
    FOREIGN KEY ([Users_Id])
    REFERENCES [dbo].[Identities_User]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Groups_Id] in table 'UserGroup'
ALTER TABLE [dbo].[UserGroup]
ADD CONSTRAINT [FK_UserGroup_Group]
    FOREIGN KEY ([Groups_Id])
    REFERENCES [dbo].[Identities_Group]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_UserGroup_Group'
CREATE INDEX [IX_FK_UserGroup_Group]
ON [dbo].[UserGroup]
    ([Groups_Id]);
GO

-- Creating foreign key on [UserId] in table 'Tokens'
ALTER TABLE [dbo].[Tokens]
ADD CONSTRAINT [FK_TokenUser]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Identities_User]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_TokenUser'
CREATE INDEX [IX_FK_TokenUser]
ON [dbo].[Tokens]
    ([UserId]);
GO

-- Creating foreign key on [Owner_Id] in table 'Files_Folder'
ALTER TABLE [dbo].[Files_Folder]
ADD CONSTRAINT [FK_FolderOwner]
    FOREIGN KEY ([Owner_Id])
    REFERENCES [dbo].[Identities_User]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_FolderOwner'
CREATE INDEX [IX_FK_FolderOwner]
ON [dbo].[Files_Folder]
    ([Owner_Id]);
GO

-- Creating foreign key on [FolderEditor_Identity_Id] in table 'FolderEditors'
ALTER TABLE [dbo].[FolderEditors]
ADD CONSTRAINT [FK_FolderEditor_Folder]
    FOREIGN KEY ([FolderEditor_Identity_Id])
    REFERENCES [dbo].[Files_Folder]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Editors_Id] in table 'FolderEditors'
ALTER TABLE [dbo].[FolderEditors]
ADD CONSTRAINT [FK_FolderEditor_Identity]
    FOREIGN KEY ([Editors_Id])
    REFERENCES [dbo].[Identities]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_FolderEditor_Identity'
CREATE INDEX [IX_FK_FolderEditor_Identity]
ON [dbo].[FolderEditors]
    ([Editors_Id]);
GO

-- Creating foreign key on [ParentFolders_Id] in table 'FolderFile'
ALTER TABLE [dbo].[FolderFile]
ADD CONSTRAINT [FK_FolderFile_Folder]
    FOREIGN KEY ([ParentFolders_Id])
    REFERENCES [dbo].[Files_Folder]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Files_Id] in table 'FolderFile'
ALTER TABLE [dbo].[FolderFile]
ADD CONSTRAINT [FK_FolderFile_File]
    FOREIGN KEY ([Files_Id])
    REFERENCES [dbo].[Files]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_FolderFile_File'
CREATE INDEX [IX_FK_FolderFile_File]
ON [dbo].[FolderFile]
    ([Files_Id]);
GO

-- Creating foreign key on [ShareFolder_Id] in table 'Files_Folder'
ALTER TABLE [dbo].[Files_Folder]
ADD CONSTRAINT [FK_FolderShareFolder]
    FOREIGN KEY ([ShareFolder_Id])
    REFERENCES [dbo].[Files_Folder]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_FolderShareFolder'
CREATE INDEX [IX_FK_FolderShareFolder]
ON [dbo].[Files_Folder]
    ([ShareFolder_Id]);
GO

-- Creating foreign key on [UserId] in table 'Clients'
ALTER TABLE [dbo].[Clients]
ADD CONSTRAINT [FK_UserClient]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Identities_User]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_UserClient'
CREATE INDEX [IX_FK_UserClient]
ON [dbo].[Clients]
    ([UserId]);
GO

-- Creating foreign key on [ClientId] in table 'Changelists'
ALTER TABLE [dbo].[Changelists]
ADD CONSTRAINT [FK_ChangelistClient]
    FOREIGN KEY ([ClientId])
    REFERENCES [dbo].[Clients]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ChangelistClient'
CREATE INDEX [IX_FK_ChangelistClient]
ON [dbo].[Changelists]
    ([ClientId]);
GO

-- Creating foreign key on [FileId] in table 'Changes'
ALTER TABLE [dbo].[Changes]
ADD CONSTRAINT [FK_ChangeFile]
    FOREIGN KEY ([FileId])
    REFERENCES [dbo].[Files]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ChangeFile'
CREATE INDEX [IX_FK_ChangeFile]
ON [dbo].[Changes]
    ([FileId]);
GO

-- Creating foreign key on [ChangelistId] in table 'Changes'
ALTER TABLE [dbo].[Changes]
ADD CONSTRAINT [FK_ChangelistChange]
    FOREIGN KEY ([ChangelistId])
    REFERENCES [dbo].[Changelists]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ChangelistChange'
CREATE INDEX [IX_FK_ChangelistChange]
ON [dbo].[Changes]
    ([ChangelistId]);
GO

-- Creating foreign key on [DocumentId] in table 'DocumentVersions'
ALTER TABLE [dbo].[DocumentVersions]
ADD CONSTRAINT [FK_DocumentDocumentVersion]
    FOREIGN KEY ([DocumentId])
    REFERENCES [dbo].[Files_Document]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DocumentDocumentVersion'
CREATE INDEX [IX_FK_DocumentDocumentVersion]
ON [dbo].[DocumentVersions]
    ([DocumentId]);
GO

-- Creating foreign key on [BlobId] in table 'DocumentVersions'
ALTER TABLE [dbo].[DocumentVersions]
ADD CONSTRAINT [FK_DocumentVersionBlob]
    FOREIGN KEY ([BlobId])
    REFERENCES [dbo].[Blobs]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DocumentVersionBlob'
CREATE INDEX [IX_FK_DocumentVersionBlob]
ON [dbo].[DocumentVersions]
    ([BlobId]);
GO

-- Creating foreign key on [DocumentVersionId] in table 'Changes'
ALTER TABLE [dbo].[Changes]
ADD CONSTRAINT [FK_ChangeDocumentVersion]
    FOREIGN KEY ([DocumentVersionId])
    REFERENCES [dbo].[DocumentVersions]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ChangeDocumentVersion'
CREATE INDEX [IX_FK_ChangeDocumentVersion]
ON [dbo].[Changes]
    ([DocumentVersionId]);
GO

-- Creating foreign key on [ClientId] in table 'DocumentVersions'
ALTER TABLE [dbo].[DocumentVersions]
ADD CONSTRAINT [FK_DocumentVersionClient]
    FOREIGN KEY ([ClientId])
    REFERENCES [dbo].[Clients]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DocumentVersionClient'
CREATE INDEX [IX_FK_DocumentVersionClient]
ON [dbo].[DocumentVersions]
    ([ClientId]);
GO

-- Creating foreign key on [Id] in table 'Identities_User'
ALTER TABLE [dbo].[Identities_User]
ADD CONSTRAINT [FK_User_inherits_Identity]
    FOREIGN KEY ([Id])
    REFERENCES [dbo].[Identities]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Id] in table 'Identities_Group'
ALTER TABLE [dbo].[Identities_Group]
ADD CONSTRAINT [FK_Group_inherits_Identity]
    FOREIGN KEY ([Id])
    REFERENCES [dbo].[Identities]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Id] in table 'Files_Folder'
ALTER TABLE [dbo].[Files_Folder]
ADD CONSTRAINT [FK_Folder_inherits_File]
    FOREIGN KEY ([Id])
    REFERENCES [dbo].[Files]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Id] in table 'Files_Document'
ALTER TABLE [dbo].[Files_Document]
ADD CONSTRAINT [FK_Document_inherits_File]
    FOREIGN KEY ([Id])
    REFERENCES [dbo].[Files]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------