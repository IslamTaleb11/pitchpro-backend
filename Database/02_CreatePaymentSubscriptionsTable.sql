-- Create paymentSubscriptions table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[paymentSubscriptions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[paymentSubscriptions] (
        [id] INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
        [club_id] INT NOT NULL,
        [plan_name] NVARCHAR(50) NOT NULL,
        [amount] DECIMAL(18, 2) NOT NULL,
        [currency] NVARCHAR(3) NOT NULL,
        [payment_method_id] INT NOT NULL,
        [chargily_invoice_id] NVARCHAR(100) NOT NULL UNIQUE,
        [is_active] BIT DEFAULT 1 NOT NULL,
        [club_subscription_id] INT NOT NULL,
        [created_date] DATETIME DEFAULT GETUTCDATE() NOT NULL,
        [payment_date] DATETIME NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_paymentSubscriptions_Clubs FOREIGN KEY ([club_id]) REFERENCES [dbo].[Clubs]([ID]),
        CONSTRAINT FK_paymentSubscriptions_PaymentMethods FOREIGN KEY ([payment_method_id]) REFERENCES [dbo].[refPaymentMethods]([id]),
        CONSTRAINT FK_paymentSubscriptions_ClubSubscriptions FOREIGN KEY ([club_subscription_id]) REFERENCES [dbo].[clubSubscriptions]([id])
    )

    -- Create index for quick lookup by chargily_invoice_id
    CREATE INDEX IX_paymentSubscriptions_ChargilyInvoiceId ON [dbo].[paymentSubscriptions]([chargily_invoice_id])
    
    -- Create index for club queries
    CREATE INDEX IX_paymentSubscriptions_ClubId ON [dbo].[paymentSubscriptions]([club_id])
END
