namespace Pms.Domain.Exceptions;

/// <summary>Base class for expected business-rule failures (mapped to 4xx by the API).</summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>A requested entity was not found (404).</summary>
public sealed class NotFoundException(string entity, object key)
    : DomainException($"{entity} '{key}' was not found.");

/// <summary>A business invariant was violated, e.g. illegal status transition (400).</summary>
public sealed class BusinessRuleException(string message) : DomainException(message);

/// <summary>A conflicting state was detected, e.g. an overlapping booking (409).</summary>
public sealed class ConflictException(string message) : DomainException(message);

/// <summary>The tenant license is missing, expired or over its plan limit (402/403).</summary>
public sealed class LicenseException(string message) : DomainException(message);
