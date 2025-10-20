namespace StarEvents.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ApplyModelChanges : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Ticket", "Event_EventId", c => c.Int());
            CreateIndex("dbo.Ticket", "Event_EventId");
            AddForeignKey("dbo.Ticket", "Event_EventId", "dbo.Event", "EventId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Ticket", "Event_EventId", "dbo.Event");
            DropIndex("dbo.Ticket", new[] { "Event_EventId" });
            DropColumn("dbo.Ticket", "Event_EventId");
        }
    }
}
