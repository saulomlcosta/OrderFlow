using OrderFlow.Api.Checkouts;
using OrderFlow.Api.Inventory;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Persistence;
using OrderFlow.Api.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddProducts();
builder.Services.AddInventory();
builder.Services.AddCheckouts();
builder.Services.AddOrders();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapProducts();
app.MapInventory();
app.MapCheckouts();
app.MapOrders();

await app.InitializeDatabaseAsync();
await app.RunAsync();

public partial class Program;
