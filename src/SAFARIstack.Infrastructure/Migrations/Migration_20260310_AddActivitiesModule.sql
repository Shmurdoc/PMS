-- Migration: Add Activities Module Tables (Safari Operations)
-- Date: 2026-03-10
-- Purpose: Support for safari/activity scheduling, guide assignments, and guest experiences

BEGIN;

-- Activity types/catalog
CREATE TABLE IF NOT EXISTS activities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(100), -- 'safari_game_drive', 'guided_tour', 'water_activity', 'wellness', etc.
    duration_minutes INT NOT NULL DEFAULT 60,
    max_capacity INT NOT NULL DEFAULT 8,
    base_price DECIMAL(10, 2) NOT NULL DEFAULT 0,
    currency_code VARCHAR(3) DEFAULT 'ZAR',
    meeting_location VARCHAR(255),
    difficulty_level VARCHAR(50), -- 'easy', 'moderate', 'challenging'
    min_age_years INT,
    max_age_years INT,
    requires_fitness_level BOOLEAN DEFAULT FALSE,
    season_available VARCHAR(100), -- 'year_round', 'summer', 'winter', 'peak_season', etc.
    vehicle_required VARCHAR(100), -- '4x4', 'minibus', 'boat', none, etc.
    guide_required BOOLEAN NOT NULL DEFAULT TRUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    thumbnail_url TEXT,
    itinerary_json JSONB,
    amenities_json JSONB DEFAULT '[]', -- water, snacks, guide, binoculars, etc.
    created_by_user_id UUID,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX IF NOT EXISTS idx_activities_property_id ON activities(property_id);
CREATE INDEX IF NOT EXISTS idx_activities_category ON activities(category);
CREATE INDEX IF NOT EXISTS idx_activities_is_active ON activities(is_active);

-- Activity instances (scheduled occurrences)
CREATE TABLE IF NOT EXISTS activity_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    activity_id UUID NOT NULL REFERENCES activities(id) ON DELETE CASCADE,
    scheduled_date DATE NOT NULL,
    scheduled_start_time TIME NOT NULL,
    scheduled_end_time TIME NOT NULL,
    available_capacity INT NOT NULL,
    total_capacity INT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'scheduled', -- scheduled, in_progress, completed, cancelled
    guide_id UUID,
    vehicle_id UUID,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_activity_schedules_activity_id ON activity_schedules(activity_id);
CREATE INDEX IF NOT EXISTS idx_activity_schedules_scheduled_date ON activity_schedules(scheduled_date);
CREATE INDEX IF NOT EXISTS idx_activity_schedules_guide_id ON activity_schedules(guide_id);
CREATE INDEX IF NOT EXISTS idx_activity_schedules_status ON activity_schedules(status);

-- Guest bookings for activities
CREATE TABLE IF NOT EXISTS activity_bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    activity_schedule_id UUID NOT NULL REFERENCES activity_schedules(id) ON DELETE CASCADE,
    booking_id UUID NOT NULL,
    guest_id UUID NOT NULL,
    num_guests INT NOT NULL DEFAULT 1,
    guest_names TEXT, -- JSON array of guest names
    special_requests TEXT,
    dietary_requirements VARCHAR(255),
    fitness_level VARCHAR(50),
    paid_price DECIMAL(10, 2),
    payment_status VARCHAR(50) DEFAULT 'unpaid', -- unpaid, partial, paid, refunded
    addons_json JSONB DEFAULT '[]', -- optional add-ons (extra guide, equipment, etc.)
    confirmation_sent_at TIMESTAMP WITH TIME ZONE,
    status VARCHAR(50) NOT NULL DEFAULT 'confirmed', -- confirmed, checked_in, no_show, completed, cancelled
    checked_in_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    feedback_rating INT, -- 1-5 stars
    feedback_comment TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_activity_bookings_schedule_id ON activity_bookings(activity_schedule_id);
CREATE INDEX IF NOT EXISTS idx_activity_bookings_booking_id ON activity_bookings(booking_id);
CREATE INDEX IF NOT EXISTS idx_activity_bookings_guest_id ON activity_bookings(guest_id);
CREATE INDEX IF NOT EXISTS idx_activity_bookings_status ON activity_bookings(status);

-- Activity guides/staff
CREATE TABLE IF NOT EXISTS activity_guides (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    staff_id UUID NOT NULL,
    property_id UUID NOT NULL,
    guide_type VARCHAR(100) NOT NULL, -- 'safari_guide', 'tour_guide', 'wellness_instructor', etc.
    specializations JSONB DEFAULT '[]', -- areas of expertise
    languages_spoken JSONB DEFAULT '[]', -- en, af, xhosa, etc.
    is_certified BOOLEAN NOT NULL DEFAULT FALSE,
    has_vehicle BOOLEAN NOT NULL DEFAULT FALSE,
    max_guests_per_activity INT DEFAULT 8,
    is_available BOOLEAN NOT NULL DEFAULT TRUE,
    availability_schedule JSONB, -- schedule of availability
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_activity_guides_staff_id ON activity_guides(staff_id);
CREATE INDEX IF NOT EXISTS idx_activity_guides_property_id ON activity_guides(property_id);
CREATE INDEX IF NOT EXISTS idx_activity_guides_is_available ON activity_guides(is_available);

COMMIT;
