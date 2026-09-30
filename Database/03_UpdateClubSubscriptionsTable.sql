-- Add ID column to clubSubscriptions if it doesn't exist (for foreign key reference)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[clubSubscriptions]') AND name = N'id')
BEGIN
    ALTER TABLE [dbo].[clubSubscriptions]
    ADD [id] INT IDENTITY(1,1) PRIMARY KEY
END

-- Add index on clubSubscriptions for quick lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_clubSubscriptions_ClubId' AND object_id = OBJECT_ID(N'[dbo].[clubSubscriptions]'))
BEGIN
    CREATE INDEX IX_clubSubscriptions_ClubId ON [dbo].[clubSubscriptions]([club_id])
END
