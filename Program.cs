using System.Globalization;
using Microsoft.AspNetCore.Localization;
using GarmentLotManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IDataService, SqliteDataService>();

// Set default culture to en-GB so dd/MM/yyyy is standard throughout application
var defaultCulture = new CultureInfo("en-GB");
defaultCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
defaultCulture.DateTimeFormat.DateSeparator = "/";

CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

var app = builder.Build();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new[] { defaultCulture },
    SupportedUICultures = new[] { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
