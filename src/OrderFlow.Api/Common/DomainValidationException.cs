namespace OrderFlow.Api.Common;

internal sealed class DomainValidationException(string message) : Exception(message);
