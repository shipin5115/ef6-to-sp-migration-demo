-- 1. SecurityMasters Table (reference/master data for a security)
CREATE TABLE [dbo].[SecurityMasters] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Symbol] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_SecurityMasters] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- 2. PortfolioSecurityTypes Table (maps a dataset/security type to its factor time range)
CREATE TABLE [dbo].[PortfolioSecurityTypes] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SecurityTypeId] INT NOT NULL,
    [DataId] INT NOT NULL,
    [FactorTimeRange] INT NOT NULL,
    CONSTRAINT [PK_PortfolioSecurityTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- 3. SecurityPrices Table (raw security price/factor records)
CREATE TABLE [dbo].[SecurityPrices] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SecurityID] INT NOT NULL,
    [DatasetId] INT NOT NULL,
    [PriceDate] DATETIME2(7) NOT NULL,
    [Price] DECIMAL(18,6) NOT NULL,
    [Factor] FLOAT NOT NULL DEFAULT 0,
    [DatasetName] NVARCHAR(200) NOT NULL,
    [EntityFactorName] NVARCHAR(200) NOT NULL,
    [TickerName] NVARCHAR(50) NOT NULL,
    [SecurityType] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_SecurityPrices] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SecurityPrices_SecurityMasters] FOREIGN KEY ([SecurityID]) REFERENCES [dbo].[SecurityMasters]([Id])
);

-- Helpful indexes for the Mortgage Factor Report's range/date lookups
CREATE NONCLUSTERED INDEX [IX_SecurityPrices_SecurityID_PriceDate]
    ON [dbo].[SecurityPrices] ([SecurityID] ASC, [PriceDate] ASC);

CREATE NONCLUSTERED INDEX [IX_SecurityPrices_DatasetId_PriceDate]
    ON [dbo].[SecurityPrices] ([DatasetId] ASC, [PriceDate] ASC);
