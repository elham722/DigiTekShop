using DigiTekShop.MVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<ISimpleApiClient, SimpleApiClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config.GetValue<string>("ApiSettings:BaseUrl") ?? "https://localhost:7055";
    
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
