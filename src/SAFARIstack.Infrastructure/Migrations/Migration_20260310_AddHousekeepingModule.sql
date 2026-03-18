-- Migration: Add Housekeeping Module Tables
-- Date: 2026-03-10
-- Purpose: Support for housekeeping task management, mobile app coordination, and quality control

BEGIN;

-- Housekeeping areas/zones
CREATE TABLE IF NOT EXISTS housekeeping_areas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    area_type VARCHAR(100), -- 'room', 'common_area', 'kitchen', 'laundry', 'outdoor', etc.
    order_priority INT DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_housekeeping_areas_property_id ON housekeeping_areas(property_id);
CREATE INDEX IF NOT EXISTS idx_housekeeping_areas_area_type ON housekeeping_areas(area_type);

-- Housekeeping task types
CREATE TABLE IF NOT EXISTS housekeeping_task_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    estimated_duration_minutes INT NOT NULL DEFAULT 30,
    required_skill_level VARCHAR(50), -- 'entry', 'intermediate', 'expert'
    checklist_items_json JSONB DEFAULT '[]', -- items to verify completion
    supplies_needed_json JSONB DEFAULT '[]', -- equipment/supplies needed
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_housekeeping_task_types_property_id ON housekeeping_task_types(property_id);

-- Housekeeping tasks (assignments)
CREATE TABLE IF NOT EXISTS housekeeping_tasks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL,
    area_id UUID NOT NULL REFERENCES housekeeping_areas(id) ON DELETE CASCADE,
    task_type_id UUID REFERENCES housekeeping_task_types(id) ON DELETE SET NULL,
    assigned_to_staff_id UUID,
    task_date DATE NOT NULL,
    scheduled_start_time TIME,
    scheduled_end_time TIME,
    priority VARCHAR(50) NOT NULL DEFAULT 'normal', -- high, normal, low
    status VARCHAR(50) NOT NULL DEFAULT 'pending', -- pending, in_progress, completed, verified, cancelled
    description TEXT,
    special_instructions TEXT,
    estimated_duration_minutes INT,
    qc_required BOOLEAN NOT NULL DEFAULT TRUE,
    qc_checked_by_staff_id UUID,
    qc_status VARCHAR(50), -- pending, pass, fail, needs_rework
    qc_feedback TEXT,
    qc_checked_at TIMESTAMP WITH TIME ZONE,
    started_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_housekeeping_tasks_property_id ON housekeeping_tasks(property_id);
CREATE INDEX IF NOT EXISTS idx_housekeeping_tasks_area_id ON housekeeping_tasks(area_id);
CREATE INDEX IF NOT EXISTS idx_housekeeping_tasks_assigned_to ON housekeeping_tasks(assigned_to_staff_id);
CREATE INDEX IF NOT EXISTS idx_housekeeping_tasks_task_date ON housekeeping_tasks(task_date);
CREATE INDEX IF NOT EXISTS idx_housekeeping_tasks_status ON housekeeping_tasks(status);
CREATE INDEX IF NOT EXISTS idx_housekeeping_tasks_qc_status ON housekeeping_tasks(qc_status);

-- Task completion evidence (photos, notes)
CREATE TABLE IF NOT EXISTS housekeeping_task_evidence (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id UUID NOT NULL REFERENCES housekeeping_tasks(id) ON DELETE CASCADE,
    evidence_type VARCHAR(50) NOT NULL, -- 'before_photo', 'after_photo', 'signature', 'note'
    file_url TEXT,
    file_name VARCHAR(255),
    file_size_bytes INT,
    notes TEXT,
    uploaded_by_staff_id UUID NOT NULL,
    uploaded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_housekeeping_task_evidence_task_id ON housekeeping_task_evidence(task_id);
CREATE INDEX IF NOT EXISTS idx_housekeeping_task_evidence_type ON housekeeping_task_evidence(evidence_type);

-- Staff housekeeping profile
CREATE TABLE IF NOT EXISTS housekeeping_staff (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL,
    staff_id UUID NOT NULL,
    skill_level VARCHAR(50) NOT NULL DEFAULT 'entry', -- entry, intermediate, expert
    is_qc_Inspector BOOLEAN DEFAULT FALSE, -- can perform quality checks
    max_concurrent_tasks INT DEFAULT 5,
    is_available BOOLEAN NOT NULL DEFAULT TRUE,
    availability_schedule JSONB, -- days/hours available
    languages_spoken JSONB DEFAULT '[]',
    certifications JSONB DEFAULT '[]', -- biohazard, safety, etc.
    tasks_completed INT DEFAULT 0,
    qc_pass_rate DECIMAL(5, 2), -- percentage
    avg_rating DECIMAL(3, 2), -- 1-5 stars
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_housekeeping_staff_property_id ON housekeeping_staff(property_id);
CREATE INDEX IF NOT EXISTS idx_housekeeping_staff_staff_id ON housekeeping_staff(staff_id);
CREATE INDEX IF NOT EXISTS idx_housekeeping_staff_skill_level ON housekeeping_staff(skill_level);
CREATE INDEX IF NOT EXISTS idx_housekeeping_staff_is_available ON housekeeping_staff(is_available);

-- Housekeeping schedule templates
CREATE TABLE IF NOT EXISTS housekeeping_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    schedule_type VARCHAR(50), -- 'daily', 'weekly', 'custom'
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    schedule_json JSONB NOT NULL, -- day/time based schedule
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_housekeeping_schedules_property_id ON housekeeping_schedules(property_id);

-- Housekeeping incidents/issues
CREATE TABLE IF NOT EXISTS housekeeping_incidents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL,
    area_id UUID,
    task_id UUID,
    incident_type VARCHAR(100) NOT NULL, -- 'damage', 'theft', 'safety_hazard', 'quality_issue', etc.
    description TEXT NOT NULL,
    severity VARCHAR(50) NOT NULL DEFAULT 'normal', -- critical, high, normal, low
    reported_by_staff_id UUID,
    reported_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_by_staff_id UUID,
    resolved_at TIMESTAMP WITH TIME ZONE,
    resolution_notes TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'open', -- open, investigating, resolved, closed
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_housekeeping_incidents_property_id ON housekeeping_incidents(property_id);
CREATE INDEX IF NOT EXISTS idx_housekeeping_incidents_status ON housekeeping_incidents(status);
CREATE INDEX IF NOT EXISTS idx_housekeeping_incidents_severity ON housekeeping_incidents(severity);

COMMIT;
