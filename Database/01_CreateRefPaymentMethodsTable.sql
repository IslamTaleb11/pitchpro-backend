-- Create refPaymentMethods lookup table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[refPaymentMethods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[refPaymentMethods] (
        [id] INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
        [methodName] NVARCHAR(50) NOT NULL,
        [description] NVARCHAR(255) NULL,
        [created_date] DATETIME DEFAULT GETUTCDATE() NOT NULL
    )

    -- Insert default payment methods
    INSERT INTO [dbo].[refPaymentMethods] ([methodName], [description]) VALUES 
        (N'CreditCard', N'Credit Card Payment'),
        (N'DebitCard', N'Debit Card Payment'),
        (N'MobileWallet', N'Mobile Wallet Payment'),
        (N'BankTransfer', N'Bank Transfer Payment')
END
