
using FixNear.Features.Messaging.Hubs;
using FixNear.Infrastructure.Configurations;
using FixNear.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Services
builder.Services.AddFixNearServices(builder.Configuration);

// 2. Register Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);

// 3. Register Swagger
builder.Services.AddFixNearSwagger();

// 4. Register Controllers
builder.Services.AddControllersWithViews();

// 5. Chat
builder.Services.AddSignalR();

var app = builder.Build();

// 6. Configure Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

app.MapHub<ChatHub>("/hubs/chat");

app.Run();
