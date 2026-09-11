using OrderFlow.Api.Authentication;
using OrderFlow.Api.Checkouts;
using OrderFlow.Api.Health;
using OrderFlow.Api.Inventory;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Persistence;
using OrderFlow.Api.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOrderFlowAuthentication(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddOrderFlowHealthChecks();
builder.Services.AddProducts();
builder.Services.AddInventory();
builder.Services.AddCheckouts();
builder.Services.AddOrders();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapOrderFlowHealthChecks();
app.MapProducts();
app.MapInventory();
app.MapCheckouts();
app.MapOrders();

await app.InitializeDatabaseAsync();
await app.RunAsync();

public partial class Program;
