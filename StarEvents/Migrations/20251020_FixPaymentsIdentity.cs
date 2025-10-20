using System;
using System.Data.Entity.Migrations;

namespace StarEvents.Migrations
{
    public partial class FixPaymentsIdentity : DbMigration
    {
        public override void Up()
        {
            // If PaymentId exists and is NOT an identity, replace it with an IDENTITY PK.
            Sql(@"
IF EXISTS(
    SELECT 1
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID('dbo.Payments')
      AND c.name = 'PaymentId'
      AND COLUMNPROPERTY(c.object_id, c.name, 'IsIdentity') = 0
)
BEGIN
    PRINT 'FixPaymentsIdentity: converting Payments.PaymentId to IDENTITY';

    -- Drop primary key constraint if exists
    DECLARE @pk nvarchar(128);
    SELECT @pk = kc.name
    FROM sys.key_constraints kc
    WHERE kc.parent_object_id = OBJECT_ID('dbo.Payments') AND kc.[type] = 'PK';

    IF @pk IS NOT NULL
        EXEC('ALTER TABLE dbo.Payments DROP CONSTRAINT [' + @pk + ']');

    -- Add new identity column (temporary)
    IF COL_LENGTH('dbo.Payments','PaymentId_tmp') IS NULL
        ALTER TABLE dbo.Payments ADD PaymentId_tmp INT IDENTITY(1,1) NOT NULL;

    -- Drop old column PaymentId
    IF COL_LENGTH('dbo.Payments','PaymentId') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Payments DROP COLUMN PaymentId;
    END

    -- Rename tmp to PaymentId
    EXEC sp_rename 'dbo.Payments.PaymentId_tmp', 'PaymentId', 'COLUMN';

    -- Add primary key on new PaymentId
    ALTER TABLE dbo.Payments ADD CONSTRAINT PK_dbo_Payments PRIMARY KEY (PaymentId);

    PRINT 'FixPaymentsIdentity: completed';
END
ELSE
BEGIN
    PRINT 'FixPaymentsIdentity: no action needed (PaymentId missing or already identity)';
END
");
        }

        public override void Down()
        {
            // Down: best-effort - do not attempt to restore original numeric values.
            Sql(@"
IF EXISTS(
    SELECT 1
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID('dbo.Payments')
      AND c.name = 'PaymentId'
      AND COLUMNPROPERTY(c.object_id, c.name, 'IsIdentity') = 1
)
BEGIN
    PRINT 'FixPaymentsIdentity.Down: not reverting identity to avoid data loss';
END
");
        }
    }
}