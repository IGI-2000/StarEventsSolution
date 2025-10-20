using System;
using System.Data.Entity.Migrations;

namespace StarEvents.Migrations
{
    public partial class AddDiscountsAndLoyalty : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Discount",
                c => new
                {
                    DiscountId = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 50),
                    Description = c.String(maxLength: 250),
                    Percentage = c.Decimal(precision: 18, scale: 2),
                    Amount = c.Decimal(precision: 18, scale: 2),
                    StartDate = c.DateTime(),
                    EndDate = c.DateTime(),
                    IsActive = c.Boolean(nullable: false, defaultValue: true),
                    UsageLimit = c.Int(),
                    TimesUsed = c.Int(nullable: false, defaultValue: 0)
                })
                .PrimaryKey(t => t.DiscountId);

            // Add LoyaltyPoints to Customers (nullable)
            AddColumn("dbo.Customers", "LoyaltyPoints", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.Customers", "LoyaltyPoints");
            DropTable("dbo.Discount");
        }
    }
}