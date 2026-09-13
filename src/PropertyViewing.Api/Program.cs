using PropertyViewing.Application.Exceptions;
using PropertyViewing.Infrastructure;
using PropertyViewing.Application;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddPolicy("ViteDevelopment", policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var (status, message) = error switch 
    { 
        ValidationException e => (400, e.Message), 
        NotFoundException e => (404, e.Message), 
        BookingConflictException e => (409, e.Message), 
        _ => (500, "An unexpected error occurred.") 
    };
    context.Response.StatusCode = status; await context.Response.WriteAsJsonAsync(new PropertyViewing.Api.DTOs.ErrorResponse(status, message));
}));
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Property Viewing API v1");
        c.RoutePrefix = string.Empty;
    });
}
app.UseCors("ViteDevelopment");
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program { }
