using System;
using System.Data.Entity.Migrations;

namespace StarEvents.Migrations
{
    public partial class Add_Payment_Booking_FK : DbMigration
    {
        public override void Up()
        {
            // 0) Defensive: ensure Payments table exists
            Sql(@"
IF OBJECT_ID('dbo.Payments','U') IS NULL
    THROW 51000, 'Payments table not found; aborting migration.', 1;
");

            // 1) Add Payments.BookingId if missing
            Sql(@"
IF COL_LENGTH('dbo.Payments','BookingId') IS NULL
BEGIN
    ALTER TABLE dbo.Payments ADD BookingId INT NULL;
END
");

            // 2) If legacy Booking.PaymentId exists, copy relation and then remove legacy FK/index/column
            Sql(@"
IF COL_LENGTH('dbo.Booking','PaymentId') IS NOT NULL
BEGIN
    -- Copy relation from Booking.PaymentId -> Payments.PaymentId into Payments.BookingId
    UPDATE p
    SET p.BookingId = b.BookingId
    FROM dbo.Payments p
    INNER JOIN dbo.Booking b ON b.PaymentId = p.PaymentId;

    -- Drop any FK constraint on Booking.PaymentId
    DECLARE @fk nvarchar(128);
    SELECT TOP 1 @fk = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    JOIN sys.tables t ON t.object_id = fk.parent_object_id
    WHERE t.name = 'Booking' AND c.name = 'PaymentId';

    IF @fk IS NOT NULL
        EXEC('ALTER TABLE [dbo].[Booking] DROP CONSTRAINT [' + @fk + ']');

    -- Drop index if exists
    DECLARE @ix nvarchar(128);
    SELECT TOP 1 @ix = ix.name
    FROM sys.indexes ix
    JOIN sys.index_columns ic ON ix.object_id = ic.object_id AND ix.index_id = ic.index_id
    JOIN sys.columns c2 ON ic.object_id = c2.object_id AND ic.column_id = c2.column_id
    JOIN sys.tables t2 ON t2.object_id = ix.object_id
    WHERE t2.name = 'Booking' AND c2.name = 'PaymentId';

    IF @ix IS NOT NULL
        EXEC('DROP INDEX [' + @ix + '] ON [dbo].[Booking]');

    ALTER TABLE dbo.Booking DROP COLUMN PaymentId;
END
");

            // 3) Create index on Payments.BookingId if not exists
            Sql(@"
IF COL_LENGTH('dbo.Payments','BookingId') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_BookingId' AND object_id = OBJECT_ID('dbo.Payments'))
    BEGIN
        CREATE INDEX IX_Payments_BookingId ON dbo.Payments (BookingId);
    END
END
");

            // 4) Add FK Payments.BookingId -> Booking(BookingId) if not exists
            Sql(@"
IF COL_LENGTH('dbo.Payments','BookingId') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Booking_BookingId')
    BEGIN
        ALTER TABLE dbo.Payments
        ADD CONSTRAINT FK_Payments_Booking_BookingId FOREIGN KEY (BookingId) REFERENCES dbo.Booking(BookingId);
    END
END
");
        }

        public override void Down()
        {
            // Drop FK and index if present
            Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Booking_BookingId')
BEGIN
    ALTER TABLE dbo.Payments DROP CONSTRAINT FK_Payments_Booking_BookingId;
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_BookingId' AND object_id = OBJECT_ID('dbo.Payments'))
BEGIN
    DROP INDEX IX_Payments_BookingId ON dbo.Payments;
END

-- Recreate Booking.PaymentId as nullable if it doesn't exist (best-effort)
IF COL_LENGTH('dbo.Booking','PaymentId') IS NULL
BEGIN
    ALTER TABLE dbo.Booking ADD PaymentId INT NULL;
END

-- Try to repopulate Booking.PaymentId from Payments.BookingId (best-effort)
IF COL_LENGTH('dbo.Payments','BookingId') IS NOT NULL AND COL_LENGTH('dbo.Booking','PaymentId') IS NOT NULL
BEGIN
    UPDATE b
    SET b.PaymentId = p.PaymentId
    FROM dbo.Booking b
    INNER JOIN dbo.Payments p ON p.BookingId = b.BookingId;
END

-- Optionally drop Payments.BookingId
IF COL_LENGTH('dbo.Payments','BookingId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Payments DROP COLUMN BookingId;
END
");
        }
    }
}
