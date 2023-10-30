using Microsoft.Extensions.FileProviders;
using capstone_mongo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using capstone_mongo.Helper;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using capstone_mongo.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB and initialize MongoConfig
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("MONGODB_URI");
var databaseName = configuration["ConnectionStrings:MONGODB_DATABASE"];
var userLoggedIn = false;
var client = new MongoClient(connectionString);
var database = client.GetDatabase(databaseName);
MongoConfig.Initialize(database, userLoggedIn);


// builder services for DI
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

builder.Services.AddScoped<SessionService>();

// mongodb related
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ModuleService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<PeerEvalService>();

builder.Services.AddScoped<GradeService>(sp =>
{
    var sessionService = sp.GetRequiredService<SessionService>();

    var moduleService = sp.GetRequiredService<ModuleService>();
    var studentService = sp.GetRequiredService<StudentService>();
    var teamService = sp.GetRequiredService<TeamService>();
    var evalService = sp.GetRequiredService<PeerEvalService>();


    var module = "";

    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    if (httpContextAccessor.HttpContext.Request.Query.ContainsKey("module"))
    {
        module = httpContextAccessor.HttpContext.Request.Query["module"];
    }

    var gradeService = new GradeService(sp, sessionService, moduleService,
        studentService, teamService, evalService);

    return gradeService;

});


// file services
builder.Services.AddScoped<FileService>();
builder.Services.AddSingleton<IFileProvider>(
            new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

//builder.Services.AddMvc();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/User/Login"; 
            options.AccessDeniedPath = "/User/AccessDenied"; 
        });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseAuthentication();
app.UseAuthorization();


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=User}/{action=Login}/{id?}");
});

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=User}/{action=Login}/{id?}");

app.Run();

