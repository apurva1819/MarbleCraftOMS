namespace MarbleCraftOMS.Core.Exceptions;

/// <summary>
/// Thrown when a request is syntactically valid but semantically invalid —
/// e.g. referencing a related entity that does not exist. Maps to HTTP 422.
/// </summary>
public sealed class UnprocessableEntityException(string message) : Exception(message);
