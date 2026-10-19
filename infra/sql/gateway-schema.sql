IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE TABLE [ExportJobs] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [RequesterOid] uniqueidentifier NOT NULL,
        [IdempotencyKey] uniqueidentifier NOT NULL,
        [RequestDigest] varchar(64) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [SourceKind] int NOT NULL,
        [Mode] int NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [State] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [AvailableAt] datetimeoffset NOT NULL,
        [Fence] bigint NOT NULL,
        [AttemptCount] int NOT NULL,
        [LeaseOwner] nvarchar(100) NULL,
        [LeaseToken] uniqueidentifier NULL,
        [LeaseExpiresAt] datetimeoffset NULL,
        [SafeErrorCode] nvarchar(80) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ExportJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ExportJobs_Digest] CHECK (LEN([RequestDigest]) = 64),
        CONSTRAINT [CK_ExportJobs_Lease] CHECK (([LeaseToken] IS NULL AND [LeaseOwner] IS NULL AND [LeaseExpiresAt] IS NULL) OR ([LeaseToken] IS NOT NULL AND [LeaseOwner] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL)),
        CONSTRAINT [CK_ExportJobs_SafeStates] CHECK ([State] IN (0,1,2,3,4,6,7,9))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE TABLE [AuditEvents] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [JobId] uniqueidentifier NULL,
        [ActorOid] uniqueidentifier NULL,
        [EventType] nvarchar(80) NOT NULL,
        [At] datetimeoffset NOT NULL,
        [CorrelationId] uniqueidentifier NOT NULL,
        [Mode] int NOT NULL,
        CONSTRAINT [PK_AuditEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditEvents_ExportJobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [ExportJobs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE TABLE [ExportAttempts] (
        [Id] uniqueidentifier NOT NULL,
        [JobId] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [Fence] bigint NOT NULL,
        [Worker] nvarchar(100) NOT NULL,
        [StartedAt] datetimeoffset NOT NULL,
        [EndedAt] datetimeoffset NULL,
        [Outcome] nvarchar(80) NULL,
        CONSTRAINT [PK_ExportAttempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExportAttempts_ExportJobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [ExportJobs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE TABLE [DocumentAcls] (
        [DocumentId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [RequesterOid] uniqueidentifier NOT NULL,
        [CanRequestProtectedCopy] bit NOT NULL,
        CONSTRAINT [PK_DocumentAcls] PRIMARY KEY ([DocumentId], [TenantId], [RequesterOid])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE TABLE [Documents] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [OwnerOid] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Purpose] nvarchar(1000) NOT NULL,
        [Status] int NOT NULL,
        [Enabled] bit NOT NULL,
        [CurrentPublishedVersionId] uniqueidentifier NULL,
        [Revision] bigint NOT NULL,
        [AclRevision] bigint NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_Documents_Id_TenantId] UNIQUE ([Id], [TenantId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE TABLE [DocumentVersions] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [UploaderOid] uniqueidentifier NOT NULL,
        [DisplayFilename] nvarchar(255) NOT NULL,
        [State] int NOT NULL,
        [SourceBindingJson] nvarchar(max) NULL,
        [ScanEvidenceJson] nvarchar(max) NULL,
        [ValidationEvidenceJson] nvarchar(max) NULL,
        [UploadedAt] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DocumentVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_DocumentVersions_Id_DocumentId_TenantId] UNIQUE ([Id], [DocumentId], [TenantId]),
        CONSTRAINT [FK_DocumentVersions_Documents_DocumentId_TenantId] FOREIGN KEY ([DocumentId], [TenantId]) REFERENCES [Documents] ([Id], [TenantId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_JobId] ON [AuditEvents] ([JobId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE INDEX [IX_AuditEvents_TenantId_At] ON [AuditEvents] ([TenantId], [At]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE INDEX [IX_Documents_CurrentPublishedVersionId_Id_TenantId] ON [Documents] ([CurrentPublishedVersionId], [Id], [TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE INDEX [IX_Documents_TenantId_OwnerOid] ON [Documents] ([TenantId], [OwnerOid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE INDEX [IX_DocumentVersions_DocumentId_TenantId] ON [DocumentVersions] ([DocumentId], [TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExportAttempts_JobId_Fence] ON [ExportAttempts] ([JobId], [Fence]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExportAttempts_JobId_Number] ON [ExportAttempts] ([JobId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE INDEX [IX_ExportJobs_State_AvailableAt_LeaseExpiresAt] ON [ExportJobs] ([State], [AvailableAt], [LeaseExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE INDEX [IX_ExportJobs_TenantId_RequesterOid_CreatedAt] ON [ExportJobs] ([TenantId], [RequesterOid], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExportJobs_TenantId_RequesterOid_IdempotencyKey] ON [ExportJobs] ([TenantId], [RequesterOid], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    ALTER TABLE [DocumentAcls] ADD CONSTRAINT [FK_DocumentAcls_Documents_DocumentId_TenantId] FOREIGN KEY ([DocumentId], [TenantId]) REFERENCES [Documents] ([Id], [TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    ALTER TABLE [Documents] ADD CONSTRAINT [FK_Documents_DocumentVersions_CurrentPublishedVersionId_Id_TenantId] FOREIGN KEY ([CurrentPublishedVersionId], [Id], [TenantId]) REFERENCES [DocumentVersions] ([Id], [DocumentId], [TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080529_InitialDurableQueue'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019080529_InitialDurableQueue', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080558_QueueSafetyConstraints'
)
BEGIN
    EXEC(N'ALTER TABLE [ExportJobs] ADD CONSTRAINT [CK_ExportJobs_Attempts] CHECK ([AttemptCount] BETWEEN 0 AND 3 AND [Fence] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080558_QueueSafetyConstraints'
)
BEGIN
    EXEC(N'ALTER TABLE [ExportJobs] ADD CONSTRAINT [CK_ExportJobs_Mode] CHECK ([Mode] IN (0,1) AND ([State] <> 9 OR [Mode] = 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080558_QueueSafetyConstraints'
)
BEGIN
    EXEC(N'ALTER TABLE [ExportJobs] ADD CONSTRAINT [CK_ExportJobs_Snapshot] CHECK (ISJSON([SnapshotJson]) = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080558_QueueSafetyConstraints'
)
BEGIN
    EXEC(N'ALTER TABLE [ExportJobs] ADD CONSTRAINT [CK_ExportJobs_SourceKind] CHECK ([SourceKind] IN (0,1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080558_QueueSafetyConstraints'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019080558_QueueSafetyConstraints', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080810_CorrelationAndExactWorkerIdentity'
)
BEGIN
    ALTER TABLE [ExportJobs] DROP CONSTRAINT [CK_ExportJobs_Lease];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080810_CorrelationAndExactWorkerIdentity'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ExportJobs]') AND [c].[name] = N'LeaseOwner');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ExportJobs] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [ExportJobs] ALTER COLUMN [LeaseOwner] nvarchar(100) COLLATE Latin1_General_100_BIN2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080810_CorrelationAndExactWorkerIdentity'
)
BEGIN
    EXEC(N'ALTER TABLE [ExportJobs] ADD CONSTRAINT [CK_ExportJobs_Lease] CHECK (([LeaseToken] IS NULL AND [LeaseOwner] IS NULL AND [LeaseExpiresAt] IS NULL) OR ([LeaseToken] IS NOT NULL AND [LeaseOwner] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080810_CorrelationAndExactWorkerIdentity'
)
BEGIN
    ALTER TABLE [ExportJobs] ADD [CorrelationId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019080810_CorrelationAndExactWorkerIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019080810_CorrelationAndExactWorkerIdentity', N'10.0.13');
END;

COMMIT;
GO

