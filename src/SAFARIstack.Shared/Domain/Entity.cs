using System.ComponentModel.DataAnnotations;

namespace SAFARIstack.Shared.Domain;

// ─── Cross-Cutting Interfaces ────────────────────────────────────────
/// <summary>
/// Marks entities that support soft deletion
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    Guid? DeletedByUserId { get; }
    void SoftDelete(Guid? userId);
    void Restore();
}

/// <summary>
/// Marks entities with full audit trail
/// </summary>
public interface IAuditable
{
    Guid? CreatedByUserId { get; }
    Guid? LastModifiedByUserId { get; }
    void SetCreatedBy(Guid userId);
    void SetModifiedBy(Guid userId);
}

/// <summary>
/// Marks entities scoped to a specific property (multi-tenancy)
/// </summary>
public interface IMultiTenant
{
    Guid PropertyId { get; }
}

// ─── Base Entity ─────────────────────────────────────────────────────
/// <summary>
/// Base entity with strongly-typed UUID identifier
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optimistic concurrency token — EF Core auto-increments on each save.
    /// </summary>
    [Timestamp]
    public uint RowVersion { get; set; }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !Equals(left, right);
    }
}

// ─── Auditable Entity ────────────────────────────────────────────────
/// <summary>
/// Entity with full audit trail and soft-delete capability
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable, ISoftDeletable
{
    public Guid? CreatedByUserId { get; private set; }
    public Guid? LastModifiedByUserId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }

    public void SetCreatedBy(Guid userId) => CreatedByUserId = userId;
    public void SetModifiedBy(Guid userId)
    {
        LastModifiedByUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete(Guid? userId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedByUserId = userId;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
    }
}

// ─── Aggregate Root ──────────────────────────────────────────────────
/// <summary>
/// Base aggregate root with domain events
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

// ─── Auditable Aggregate Root ────────────────────────────────────────
/// <summary>
/// Aggregate root with full audit, soft-delete, and domain event support
/// </summary>
public abstract class AuditableAggregateRoot : AggregateRoot, IAuditable, ISoftDeletable
{
    public Guid? CreatedByUserId { get; private set; }
    public Guid? LastModifiedByUserId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }

    public void SetCreatedBy(Guid userId) => CreatedByUserId = userId;
    public void SetModifiedBy(Guid userId)
    {
        LastModifiedByUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete(Guid? userId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedByUserId = userId;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
    }
}

// ─── Enumeration Base ────────────────────────────────────────────────
/// <summary>
/// Base class for type-safe enumerations (replaces magic strings)
/// </summary>
public abstract class Enumeration : IComparable
{
    public int Value { get; }
    public string Name { get; }

    protected Enumeration(int value, string name) => (Value, Name) = (value, name);

    public override string ToString() => Name;
    public override bool Equals(object? obj) =>
        obj is Enumeration other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(object? other) => Value.CompareTo(((Enumeration)other!).Value);

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.Static |
                            System.Reflection.BindingFlags.DeclaredOnly)
                 .Select(f => f.GetValue(null))
                 .Cast<T>();

    public static T FromValue<T>(int value) where T : Enumeration =>
        GetAll<T>().FirstOrDefault(e => e.Value == value)
        ?? throw new ArgumentException($"No {typeof(T).Name} with value {value}");

    public static T FromName<T>(string name) where T : Enumeration =>
        GetAll<T>().FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"No {typeof(T).Name} with name '{name}'");
}
