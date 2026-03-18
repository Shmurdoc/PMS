using FluentAssertions;
using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Tests.Unit.Base;

// Concrete test entity classes
file class TestEntity : Entity { }
file class TestAuditableEntity : AuditableEntity { }
file class TestAggregateRoot : AggregateRoot
{
    public void RaiseDomainEvent(IDomainEvent evt) => AddDomainEvent(evt);
}
file class TestAuditableAggregateRoot : AuditableAggregateRoot
{
    public void RaiseDomainEvent(IDomainEvent evt) => AddDomainEvent(evt);
}
file record TestDomainEvent(string Message) : DomainEvent;

file class TestEnumeration : Enumeration
{
    public static readonly TestEnumeration Alpha = new(1, "Alpha");
    public static readonly TestEnumeration Beta = new(2, "Beta");
    public static readonly TestEnumeration Gamma = new(3, "Gamma");
    private TestEnumeration(int value, string name) : base(value, name) { }
}

public class EntityTests
{
    [Fact]
    public void NewEntity_HasGuidId()
    {
        var entity = new TestEntity();
        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void NewEntity_HasTimestamps()
    {
        var entity = new TestEntity();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Entity_Equality_SameId_AreEqual()
    {
        var e1 = new TestEntity();
        var e2 = new TestEntity();
        // Different IDs, so not equal
        e1.Should().NotBe(e2);

        // Same reference
        e1.Equals(e1).Should().BeTrue();
    }

    [Fact]
    public void Entity_GetHashCode_DependsOnId()
    {
        var e = new TestEntity();
        e.GetHashCode().Should().Be(e.Id.GetHashCode());
    }

    [Fact]
    public void Entity_Equality_NullComparison()
    {
        var e = new TestEntity();
        e.Equals(null).Should().BeFalse();
        (e == null).Should().BeFalse();
        (null == e).Should().BeFalse();
    }

    [Fact]
    public void Entity_Operator_Inequality()
    {
        var e1 = new TestEntity();
        var e2 = new TestEntity();
        (e1 != e2).Should().BeTrue();
    }
}

public class AuditableEntityTests
{
    [Fact]
    public void SetCreatedBy_SetsUserId()
    {
        var entity = new TestAuditableEntity();
        var userId = Guid.NewGuid();
        entity.SetCreatedBy(userId);
        entity.CreatedByUserId.Should().Be(userId);
    }

    [Fact]
    public void SetModifiedBy_SetsUserIdAndUpdatesTimestamp()
    {
        var entity = new TestAuditableEntity();
        var userId = Guid.NewGuid();
        entity.SetModifiedBy(userId);

        entity.LastModifiedByUserId.Should().Be(userId);
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SoftDelete_SetsFlags()
    {
        var entity = new TestAuditableEntity();
        var userId = Guid.NewGuid();
        entity.SoftDelete(userId);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.DeletedByUserId.Should().Be(userId);
    }

    [Fact]
    public void SoftDelete_NullUserId_Allowed()
    {
        var entity = new TestAuditableEntity();
        entity.SoftDelete(null);
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedByUserId.Should().BeNull();
    }

    [Fact]
    public void Restore_ClearsDeleteFlags()
    {
        var entity = new TestAuditableEntity();
        entity.SoftDelete(Guid.NewGuid());
        entity.Restore();

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedByUserId.Should().BeNull();
    }
}

public class AggregateRootTests
{
    [Fact]
    public void AddDomainEvent_AddsToCollection()
    {
        var agg = new TestAggregateRoot();
        agg.RaiseDomainEvent(new TestDomainEvent("test"));
        agg.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAll()
    {
        var agg = new TestAggregateRoot();
        agg.RaiseDomainEvent(new TestDomainEvent("a"));
        agg.RaiseDomainEvent(new TestDomainEvent("b"));
        agg.ClearDomainEvents();
        agg.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_IsReadOnly()
    {
        var agg = new TestAggregateRoot();
        agg.DomainEvents.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }
}

public class AuditableAggregateRootTests
{
    [Fact]
    public void HasBothAuditAndDomainEventCapabilities()
    {
        var agg = new TestAuditableAggregateRoot();
        agg.SetCreatedBy(Guid.NewGuid());
        agg.RaiseDomainEvent(new TestDomainEvent("test"));
        agg.SoftDelete(Guid.NewGuid());

        agg.CreatedByUserId.Should().NotBeNull();
        agg.DomainEvents.Should().HaveCount(1);
        agg.IsDeleted.Should().BeTrue();
    }
}

public class DomainEventTests
{
    [Fact]
    public void DomainEvent_HasOccurredAt()
    {
        var evt = new TestDomainEvent("hello");
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public class EnumerationTests
{
    [Fact]
    public void GetAll_ReturnsAllStaticFields()
    {
        var all = Enumeration.GetAll<TestEnumeration>().ToList();
        all.Should().HaveCount(3);
    }

    [Fact]
    public void FromValue_ValidValue_ReturnsCorrect()
    {
        var result = Enumeration.FromValue<TestEnumeration>(2);
        result.Name.Should().Be("Beta");
    }

    [Fact]
    public void FromValue_InvalidValue_Throws()
    {
        var act = () => Enumeration.FromValue<TestEnumeration>(99);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromName_ValidName_ReturnsCorrect()
    {
        var result = Enumeration.FromName<TestEnumeration>("gamma");
        result.Value.Should().Be(3);
    }

    [Fact]
    public void FromName_CaseInsensitive()
    {
        var result = Enumeration.FromName<TestEnumeration>("ALPHA");
        result.Value.Should().Be(1);
    }

    [Fact]
    public void FromName_InvalidName_Throws()
    {
        var act = () => Enumeration.FromName<TestEnumeration>("Delta");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        TestEnumeration.Alpha.ToString().Should().Be("Alpha");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        TestEnumeration.Alpha.Equals(TestEnumeration.Alpha).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        TestEnumeration.Alpha.Equals(TestEnumeration.Beta).Should().BeFalse();
    }

    [Fact]
    public void CompareTo_ReturnsCorrectOrder()
    {
        TestEnumeration.Alpha.CompareTo(TestEnumeration.Beta).Should().BeLessThan(0);
        TestEnumeration.Gamma.CompareTo(TestEnumeration.Alpha).Should().BeGreaterThan(0);
    }
}
