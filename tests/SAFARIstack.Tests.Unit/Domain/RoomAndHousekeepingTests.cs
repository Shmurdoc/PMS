using FluentAssertions;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Tests.Unit.Domain;

public class RoomTypeTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var rt = RoomType.Create(Guid.NewGuid(), "Deluxe Suite", "DLX", 2500, 4, 2, 2);
        rt.Name.Should().Be("Deluxe Suite");
        rt.Code.Should().Be("DLX");
        rt.BasePrice.Should().Be(2500);
        rt.MaxGuests.Should().Be(4);
        rt.MaxAdults.Should().Be(2);
        rt.MaxChildren.Should().Be(2);
        rt.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdatePricing_ValidPrice_Updates()
    {
        var rt = RoomType.Create(Guid.NewGuid(), "Std", "STD", 1000, 2);
        rt.UpdatePricing(1500);
        rt.BasePrice.Should().Be(1500);
    }

    [Fact]
    public void UpdatePricing_NegativePrice_Throws()
    {
        var rt = RoomType.Create(Guid.NewGuid(), "Std", "STD", 1000, 2);
        var act = () => rt.UpdatePricing(-1);
        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var rt = RoomType.Create(Guid.NewGuid(), "Std", "STD", 1000, 2);
        rt.Deactivate();
        rt.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsActive()
    {
        var rt = RoomType.Create(Guid.NewGuid(), "Std", "STD", 1000, 2);
        rt.Deactivate();
        rt.Activate();
        rt.IsActive.Should().BeTrue();
    }
}

public class RoomTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();
    private static readonly Guid RoomTypeId = Guid.NewGuid();

    [Fact]
    public void Create_SetsAllProperties()
    {
        var room = Room.Create(PropertyId, RoomTypeId, "101", 1, "North");
        room.PropertyId.Should().Be(PropertyId);
        room.RoomTypeId.Should().Be(RoomTypeId);
        room.RoomNumber.Should().Be("101");
        room.Floor.Should().Be(1);
        room.Wing.Should().Be("North");
        room.Status.Should().Be(RoomStatus.Available);
        room.HkStatus.Should().Be(HousekeepingStatus.Clean);
        room.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        var room = Room.Create(PropertyId, RoomTypeId, "102");
        room.UpdateStatus(RoomStatus.Occupied);
        room.Status.Should().Be(RoomStatus.Occupied);
    }

    [Fact]
    public void MarkClean_SetsHkStatusAndLastCleaned()
    {
        var room = Room.Create(PropertyId, RoomTypeId, "103");
        room.MarkDirty();
        room.UpdateStatus(RoomStatus.Cleaning);

        var cleanedAt = DateTime.UtcNow;
        room.MarkClean(cleanedAt);

        room.HkStatus.Should().Be(HousekeepingStatus.Clean);
        room.LastCleanedAt.Should().Be(cleanedAt);
        room.Status.Should().Be(RoomStatus.Available); // Auto-transitions from Cleaning
    }

    [Fact]
    public void MarkClean_NotFromCleaning_KeepsCurrentStatus()
    {
        var room = Room.Create(PropertyId, RoomTypeId, "104");
        room.UpdateStatus(RoomStatus.Occupied);
        room.MarkClean(DateTime.UtcNow);

        room.HkStatus.Should().Be(HousekeepingStatus.Clean);
        room.Status.Should().Be(RoomStatus.Occupied); // Doesn't change from Occupied
    }

    [Fact]
    public void MarkDirty_SetsHkStatus()
    {
        var room = Room.Create(PropertyId, RoomTypeId, "105");
        room.MarkDirty();
        room.HkStatus.Should().Be(HousekeepingStatus.Dirty);
    }

    [Fact]
    public void PutOutOfService_SetsStatusAndNotes()
    {
        var room = Room.Create(PropertyId, RoomTypeId, "106");
        room.PutOutOfService("Broken AC");
        room.Status.Should().Be(RoomStatus.OutOfService);
        room.Notes.Should().Be("Broken AC");
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var room = Room.Create(PropertyId, RoomTypeId, "107");
        room.Deactivate();
        room.IsActive.Should().BeFalse();
    }
}

public class RoomBlockTests
{
    [Fact]
    public void Create_ValidRange_Succeeds()
    {
        var block = RoomBlock.Create(Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5),
            RoomBlockReason.Renovation, "Full refurb");

        block.Reason.Should().Be(RoomBlockReason.Renovation);
        block.Notes.Should().Be("Full refurb");
    }

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        var act = () => RoomBlock.Create(Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(1),
            RoomBlockReason.Maintenance);
        act.Should().Throw<ArgumentException>().WithMessage("*after*");
    }
}

