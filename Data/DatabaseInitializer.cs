using Microsoft.Data.SqlClient;
using StockSmart.Exceptions;
using StockSmart.Security;

namespace StockSmart.Data;

/// <summary>
/// Creates the StockSmart database, its five tables and sample data on first run,
/// so no manual database setup is needed (NFR5). Safe to run every time the app starts:
/// existing tables and data are left untouched.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void CreateAndSeed()
    {
        try
        {
            CreateDatabaseIfMissing();
            CreateTables();
            SeedSampleData();
        }
        catch (SqlException ex)
        {
            throw new DatabaseException("The StockSmart database could not be created or opened.", ex);
        }
    }

    private void CreateDatabaseIfMissing()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        string databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        // Store the database files in a local folder (outside OneDrive and outside the project).
        // The location is set explicitly because LocalDB's default data path can be invalid
        // (it can drop the trailing backslash, which causes "Access is denied").
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StockSmart");
        Directory.CreateDirectory(folder);
        string dataFile = Path.Combine(folder, databaseName + ".mdf").Replace("'", "''");
        string logFile = Path.Combine(folder, databaseName + "_log.ldf").Replace("'", "''");

        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();

        // The database name and file paths come from our own configuration, never from user input.
        using var command = new SqlCommand(
            $@"IF DB_ID(@name) IS NULL
               CREATE DATABASE [{databaseName}]
               ON PRIMARY (NAME = N'{databaseName}', FILENAME = N'{dataFile}')
               LOG ON (NAME = N'{databaseName}_log', FILENAME = N'{logFile}');", connection);
        command.Parameters.AddWithValue("@name", databaseName);
        command.ExecuteNonQuery();
    }

    private void CreateTables()
    {
        string[] statements =
        {
            @"IF OBJECT_ID(N'dbo.Supplier', N'U') IS NULL
              CREATE TABLE dbo.Supplier (
                  SupplierId INT IDENTITY(1,1) CONSTRAINT PK_Supplier PRIMARY KEY,
                  Name       NVARCHAR(100) NOT NULL,
                  Phone      NVARCHAR(20)  NULL,
                  Email      NVARCHAR(100) NULL
              );",

            @"IF OBJECT_ID(N'dbo.Product', N'U') IS NULL
              CREATE TABLE dbo.Product (
                  ProductId    INT IDENTITY(1,1) CONSTRAINT PK_Product PRIMARY KEY,
                  Name         NVARCHAR(100) NOT NULL CONSTRAINT UQ_Product_Name UNIQUE,
                  Category     NVARCHAR(50)  NULL,
                  Price        DECIMAL(10,2) NOT NULL CONSTRAINT CK_Product_Price CHECK (Price >= 0),
                  Quantity     INT           NOT NULL CONSTRAINT CK_Product_Quantity CHECK (Quantity >= 0),
                  ReorderLevel INT           NOT NULL CONSTRAINT CK_Product_ReorderLevel CHECK (ReorderLevel >= 0),
                  IsActive     BIT           NOT NULL CONSTRAINT DF_Product_IsActive DEFAULT (1),
                  SupplierId   INT           NOT NULL CONSTRAINT FK_Product_Supplier REFERENCES dbo.Supplier(SupplierId)
              );",

            @"IF OBJECT_ID(N'dbo.AppUser', N'U') IS NULL
              CREATE TABLE dbo.AppUser (
                  UserId       INT IDENTITY(1,1) CONSTRAINT PK_AppUser PRIMARY KEY,
                  Username     NVARCHAR(50)  NOT NULL CONSTRAINT UQ_AppUser_Username UNIQUE,
                  PasswordHash NVARCHAR(255) NOT NULL,
                  Role         NVARCHAR(20)  NOT NULL CONSTRAINT CK_AppUser_Role CHECK (Role IN (N'Owner', N'Manager', N'Staff')),
                  IsActive     BIT           NOT NULL CONSTRAINT DF_AppUser_IsActive DEFAULT (1)
              );",

            @"IF OBJECT_ID(N'dbo.Sale', N'U') IS NULL
              CREATE TABLE dbo.Sale (
                  SaleId   INT IDENTITY(1,1) CONSTRAINT PK_Sale PRIMARY KEY,
                  SaleDate DATETIME      NOT NULL CONSTRAINT DF_Sale_SaleDate DEFAULT (GETDATE()),
                  Total    DECIMAL(10,2) NOT NULL CONSTRAINT CK_Sale_Total CHECK (Total >= 0),
                  UserId   INT           NOT NULL CONSTRAINT FK_Sale_AppUser REFERENCES dbo.AppUser(UserId)
              );",

            @"IF OBJECT_ID(N'dbo.SaleItem', N'U') IS NULL
              CREATE TABLE dbo.SaleItem (
                  SaleItemId INT IDENTITY(1,1) CONSTRAINT PK_SaleItem PRIMARY KEY,
                  SaleId     INT           NOT NULL CONSTRAINT FK_SaleItem_Sale REFERENCES dbo.Sale(SaleId),
                  ProductId  INT           NOT NULL CONSTRAINT FK_SaleItem_Product REFERENCES dbo.Product(ProductId),
                  Quantity   INT           NOT NULL CONSTRAINT CK_SaleItem_Quantity CHECK (Quantity > 0),
                  UnitPrice  DECIMAL(10,2) NOT NULL CONSTRAINT CK_SaleItem_UnitPrice CHECK (UnitPrice >= 0)
              );"
        };

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        foreach (string sql in statements)
        {
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }

    private void SeedSampleData()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using (var check = new SqlCommand("SELECT COUNT(*) FROM dbo.AppUser;", connection))
        {
            if ((int)check.ExecuteScalar()! > 0)
                return; // Already seeded on an earlier run.
        }

        // All sample data is saved together, or not at all (NFR3).
        using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            AddUser(connection, transaction, "owner", "Owner#2026", "Owner");
            AddUser(connection, transaction, "manager", "Manager#2026", "Manager");
            AddUser(connection, transaction, "staff", "Staff#2026", "Staff");

            int stationers = AddSupplier(connection, transaction, "Mzansi Stationers", "011 555 0101", "orders@mzansistationers.example");
            int bakery = AddSupplier(connection, transaction, "Golden Crust Bakery", "011 555 0202", "sales@goldencrust.example");
            int household = AddSupplier(connection, transaction, "CleanHome Distributors", "011 555 0303", "info@cleanhome.example");

            AddProduct(connection, transaction, "Bic Pen Blue", "Stationery", 6.50m, 3, 10, stationers);
            AddProduct(connection, transaction, "A4 Exercise Book 72 pg", "Stationery", 12.50m, 40, 15, stationers);
            AddProduct(connection, transaction, "White Bread 700 g", "Bakery", 19.99m, 24, 8, bakery);
            AddProduct(connection, transaction, "Sunlight Dishwashing Liquid 750 ml", "Household", 32.99m, 6, 6, household);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void AddUser(SqlConnection connection, SqlTransaction transaction, string username, string password, string role)
    {
        using var command = new SqlCommand(
            "INSERT INTO dbo.AppUser (Username, PasswordHash, Role) VALUES (@username, @hash, @role);",
            connection, transaction);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@hash", PasswordHasher.Hash(password));
        command.Parameters.AddWithValue("@role", role);
        command.ExecuteNonQuery();
    }

    private static int AddSupplier(SqlConnection connection, SqlTransaction transaction, string name, string phone, string email)
    {
        using var command = new SqlCommand(
            "INSERT INTO dbo.Supplier (Name, Phone, Email) OUTPUT INSERTED.SupplierId VALUES (@name, @phone, @email);",
            connection, transaction);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@phone", phone);
        command.Parameters.AddWithValue("@email", email);
        return (int)command.ExecuteScalar()!;
    }

    private static void AddProduct(SqlConnection connection, SqlTransaction transaction,
        string name, string category, decimal price, int quantity, int reorderLevel, int supplierId)
    {
        using var command = new SqlCommand(
            @"INSERT INTO dbo.Product (Name, Category, Price, Quantity, ReorderLevel, SupplierId)
              VALUES (@name, @category, @price, @quantity, @reorderLevel, @supplierId);",
            connection, transaction);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@category", category);
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@quantity", quantity);
        command.Parameters.AddWithValue("@reorderLevel", reorderLevel);
        command.Parameters.AddWithValue("@supplierId", supplierId);
        command.ExecuteNonQuery();
    }
}
