namespace StarEvents.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BaseUser",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Email = c.String(nullable: false, maxLength: 200),
                        PasswordHash = c.String(nullable: false, maxLength: 200),
                        FirstName = c.String(nullable: false, maxLength: 100),
                        LastName = c.String(nullable: false, maxLength: 100),
                        PhoneNumber = c.String(maxLength: 30),
                        CreatedDate = c.DateTime(nullable: false),
                        LastLoginDate = c.DateTime(),
                        IsActive = c.Boolean(nullable: false),
                        Role = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.BookingDetail",
                c => new
                    {
                        BookingDetailId = c.Int(nullable: false, identity: true),
                        Quantity = c.Int(nullable: false),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Subtotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BookingId = c.Int(nullable: false),
                        TicketTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.BookingDetailId)
                .ForeignKey("dbo.Booking", t => t.BookingId, cascadeDelete: true)
                .ForeignKey("dbo.TicketType", t => t.TicketTypeId)
                .Index(t => t.BookingId)
                .Index(t => t.TicketTypeId);
            
            CreateTable(
                "dbo.Booking",
                c => new
                    {
                        BookingId = c.Int(nullable: false, identity: true),
                        BookingReference = c.String(nullable: false, maxLength: 50),
                        BookingDate = c.DateTime(nullable: false),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DiscountAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        FinalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Status = c.Int(nullable: false),
                        LoyaltyPointsEarned = c.Int(nullable: false),
                        LoyaltyPointsUsed = c.Int(nullable: false),
                        CustomerId = c.Int(nullable: false),
                        EventId = c.Int(nullable: false),
                        PaymentId = c.Int(),
                    })
                .PrimaryKey(t => t.BookingId)
                .ForeignKey("dbo.Customers", t => t.CustomerId)
                .ForeignKey("dbo.Event", t => t.EventId)
                .Index(t => t.CustomerId)
                .Index(t => t.EventId);
            
            CreateTable(
                "dbo.Event",
                c => new
                    {
                        EventId = c.Int(nullable: false, identity: true),
                        EventName = c.String(nullable: false, maxLength: 200),
                        Description = c.String(nullable: false, maxLength: 2000),
                        EventDate = c.DateTime(nullable: false),
                        EventEndDate = c.DateTime(nullable: false),
                        Category = c.String(maxLength: 100),
                        ImageUrl = c.String(),
                        TotalSeats = c.Int(nullable: false),
                        AvailableSeats = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        OrganizerId = c.Int(nullable: false),
                        VenueId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.EventId)
                .ForeignKey("dbo.EventOrganizers", t => t.OrganizerId)
                .ForeignKey("dbo.Venue", t => t.VenueId)
                .Index(t => t.OrganizerId)
                .Index(t => t.VenueId);
            
            CreateTable(
                "dbo.TicketType",
                c => new
                    {
                        TicketTypeId = c.Int(nullable: false, identity: true),
                        TypeName = c.String(maxLength: 100),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AvailableQuantity = c.Int(nullable: false),
                        Description = c.String(maxLength: 1000),
                        EventId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.TicketTypeId)
                .ForeignKey("dbo.Event", t => t.EventId, cascadeDelete: true)
                .Index(t => t.EventId);
            
            CreateTable(
                "dbo.Venue",
                c => new
                    {
                        VenueId = c.Int(nullable: false, identity: true),
                        VenueName = c.String(nullable: false, maxLength: 200),
                        Address = c.String(nullable: false, maxLength: 500),
                        City = c.String(maxLength: 100),
                        Province = c.String(maxLength: 100),
                        Capacity = c.Int(nullable: false),
                        ContactNumber = c.String(maxLength: 50),
                        Facilities = c.String(),
                    })
                .PrimaryKey(t => t.VenueId);
            
            CreateTable(
                "dbo.Payment",
                c => new
                    {
                        PaymentId = c.Int(nullable: false),
                        TransactionId = c.String(maxLength: 100),
                        PaymentDate = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Method = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        PaymentGatewayResponse = c.String(),
                        BookingId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PaymentId)
                .ForeignKey("dbo.Booking", t => t.PaymentId)
                .Index(t => t.PaymentId);
            
            CreateTable(
                "dbo.Ticket",
                c => new
                    {
                        TicketId = c.Int(nullable: false, identity: true),
                        TicketNumber = c.String(nullable: false, maxLength: 50),
                        QRCodeData = c.String(),
                        QRCodeImage = c.Binary(),
                        IsUsed = c.Boolean(nullable: false),
                        UsedDate = c.DateTime(),
                        IssueDate = c.DateTime(nullable: false),
                        BookingId = c.Int(nullable: false),
                        TicketTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.TicketId)
                .ForeignKey("dbo.Booking", t => t.BookingId, cascadeDelete: true)
                .ForeignKey("dbo.TicketType", t => t.TicketTypeId, cascadeDelete: true)
                .Index(t => t.BookingId)
                .Index(t => t.TicketTypeId);
            
            CreateTable(
                "dbo.Discount",
                c => new
                    {
                        DiscountId = c.Int(nullable: false, identity: true),
                        DiscountCode = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 1000),
                        DiscountPercentage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MaxDiscountAmount = c.Decimal(precision: 18, scale: 2),
                        ValidFrom = c.DateTime(nullable: false),
                        ValidTo = c.DateTime(nullable: false),
                        MaxUsageCount = c.Int(),
                        CurrentUsageCount = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        Type = c.Int(nullable: false),
                        EventId = c.Int(),
                    })
                .PrimaryKey(t => t.DiscountId)
                .ForeignKey("dbo.Event", t => t.EventId)
                .Index(t => t.EventId);
            
            CreateTable(
                "dbo.Admins",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        AdminLevel = c.String(),
                        LastPasswordChangeDate = c.DateTime(),
                        UserType = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BaseUser", t => t.Id)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.Customers",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Address = c.String(),
                        LoyaltyPoints = c.Int(nullable: false),
                        DateOfBirth = c.DateTime(),
                        UserType = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BaseUser", t => t.Id)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.EventOrganizers",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        CompanyName = c.String(),
                        BusinessRegistrationNumber = c.String(),
                        BankAccountDetails = c.String(),
                        IsVerified = c.Boolean(nullable: false),
                        UserType = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BaseUser", t => t.Id)
                .Index(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.EventOrganizers", "Id", "dbo.BaseUser");
            DropForeignKey("dbo.Customers", "Id", "dbo.BaseUser");
            DropForeignKey("dbo.Admins", "Id", "dbo.BaseUser");
            DropForeignKey("dbo.Discount", "EventId", "dbo.Event");
            DropForeignKey("dbo.BookingDetail", "TicketTypeId", "dbo.TicketType");
            DropForeignKey("dbo.BookingDetail", "BookingId", "dbo.Booking");
            DropForeignKey("dbo.Ticket", "TicketTypeId", "dbo.TicketType");
            DropForeignKey("dbo.Ticket", "BookingId", "dbo.Booking");
            DropForeignKey("dbo.Payment", "PaymentId", "dbo.Booking");
            DropForeignKey("dbo.Booking", "EventId", "dbo.Event");
            DropForeignKey("dbo.Event", "VenueId", "dbo.Venue");
            DropForeignKey("dbo.TicketType", "EventId", "dbo.Event");
            DropForeignKey("dbo.Event", "OrganizerId", "dbo.EventOrganizers");
            DropForeignKey("dbo.Booking", "CustomerId", "dbo.Customers");
            DropIndex("dbo.EventOrganizers", new[] { "Id" });
            DropIndex("dbo.Customers", new[] { "Id" });
            DropIndex("dbo.Admins", new[] { "Id" });
            DropIndex("dbo.Discount", new[] { "EventId" });
            DropIndex("dbo.Ticket", new[] { "TicketTypeId" });
            DropIndex("dbo.Ticket", new[] { "BookingId" });
            DropIndex("dbo.Payment", new[] { "PaymentId" });
            DropIndex("dbo.TicketType", new[] { "EventId" });
            DropIndex("dbo.Event", new[] { "VenueId" });
            DropIndex("dbo.Event", new[] { "OrganizerId" });
            DropIndex("dbo.Booking", new[] { "EventId" });
            DropIndex("dbo.Booking", new[] { "CustomerId" });
            DropIndex("dbo.BookingDetail", new[] { "TicketTypeId" });
            DropIndex("dbo.BookingDetail", new[] { "BookingId" });
            DropTable("dbo.EventOrganizers");
            DropTable("dbo.Customers");
            DropTable("dbo.Admins");
            DropTable("dbo.Discount");
            DropTable("dbo.Ticket");
            DropTable("dbo.Payment");
            DropTable("dbo.Venue");
            DropTable("dbo.TicketType");
            DropTable("dbo.Event");
            DropTable("dbo.Booking");
            DropTable("dbo.BookingDetail");
            DropTable("dbo.BaseUser");
        }
    }
}
