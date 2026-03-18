-- Safety check: ensure this is the correct database
-- Migration: Add NullClaw Security Alerts Table
-- Date: 2026-03-10
-- Purpose: Store security alerts from autonomous threat detection

BEGIN;

-- Create SecurityAlert table
CREATE TABLE IF NOT EXISTS security_alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    alert_type VARCHAR(100) NOT NULL,
    severity INT NOT NULL DEFAULT 0,
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    source VARCHAR(255) NOT NULL,
    affected_resource_id UUID,
    affected_resource_type VARCHAR(100),
    status INT NOT NULL DEFAULT 0,
    autonomous_action INT NOT NULL DEFAULT 0,
    autonomous_action_details TEXT,
    ip_address INET,
    user_id UUID,
    confidence_score INT DEFAULT 50,
    context_json JSONB NOT NULL DEFAULT '{}',
    admin_notes TEXT,
    acknowledged_by_admin_id UUID,
    acknowledged_at TIMESTAMP WITH TIME ZONE,
    resolved_at TIMESTAMP WITH TIME ZONE,
    is_escalated BOOLEAN DEFAULT FALSE,
    escalated_at TIMESTAMP WITH TIME ZONE,
    is_prod_issue BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes for common queries
    CONSTRAINT pk_security_alerts PRIMARY KEY (id)
);

-- Indexes for improved query performance
CREATE INDEX IF NOT EXISTS idx_security_alerts_created_at ON security_alerts(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_security_alerts_severity ON security_alerts(severity);
CREATE INDEX IF NOT EXISTS idx_security_alerts_status ON security_alerts(status);
CREATE INDEX IF NOT EXISTS idx_security_alerts_alert_type ON security_alerts(alert_type);
CREATE INDEX IF NOT EXISTS idx_security_alerts_user_id ON security_alerts(user_id);
CREATE INDEX IF NOT EXISTS idx_security_alerts_ip_address ON security_alerts(ip_address);
CREATE INDEX IF NOT EXISTS idx_security_alerts_affected_resource ON security_alerts(affected_resource_id, affected_resource_type);

-- Create security_alert_audit_log table for compliance/audit trail
CREATE TABLE IF NOT EXISTS security_alert_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    alert_id UUID NOT NULL REFERENCES security_alerts(id) ON DELETE CASCADE,
    action VARCHAR(100) NOT NULL, -- acknowledged, resolved, escalated, etc.
    admin_id UUID NOT NULL,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_security_alert_audit_log_alert_id ON security_alert_audit_log(alert_id);
CREATE INDEX IF NOT EXISTS idx_security_alert_audit_log_created_at ON security_alert_audit_log(created_at DESC);

-- Create blocked_sources table for rate limiting/brute force protection
CREATE TABLE IF NOT EXISTS blocked_sources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_identifier VARCHAR(255) NOT NULL UNIQUE,
    reason VARCHAR(255),
    blocked_until TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_blocked_sources_source_identifier ON blocked_sources(source_identifier);
CREATE INDEX IF NOT EXISTS idx_blocked_sources_blocked_until ON blocked_sources(blocked_until);

COMMIT;
