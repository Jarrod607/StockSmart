using Microsoft.AspNetCore.Authentication.Cookies;
using StockSmart.Data;
using StockSmart.Repositories;
using StockSmart.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Every page needs a logged-in user, except Login, Error and Privacy.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToPage("/Privacy");
});

// Cookie login (FR1.1): logged-out users are sent to /Login.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);   // One working day
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;                   // JavaScript can't read the cookie
    });

// Repositories: one new instance per web request.
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<SupplierRepository>();
builder.Services.AddScoped<ProductRepository>();

// Services: business logic that sits between the pages and the repositories.
builder.Services.AddScoped<AuthService>();

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

app.UseAuthentication();   // Who is this user? (reads the login cookie)
app.UseAuthorization();    // Are they allowed on this page?

app.MapRazorPages();

app.Run();