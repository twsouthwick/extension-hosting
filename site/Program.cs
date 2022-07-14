using Extension.Manager;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExtensionManagement();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/extensions", ([FromBody] ExtensionPath path, IExtensionManager manager) => manager.AddAsync(path.Path));
app.MapDelete("/extensions", ([FromBody] ExtensionPath path, IExtensionManager manager) => manager.DeleteAsync(path.Path));
app.MapGet("/extensions", (IExtensionManager manager) => manager.Extensions);
app.MapPost("/run", (IExtensionManager manager, CancellationToken token) => manager.RunAsync(token));


app.Run();
record ExtensionPath (string Path);

