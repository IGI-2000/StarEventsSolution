namespace StarEvents.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncModelToDb : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.Ticket", newName: "Tickets");
            RenameTable(name: "dbo.Payment", newName: "Payments");

            // Drop FK on the current (renamed) table name if exists
            DropForeignKey("dbo.Tickets", "TicketTypeId", "dbo.TicketType");
            DropIndex("dbo.Tickets", new[] { "TicketTypeId" });

            CreateTable(
                "dbo.PaymentMethods",
                c => new
                    {
                        PaymentMethodId = c.Int(nullable: false, identity: true),
                        Token = c.String(nullable: false, maxLength: 200),
                        Last4 = c.String(nullable: false, maxLength: 10),
                        ExpiryMonth = c.Int(nullable: false),
                        ExpiryYear = c.Int(nullable: false),
                        CardBrand = c.String(maxLength: 50),
                        CustomerId = c.Int(nullable: false),
                        DisplayName = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.PaymentMethodId);
            
            AddColumn("dbo.Tickets", "BookingDetailId", c => c.Int());
            AddColumn("dbo.Tickets", "QrCode", c => c.Binary());
            AddColumn("dbo.Payments", "Last4", c => c.String(maxLength: 10));
            AddColumn("dbo.Payments", "PaymentMethodId", c => c.Int());
            AlterColumn("dbo.Tickets", "TicketNumber", c => c.String(maxLength: 100));
            AlterColumn("dbo.Payments", "TransactionId", c => c.String(maxLength: 200));
            AlterColumn("dbo.Payments", "PaymentGatewayResponse", c => c.String(maxLength: 1000));
            CreateIndex("dbo.Tickets", "BookingDetailId");
            CreateIndex("dbo.Payments", "PaymentMethodId");
            AddForeignKey("dbo.Tickets", "BookingDetailId", "dbo.BookingDetail", "BookingDetailId");
            AddForeignKey("dbo.Payments", "PaymentMethodId", "dbo.PaymentMethods", "PaymentMethodId");

            // Defensive: drop any remaining foreign keys that reference TicketTypeId on Ticket/Tickets
            // This handles cases where a FK name still references 'Ticket' (singular) or other legacy names.
            Sql(@"
DECLARE @fkName nvarchar(128), @tbl nvarchar(128);
DECLARE fk_cursor CURSOR FOR
SELECT fk.name, t.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
JOIN sys.tables t ON t.object_id = fk.parent_object_id
WHERE c.name = 'TicketTypeId' AND t.name IN ('Ticket','Tickets');

OPEN fk_cursor;
FETCH NEXT FROM fk_cursor INTO @fkName, @tbl;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF @fkName IS NOT NULL AND @tbl IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [' + @tbl + '] DROP CONSTRAINT [' + @fkName + ']');
    END
    FETCH NEXT FROM fk_cursor INTO @fkName, @tbl;
END
CLOSE fk_cursor;
DEALLOCATE fk_cursor;
");

            // drop legacy columns no longer used
            // Remove old QR columns and TicketTypeId (after FK dropped)
            DropColumn("dbo.Tickets", "QRCodeData");
            DropColumn("dbo.Tickets", "QRCodeImage");
            DropColumn("dbo.Tickets", "IsUsed");
            DropColumn("dbo.Tickets", "UsedDate");
            DropColumn("dbo.Tickets", "TicketTypeId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Tickets", "TicketTypeId", c => c.Int(nullable: false));
            AddColumn("dbo.Tickets", "UsedDate", c => c.DateTime());
            AddColumn("dbo.Tickets", "IsUsed", c => c.Boolean(nullable: false));
            AddColumn("dbo.Tickets", "QRCodeImage", c => c.Binary());
            AddColumn("dbo.Tickets", "QRCodeData", c => c.String());
            DropForeignKey("dbo.Payments", "PaymentMethodId", "dbo.PaymentMethods");
            DropForeignKey("dbo.Tickets", "BookingDetailId", "dbo.BookingDetail");
            DropIndex("dbo.Payments", new[] { "PaymentMethodId" });
            DropIndex("dbo.Tickets", new[] { "BookingDetailId" });
            AlterColumn("dbo.Payments", "PaymentGatewayResponse", c => c.String());
            AlterColumn("dbo.Payments", "TransactionId", c => c.String(maxLength: 100));
            AlterColumn("dbo.Tickets", "TicketNumber", c => c.String(nullable: false, maxLength: 50));
            DropColumn("dbo.Payments", "PaymentMethodId");
            DropColumn("dbo.Payments", "Last4");
            DropColumn("dbo.Tickets", "QrCode");
            DropColumn("dbo.Tickets", "BookingDetailId");
            DropTable("dbo.PaymentMethods");
            CreateIndex("dbo.Tickets", "TicketTypeId");
            AddForeignKey("dbo.Ticket", "TicketTypeId", "dbo.TicketType", "TicketTypeId", cascadeDelete: true);
            RenameTable(name: "dbo.Payments", newName: "Payment");
            RenameTable(name: "dbo.Tickets", newName: "Ticket");
        }
    }
}
