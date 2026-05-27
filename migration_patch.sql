BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527103431_AddResultStateAndPercentile'
)
BEGIN
    ALTER TABLE [PaperQuestions] DROP CONSTRAINT [FK_PaperQuestions_PaperVersions_PaperVersionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527103431_AddResultStateAndPercentile'
)
BEGIN
    DROP INDEX [IX_PaperQuestions_PaperVersionId] ON [PaperQuestions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527103431_AddResultStateAndPercentile'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaperQuestions]') AND [c].[name] = N'PaperVersionId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [PaperQuestions] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [PaperQuestions] DROP COLUMN [PaperVersionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527103431_AddResultStateAndPercentile'
)
BEGIN
    ALTER TABLE [Results] ADD [Percentile] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527103431_AddResultStateAndPercentile'
)
BEGIN
    ALTER TABLE [Results] ADD [State] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527103431_AddResultStateAndPercentile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260527103431_AddResultStateAndPercentile', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527110309_AddCachedLeaderboard'
)
BEGIN
    CREATE TABLE [CachedLeaderboardEntries] (
        [Id] uniqueidentifier NOT NULL,
        [ExamId] uniqueidentifier NOT NULL,
        [ClassBatchId] uniqueidentifier NOT NULL,
        [StudentProfileId] uniqueidentifier NOT NULL,
        [Rank] int NOT NULL,
        [Percentile] float NOT NULL,
        [TotalScore] decimal(18,2) NOT NULL,
        [ComputedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_CachedLeaderboardEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260527110309_AddCachedLeaderboard'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260527110309_AddCachedLeaderboard', N'10.0.8');
END;

COMMIT;
GO

