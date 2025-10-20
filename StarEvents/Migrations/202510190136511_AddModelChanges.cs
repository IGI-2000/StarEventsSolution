namespace StarEvents.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddModelChanges : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Event", "IsPublished", c => c.Boolean(nullable: false));
            AddColumn("dbo.Event", "PublishedDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Event", "PublishedDate");
            DropColumn("dbo.Event", "IsPublished");
        }
    }
}
