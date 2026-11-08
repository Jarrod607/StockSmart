-- FR7 test data: three back-dated sales earlier in the week of 2-8 Nov 2026 (recorded as staff).
-- Run once on 8 Nov 2026. Stock is NOT reduced: these stand in for sales made before go-live.
-- Safety check: if the sales are already there, stop without changing anything.
IF EXISTS (SELECT 1 FROM dbo.Sale WHERE SaleDate = '2026-11-02T09:15:00')
BEGIN
    PRINT 'FR7 test sales already exist - nothing was added.';
    RETURN;
END

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @staff INT = (SELECT UserId    FROM dbo.AppUser WHERE Username = 'staff');
DECLARE @bread INT = (SELECT ProductId FROM dbo.Product WHERE Name = 'White Bread 700 g');
DECLARE @soap  INT = (SELECT ProductId FROM dbo.Product WHERE Name = 'Sunlight Dishwashing Liquid 750 ml');
DECLARE @book  INT = (SELECT ProductId FROM dbo.Product WHERE Name = 'A4 Exercise Book 72 pg');
DECLARE @saleId INT;

-- Mon 2 Nov, 09:15: 3 x White Bread = R59.97
INSERT INTO dbo.Sale (SaleDate, Total, UserId) VALUES ('2026-11-02T09:15:00', 59.97, @staff);
SET @saleId = SCOPE_IDENTITY();
INSERT INTO dbo.SaleItem (SaleId, ProductId, Quantity, UnitPrice) VALUES (@saleId, @bread, 3, 19.99);

-- Wed 4 Nov, 14:30: 2 x White Bread + 1 x Sunlight = R72.97
INSERT INTO dbo.Sale (SaleDate, Total, UserId) VALUES ('2026-11-04T14:30:00', 72.97, @staff);
SET @saleId = SCOPE_IDENTITY();
INSERT INTO dbo.SaleItem (SaleId, ProductId, Quantity, UnitPrice) VALUES (@saleId, @bread, 2, 19.99);
INSERT INTO dbo.SaleItem (SaleId, ProductId, Quantity, UnitPrice) VALUES (@saleId, @soap, 1, 32.99);

-- Thu 5 Nov, 11:00: 4 x A4 Exercise Book = R50.00
INSERT INTO dbo.Sale (SaleDate, Total, UserId) VALUES ('2026-11-05T11:00:00', 50.00, @staff);
SET @saleId = SCOPE_IDENTITY();
INSERT INTO dbo.SaleItem (SaleId, ProductId, Quantity, UnitPrice) VALUES (@saleId, @book, 4, 12.50);

COMMIT;

-- Show the whole week, day by day
SELECT CAST(SaleDate AS date) AS Day, COUNT(*) AS Sales, SUM(Total) AS Total
FROM dbo.Sale
WHERE SaleDate >= '2026-11-02' AND SaleDate < '2026-11-09'
GROUP BY CAST(SaleDate AS date)
ORDER BY Day;