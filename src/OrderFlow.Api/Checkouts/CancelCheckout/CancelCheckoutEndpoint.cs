using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Authentication;
using OrderFlow.Api.Common;
using OrderFlow.Api.Persistence;

namespace OrderFlow.Api.Checkouts.CancelCheckout;

internal static class CancelCheckoutEndpoint
{
    internal static void MapCancelCheckout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/checkouts/{id:guid}/cancel", HandleAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ClaimsPrincipal user,
        OrderFlowDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var customerSubject = user.GetSubject();

        if (customerSubject is null)
        {
            return Results.Unauthorized();
        }

        var checkout = await dbContext.Checkouts.SingleOrDefaultAsync(
            x => x.Id == id && x.CustomerSubject == customerSubject,
            cancellationToken);

        if (checkout is null)
        {
            return Results.NotFound();
        }

        var now = timeProvider.GetUtcNow();

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            checkout.Cancel(now);

            foreach (var item in checkout.Items)
            {
                var affectedRows = await dbContext.Inventories
                    .Where(x => x.ProductId == item.ProductId && x.ReservedQuantity >= item.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            inventory => inventory.ReservedQuantity,
                            inventory => inventory.ReservedQuantity - item.Quantity),
                        cancellationToken);

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["checkout"] = ["The reservation could not be released consistently."]
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Ok(checkout.ToResponse(now));
        }
        catch (DomainValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["checkout"] = [exception.Message]
            });
        }
    }
}
