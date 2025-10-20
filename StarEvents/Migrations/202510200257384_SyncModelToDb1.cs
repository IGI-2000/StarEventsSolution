namespace StarEvents.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncModelToDb1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Tickets", "SeatNumber", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tickets", "SeatNumber");
        }
    }
}
