using Extension.Manager;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExtensionManagement();

var app = builder.Build();

app.MapPost("/extensions", ([FromBody] ExtensionAddition extension, IExtensionManager manager) => manager.Add(extension.Name, extension.Directory));
app.MapGet("/extensions", (IExtensionManager manager) => manager.Extensions);

app.Run();

record ExtensionAddition(string Name, string Directory);

