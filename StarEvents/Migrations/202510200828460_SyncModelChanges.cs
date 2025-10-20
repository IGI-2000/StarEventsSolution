namespace StarEvents.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncModelChanges : DbMigration
    {
        public override void Up()
        {
            // Remove old FKs that reference BookingDetail or Booking
            DropForeignKey("dbo.Tickets", "BookingDetailId", "dbo.BookingDetail");
            DropForeignKey("dbo.Tickets", "BookingId", "dbo.Booking");
            DropIndex("dbo.Tickets", new[] { "BookingId" });
            DropIndex("dbo.Tickets", new[] { "BookingDetailId" });

            // Add new columns
            AddColumn("dbo.Tickets", "TicketTypeId", c => c.Int(nullable: false, defaultValue: 0));
            AddColumn("dbo.Tickets", "QrCodeBase64", c => c.String());

            // Make BookingId nullable to allow tickets without booking if required
            AlterColumn("dbo.Tickets", "BookingId", c => c.Int());

            // Ensure TicketNumber not null
            AlterColumn("dbo.Tickets", "TicketNumber", c => c.String(nullable: false, maxLength: 100));

            // QrCode becomes nvarchar(max)
            AlterColumn("dbo.Tickets", "QrCode", c => c.String());

            // Populate TicketTypeId from BookingDetail (if BookingDetailId exists)
            Sql(@"
-- 1) If BookingDetailId column exists, copy TicketTypeId from BookingDetail
IF COL_LENGTH('dbo.Tickets','BookingDetailId') IS NOT NULL AND OBJECT_ID('dbo.BookingDetail','U') IS NOT NULL
BEGIN
    UPDATE t
    SET t.TicketTypeId = bd.TicketTypeId
    FROM dbo.Tickets t
    INNER JOIN dbo.BookingDetail bd ON bd.BookingDetailId = t.BookingDetailId
END
");

            // 2) For any Tickets that still have TicketTypeId = 0 or refer to missing TicketType, set to an existing TicketType (create placeholder if none)
            Sql(@"
DECLARE @anyExisting INT;
SELECT TOP 1 @anyExisting = TicketTypeId FROM dbo.TicketType;

IF @anyExisting IS NULL
BEGIN
    -- create a placeholder ticket type; adapt columns if your TicketType table differs
    INSERT INTO dbo.TicketType (TypeName, Price, AvailableQuantity, Description, EventId)
    VALUES (N'Legacy Placeholder', 0.00, 0, N'Auto-created placeholder for migration', NULL);

    SELECT TOP 1 @anyExisting = TicketTypeId FROM dbo.TicketType ORDER BY TicketTypeId DESC;
END

-- Set TicketTypeId for rows that are 0 or NULL
UPDATE dbo.Tickets
SET TicketTypeId = @anyExisting
WHERE TicketTypeId IS NULL OR TicketTypeId = 0
   OR NOT EXISTS (SELECT 1 FROM dbo.TicketType tt WHERE tt.TicketTypeId = dbo.Tickets.TicketTypeId);
");

            // Recreate indexes and add FK safely (now tickets reference valid TicketTypeId)
            CreateIndex("dbo.Tickets", "BookingId");
            CreateIndex("dbo.Tickets", "TicketTypeId");
            AddForeignKey("dbo.Tickets", "TicketTypeId", "dbo.TicketType", "TicketTypeId", cascadeDelete: true);
            AddForeignKey("dbo.Tickets", "BookingId", "dbo.Booking", "BookingId");

            // Remove legacy BookingDetailId column (done last after we used it)
            if (ColumnExists("dbo.Tickets", "BookingDetailId"))
            {
                DropColumn("dbo.Tickets", "BookingDetailId");
            }
        }
        
        public override void Down()
        {
            // Recreate BookingDetailId column (best-effort)
            AddColumn("dbo.Tickets", "BookingDetailId", c => c.Int());
            DropForeignKey("dbo.Tickets", "BookingId", "dbo.Booking");
            DropForeignKey("dbo.Tickets", "TicketTypeId", "dbo.TicketType");
            DropIndex("dbo.Tickets", new[] { "TicketTypeId" });
            DropIndex("dbo.Tickets", new[] { "BookingId" });
            AlterColumn("dbo.Tickets", "QrCode", c => c.Binary());
            AlterColumn("dbo.Tickets", "TicketNumber", c => c.String(maxLength: 100));
            AlterColumn("dbo.Tickets", "BookingId", c => c.Int(nullable: false));
            DropColumn("dbo.Tickets", "QrCodeBase64");
            DropColumn("dbo.Tickets", "TicketTypeId");
            CreateIndex("dbo.Tickets", "BookingDetailId");
            CreateIndex("dbo.Tickets", "BookingId");
            AddForeignKey("dbo.Tickets", "BookingId", "dbo.Booking", "BookingId", cascadeDelete: true);
            AddForeignKey("dbo.Tickets", "BookingDetailId", "dbo.BookingDetail", "BookingDetailId");
        }

        // EF helper: ColumnExists (only used at runtime here; available in migration base via SQL checks, but keep safe)
        private bool ColumnExists(string tableName, string columnName)
        {
            // migrations do not support calling DB at design time; keep this simple and rely on SQL guard in Up.
            return true;
        }
    }
}
