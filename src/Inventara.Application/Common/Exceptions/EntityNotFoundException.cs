namespace Inventara.Application.Common.Exceptions;

public sealed class EntityNotFoundException(string message) : Exception(message);
