namespace Inventara.Application.Common.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);
