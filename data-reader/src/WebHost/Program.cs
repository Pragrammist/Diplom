using Hangfire;
using Microsoft.Playwright;
using StackExchange.Redis;
using Hangfire.Pro.Redis;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebHost.Hubs;
using Microsoft.AspNetCore.SignalR;
using static System.Formats.Asn1.AsnWriter;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;
var redisConnections = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379";

var options = new RedisStorageOptions
{
    Prefix = "hangfire:"
};


services.AddHttpClient();
services.AddHttpClient("yandexMarketClient", opt => {
    opt.BaseAddress = new Uri("https://market.yandex.ru");
});
services.AddControllersWithViews();

var sqlServerConStr = "Server=(localdb)\\mssqllocaldb;Integrated Security=SSPI;TrustServerCertificate=True;Encrypt=False;Database=Hangfire;";
// Add Hangfire services.
services.AddHangfire(
    configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    // .UseRedisStorage(redisConnections, options)
    .UseSqlServerStorage(sqlServerConStr)
    .WithJobExpirationTimeout(TimeSpan.FromMinutes(20))
);

// Add the processing server as IHostedService
services.AddHangfireServer();



services.AddSingleton(ConnectionMultiplexer.Connect(redisConnections));
services.AddSignalR();

services.AddSingleton(services => {
    var multRedis = services.GetRequiredService<ConnectionMultiplexer>();
    return multRedis.GetDatabase();
});


var lableServiceUrl = configuration["LABLE_SERVICE_GRPC_URL"] ?? "http://localhost:50052";
services.AddGrpcClient<LableService.LableServiceClient>(o => {
    o.Address = new Uri(lableServiceUrl);
});

var vectorServiceUrl = configuration["VECTOR_SERVICE_GRPC_URL"] ?? "http://localhost:50051";
services.AddGrpcClient<VectorService.VectorServiceClient>(o => {
    o.Address = new Uri(vectorServiceUrl);
});
services.AddSingleton<DuplexStreamLableService>();
services.AddSingleton<DuplexStreamVectorService>();



services.AddTransient<MarketPlacesObservable>();
services.AddTransient<MarketPlacesObserver>();
services.AddSingleton<DataLoaderService>();
services.AddTransient<IMarketPlaceRepository, MarketPlaceRepository>();

     

services.AddSingleton(s => {
    var pw = Playwright.CreateAsync();
    pw.Wait();
    return pw.Result;
});


// установка конфигурации подключения
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => //CookieAuthenticationOptions
    {
        options.LoginPath = new PathString("/");
    });

var sqlServerConStr2 = "Server=(localdb)\\mssqllocaldb;Database=UserDataDb;Integrated Security=SSPI;TrustServerCertificate=True;Encrypt=False;";

services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(sqlServerConStr2));

services.AddSingleton(s => {
    var pw = s.GetRequiredService<IPlaywright>();
    var taskLaunch = pw.Chromium.LaunchAsync(
        new BrowserTypeLaunchOptions{
            Headless = true
        });
    taskLaunch.Wait();
    return taskLaunch.Result;
});

services.AddSingleton(s => {
    var browser = s.GetRequiredService<IBrowser>();
    var contextGettingTask = browser.NewContextAsync(new BrowserNewContextOptions
    {
        BaseURL = "https://market.yandex.ru",
        UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36"
    });
    contextGettingTask.Wait();
    return contextGettingTask.Result;
});

services.AddSingleton(s => {
    var browserContext = s.GetRequiredService<IBrowserContext>();
    var contextGettingTask = browserContext.NewPageAsync();
    contextGettingTask.Wait();
    return contextGettingTask.Result;
});


services.AddSingleton<PageLoaderService>();

services.AddTransient<ClusterizationService>();


var app = builder.Build();


app.MapHangfireDashboard();
app.UseStaticFiles();

app.UseAuthentication();    // аутентификация
app.UseAuthorization();     // авторизация
// app.Map("yandex-market-omega-test", 
//     () => BackgroundJob.Enqueue<MarketPlacesObservable>(s => s.NotifyObservers(new MarketplaceIndetificator(
//         "/product--california-gold-nutrition-omega-800-fish-oil-kaps/1665844608?skuId=101612053899&sku=101612053899&show-uid=16983306626964790098306007&do-waremd5=vhw1HGU3892iXpP3ANPe4w&cpc=CBjd56ZYeZR6QxlrNfXoZgSifR0jHJfl6CJ4O9bQeZVlq4m-KdIVoapGJgaeWZ8usckTaEjdkLYpyTJvgrIE9oId4LTJfErDn6LY1KIWxJ4gvNz-REJLsM-SD91S8_NanrgXMhf2LOoidD8jTRlU3P7dv8z_80V0lZhj2zF-3tP0vF7EEfBWtkBIhd0UwBn0Wud_lJv1_dQe6WxGRRJbqyCNw3Pnt1pQDsHkJl_hP2a4wT3jwi0CtSAom7toBYjrpSspXCtDm40%2C&uniqueId=924574&_redirectCount=1",
//         "yandex-market",
//         string.Empty
// ))));

app.Map("/test-hub", async (ctx) =>
{
    var productsHub = ctx.RequestServices.GetRequiredService<IHubContext<ProductsHub>>();

    await productsHub.Clients.All.SendAsync("SetIsLoaded", "Омега-800 Витамины");
});

// app.Map("yandex-market-omega-test-clustering", 
// () =>  BackgroundJob.Enqueue<ClusterizationService>((s) => s.ClusterComments("/product--california-gold-nutrition-omega-800-fish-oil-kaps/1665844608?skuId=101612053899&sku=101612053899&show-uid=16983306626964790098306007&do-waremd5=vhw1HGU3892iXpP3ANPe4w&cpc=CBjd56ZYeZR6QxlrNfXoZgSifR0jHJfl6CJ4O9bQeZVlq4m-KdIVoapGJgaeWZ8usckTaEjdkLYpyTJvgrIE9oId4LTJfErDn6LY1KIWxJ4gvNz-REJLsM-SD91S8_NanrgXMhf2LOoidD8jTRlU3P7dv8z_80V0lZhj2zF-3tP0vF7EEfBWtkBIhd0UwBn0Wud_lJv1_dQe6WxGRRJbqyCNw3Pnt1pQDsHkJl_hP2a4wT3jwi0CtSAom7toBYjrpSspXCtDm40%2C&uniqueId=924574&_redirectCount=1", 0)));

app.MapHub<ProductsHub>("/products-hub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();



