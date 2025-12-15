using BookingTour.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.DescribeAllParametersInCamelCase();
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});
builder.Services.AddDbContext<TourBookingSystemContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();


    app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api V1");
    //options.RoutePrefix = string.Empty;
});
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TourBookingSystemContext>(); // Đổi tên DbContext cho đúng của bạn

        context.Database.Migrate();
        Console.WriteLine("--> Đã thực hiện Migration Database thành công!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "--> Lỗi khi đang Migration Database.");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowFE");

app.UseAuthorization();

app.MapControllers();

app.Run();