public class HousekeepingTaskTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();
    private static readonly Guid RoomId = Guid.NewGuid();
    private static readonly Guid StaffId = Guid.NewGuid();

    private static HousekeepingTask CreateTask(HousekeepingTaskStatus? desiredStatus = null)
    {
        var task = HousekeepingTask.Create(PropertyId, RoomId, HousekeepingTaskType.Turnover, DateTime.UtcNow);
        if (desiredStatus == HousekeepingTaskStatus.Assigned)
            task.AssignTo(StaffId);
        else if (desiredStatus == HousekeepingTaskStatus.InProgress)
        {
            task.AssignTo(StaffId);
            task.Start();
        }
        else if (desiredStatus == HousekeepingTaskStatus.Completed)
        {
            task.AssignTo(StaffId);
            task.Start();
            task.Complete(true, true, true, true, true);
        }
        return task;
    }

    [Fact]
    public void Create_SetsAllProperties()
    {
        var task = HousekeepingTask.Create(PropertyId, RoomId, HousekeepingTaskType.DeepClean, DateTime.UtcNow, HousekeepingPriority.High);
        task.PropertyId.Should().Be(PropertyId);
        task.RoomId.Should().Be(RoomId);
        task.TaskType.Should().Be(HousekeepingTaskType.DeepClean);
        task.Priority.Should().Be(HousekeepingPriority.High);
        task.Status.Should().Be(HousekeepingTaskStatus.Pending);
    }

    [Fact]
    public void Create_RaisesEvent()
    {
        var task = HousekeepingTask.Create(PropertyId, RoomId, HousekeepingTaskType.Turnover, DateTime.UtcNow);
        task.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<HousekeepingTaskCreatedEvent>();
    }

    [Fact]
    public void AssignTo_SetsStaffAndStatus()
    {
        var task = CreateTask();
        task.AssignTo(StaffId);
        task.AssignedToStaffId.Should().Be(StaffId);
        task.Status.Should().Be(HousekeepingTaskStatus.Assigned);
    }

    [Fact]
    public void Start_FromAssigned_Works()
    {
        var task = CreateTask(HousekeepingTaskStatus.Assigned);
        task.Start();
        task.Status.Should().Be(HousekeepingTaskStatus.InProgress);
        task.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Start_FromPending_Works()
    {
        var task = CreateTask();
        task.Start();
        task.Status.Should().Be(HousekeepingTaskStatus.InProgress);
    }

    [Fact]
    public void Start_FromCompleted_Throws()
    {
        var task = CreateTask(HousekeepingTaskStatus.Completed);
        var act = () => task.Start();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_FromInProgress_SetsChecklist()
    {
        var task = CreateTask(HousekeepingTaskStatus.InProgress);
        task.Complete(true, true, false, true, false);

        task.Status.Should().Be(HousekeepingTaskStatus.Completed);
        task.LinenChanged.Should().BeTrue();
        task.BathroomCleaned.Should().BeTrue();
        task.FloorsCleaned.Should().BeFalse();
        task.MinibarRestocked.Should().BeTrue();
        task.AmenitiesReplenished.Should().BeFalse();
        task.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        task.DurationMinutes.Should().NotBeNull();
    }

    [Fact]
    public void Complete_NotInProgress_Throws()
    {
        var task = CreateTask(HousekeepingTaskStatus.Assigned);
        var act = () => task.Complete(true, true, true, true, true);
        act.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
    }

    [Fact]
    public void Inspect_Passed_StatusInspected()
    {
        var task = CreateTask(HousekeepingTaskStatus.Completed);
        var inspectorId = Guid.NewGuid();
        task.Inspect(inspectorId, true, "Looks great");

        task.Status.Should().Be(HousekeepingTaskStatus.Inspected);
        task.PassedInspection.Should().BeTrue();
        task.InspectedByStaffId.Should().Be(inspectorId);
        task.InspectionNotes.Should().Be("Looks great");
    }

    [Fact]
    public void Inspect_Failed_StatusFailedInspection()
    {
        var task = CreateTask(HousekeepingTaskStatus.Completed);
        task.Inspect(Guid.NewGuid(), false, "Bathroom not clean");

        task.Status.Should().Be(HousekeepingTaskStatus.FailedInspection);
        task.PassedInspection.Should().BeFalse();
    }

    [Fact]
    public void Inspect_NotCompleted_Throws()
    {
        var task = CreateTask(HousekeepingTaskStatus.InProgress);
        var act = () => task.Inspect(Guid.NewGuid(), true, null);
        act.Should().Throw<InvalidOperationException>().WithMessage("*completed*");
    }

    [Fact]
    public void FullLifecycle_PendingToInspected()
    {
        var task = HousekeepingTask.Create(PropertyId, RoomId, HousekeepingTaskType.Turnover, DateTime.UtcNow);
        task.Status.Should().Be(HousekeepingTaskStatus.Pending);

        task.AssignTo(StaffId);
        task.Status.Should().Be(HousekeepingTaskStatus.Assigned);

        task.Start();
        task.Status.Should().Be(HousekeepingTaskStatus.InProgress);

        task.Complete(true, true, true, true, true);
        task.Status.Should().Be(HousekeepingTaskStatus.Completed);

        task.Inspect(Guid.NewGuid(), true, "Perfect");
        task.Status.Should().Be(HousekeepingTaskStatus.Inspected);
    }
}

public class AmenityTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var amenity = Amenity.Create(Guid.NewGuid(), "Free WiFi", AmenityCategory.RoomBasic, "wifi-icon");
        amenity.Name.Should().Be("Free WiFi");
        amenity.Category.Should().Be(AmenityCategory.RoomBasic);
        amenity.Icon.Should().Be("wifi-icon");
        amenity.IsActive.Should().BeTrue();
    }
}

public class RoomTypeAmenityTests
{
    [Fact]
    public void Create_SetsJoinIds()
    {
        var rtId = Guid.NewGuid();
        var aId = Guid.NewGuid();
        var rta = RoomTypeAmenity.Create(rtId, aId);
        rta.RoomTypeId.Should().Be(rtId);
        rta.AmenityId.Should().Be(aId);
    }
}
