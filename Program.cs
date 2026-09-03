global using Demo.Models;
global using Demo.Hubs;
global using Demo;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
// builder.Services.AddSqlServer<DB>($@"
//     Data Source=(LocalDB)\MSSQLLocalDB;
//     AttachDbFilename={builder.Environment.ContentRootPath}\PawPointDB.mdf;
//     Initial Catalog=PawPointCourseDB;
// ");
builder.Services.AddSqlServer<DB>(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddScoped<Helper>();
builder.Services.AddAntiforgery();
builder.Services.AddAuthentication().AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    db.Database.EnsureCreated();
    var hp = scope.ServiceProvider.GetRequiredService<Helper>();
    SeedData.Initialize(db, hp);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization("en-MY");
app.UseAntiforgery();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.MapHub<ChatHub>("/chatHub");
app.Run();
