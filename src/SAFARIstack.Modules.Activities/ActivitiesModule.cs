using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SAFARIstack.Modules.Activities;

/// <summary>
/// Activities & Safari Operations Module
/// Handles scheduling, guide assignments, and guest bookings for activities/experiences
/// </summary>
public class ActivitiesModule
{
    /// <summary>
    /// Module name
    /// </summary>
    public const string ModuleName = "Activities & Safari Operations";

    /// <summary>
    /// Register activities module services
    /// </summary>
    public static void RegisterServices(IServiceCollection services)
    {
        // TODO: Register activity-specific services
        // - IActivityService
        // - IActivityScheduleService
        // - IActivityBookingService
        // - IActivityGuideService
        // - IActivitiesRepository
        
        // Service implementations
        // services.AddScoped<IActivityService, ActivityService>();
        // services.AddScoped<IActivityScheduleService, ActivityScheduleService>();
        // services.AddScoped<IActivityBookingService, ActivityBookingService>();
        // services.AddScoped<IActivityGuideService, ActivityGuideService>();
    }

    /// <summary>
    /// Register activities module endpoints
    /// </summary>
    public static void MapEndpoints(WebApplication app)
    {
        // TODO: Map activity endpoints
        // - GET /api/v1/activities
        // - GET /api/v1/activities/{id}
        // - POST /api/v1/activities
        // - PUT /api/v1/activities/{id}
        // - DELETE /api/v1/activities/{id}
        
        // - GET /api/v1/activities/{id}/schedules
        // - POST /api/v1/activities/{id}/schedules
        
        // - GET /api/v1/activity-bookings
        // - POST /api/v1/activity-bookings (guest booking)
        // - POST /api/v1/activity-bookings/{id}/check-in
        // - POST /api/v1/activity-bookings/{id}/feedback
        
        // - GET /api/v1/activity-guides
        // - GET /api/v1/activity-guides/{id}/schedule
    }
}

/// <summary>
/// Activities module features
/// </summary>
public class ActivitiesModuleFeatures
{
    // Activity Management
    public const string ActivityCatalog = "Activity catalog management";
    public const string ActivityScheduling = "Schedule multiple instances per activity";
    public const string SeasonalAvailability = "Seasonal availability management";
    public const string DifficultyLevels = "Difficulty level categorization";
    
    // Guide Management
    public const string GuideAssignment = "Assign guides to activities";
    public const string GuideSpecializations = "Guide specializations tracking";
    public const string GuideAvailability = "Guide availability scheduling";
    
    // Booking Management
    public const string GuestBookings = "Guest bookings for activities";
    public const string BookingConfirmations = "Automated booking confirmations";
    public const string CapacityManagement = "Real-time capacity tracking";
    
    // Guest Experience
    public const string SpecialRequests = "Special requests handling";
    public const string DietaryRequirements = "Dietary requirements tracking";
    public const string AddOnServices = "Activity add-ons (extra guide, equipment)";
    public const string GuestFeedback = "Post-activity feedback & ratings";
    
    // Operations
    public const string CheckInProcess = "Mobile check-in process";
    public const string CompletionTracking = "Completion & performance tracking";
    public const string MultiLanguageSupport = "Multi-language guide support";
    
    // Reporting
    public const string ActivityRevenue = "Revenue tracking per activity";
    public const string GuestSatisfaction = "Satisfaction metrics & analytics";
    public const string GuidePerformance = "Guide performance analytics";
    public const string CapacityMetrics = "Capacity utilization metrics";
}

/*
 * ===================================
 * Phase 1 Week 1 - Implementation Plan
 * ===================================
 * 
 * COMPLETED (Week 1):
 * ✅ Domain entities (Activity, ActivitySchedule, ActivityBooking, ActivityGuide)
 * ✅ Database schema & migrations
 * ✅ Core interfaces & service contracts
 * ✅ Module registration framework
 * 
 * SCHEDULED (Week 2):
 * ⏳ Repository implementations (EF Core)
 * ⏳ Service implementations (CQRS with MediatR)
 * ⏳ Unit tests (minimum 85% coverage)
 * ⏳ Integration tests (full workflows)
 * 
 * SCHEDULED (Week 3):
 * ⏳ API endpoints (11 total)
 * ⏳ OpenAPI documentation
 * ⏳ Request/response validation
 * ⏳ Error handling & logging
 * 
 * SCHEDULED (Week 4):
 * ⏳ Mobile app integration (activities listing, booking)
 * ⏳ Guest experience flows
 * ⏳ Multi-language support
 * ⏳ Performance optimization & caching
 * 
 * Features Roadmap:
 * - Basic CRUD: Week 2
 * - Scheduling: Week 2-3
 * - Bookings: Week 3
 * - Analytics: Week 4
 * - Integration: Week 4
 * 
 * Expected Completion: March 31, 2026
 * Endpoints by Launch: 15+ Activity-related endpoints
 * Expected ROI Impact: $50K-150K Year 1 (premium experiences)
 */
