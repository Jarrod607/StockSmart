using StockSmart.Data;
using StockSmart.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Repositories: one new instance per web request.
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<SupplierRepository>();
builder.Services.AddScoped<ProductRepository>();

var app = builder.Build();

// Create the StockSmart database, tables and sample data on first run (NFR5).
string connectionString = builder.Configuration.GetConnectionString("StockSmart")
    ?? throw new InvalidOperationException("Connection string 'StockSmart' is missing from appsettings.json.");
new DatabaseInitializer(connectionString).CreateAndSeed();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();