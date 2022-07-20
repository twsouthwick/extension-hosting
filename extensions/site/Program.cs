using Extension;
using Extension.Manager;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Loader;

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

app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (ExtensionException e)
    {
        ctx.Response.StatusCode = 400;

        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = e.Message,
        });
    }
});

app.MapPost("/extensions", ([FromBody] ExtensionPath path, IExtensionManager manager) => manager.AddAsync(path.Path));
app.MapDelete("/extensions", ([FromBody] ExtensionPath path, IExtensionManager manager) => manager.DeleteAsync(path.Path));
app.MapGet("/extensions", (IExtensionManager manager) => manager.Extensions);
app.MapPost("/run", (IExtensionManager manager, CancellationToken token) => manager.RunAsync(token));
app.MapGet("/contexts", () => AssemblyLoadContext.All.Select(a => a.Name));

app.Run();

record ExtensionPath(string Path);

