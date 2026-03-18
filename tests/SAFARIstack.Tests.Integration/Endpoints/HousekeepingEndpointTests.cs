using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/housekeeping/* endpoints — task creation, assignment,
/// start, complete, inspect lifecycle via HTTP.
/// </summary>
[Collection("WebApp")]
public class HousekeepingEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public HousekeepingEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateHkTask_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/housekeeping", new
        {
            PropertyId = _factory.PropertyAId,
            RoomId = _factory.RoomAId,
            TaskType = 0, // HousekeepingTaskType enum
            ScheduledDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Priority = 0  // Normal
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("id");
    }

    [Fact]
    public async Task CreateHkTask_WithAssignment_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/housekeeping", new
        {
            PropertyId = _factory.PropertyAId,
            RoomId = _factory.RoomAId,
            TaskType = 0,
            ScheduledDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Priority = 0,
            AssignedToStaffId = _factory.StaffAId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetPendingTasks_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync(
            $"/api/housekeeping/tasks/{_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task AssignTask_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync(
            $"/api/housekeeping/{Guid.NewGuid()}/assign?staffId={_factory.StaffAId}", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartTask_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync($"/api/housekeeping/{Guid.NewGuid()}/start", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InspectTask_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync($"/api/housekeeping/{Guid.NewGuid()}/inspect", new
        {
            InspectorStaffId = _factory.StaffAId,
            Passed = true,
            Notes = "All good"
        });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateHkTask_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/housekeeping", new
        {
            PropertyId = _factory.PropertyAId,
            RoomId = _factory.RoomAId,
            TaskType = 0,
            ScheduledDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
