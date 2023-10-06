using Microsoft.EntityFrameworkCore;
using WebStoreApiEF.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/O enAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string connexionString = builder.Configuration.GetConnectionString("beststore")!;
    options.UseSqlServer(connexionString);
});

builder.Services.AddScoped<EmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles(); // enable static file to share files 

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
