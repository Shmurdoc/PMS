-- Migration: Add Add-ons Framework Tables
-- Date: 2026-03-10
-- Purpose: Support for third-party addon installation and management

BEGIN;

-- Main table for installed addons
CREATE TABLE IF NOT EXISTS installed_addons (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    addon_id VARCHAR(255) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    version VARCHAR(50) NOT NULL,
    status INT NOT NULL DEFAULT 2, -- AddOnLifecyclePhase.Installed = 2
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    installed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    config_json JSONB NOT NULL DEFAULT '{}',
    last_error TEXT,
    update_count INT NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_installed_addons_addon_id ON installed_addons(addon_id);
CREATE INDEX IF NOT EXISTS idx_installed_addons_is_enabled ON installed_addons(is_enabled);
CREATE INDEX IF NOT EXISTS idx_installed_addons_status ON installed_addons(status);

-- Addon configuration key-value storage
CREATE TABLE IF NOT EXISTS addon_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    installed_addon_id UUID NOT NULL REFERENCES installed_addons(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT NOT NULL,
    is_encrypted BOOLEAN NOT NULL DEFAULT FALSE,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(installed_addon_id, key)
);

CREATE INDEX IF NOT EXISTS idx_addon_configurations_addon_id ON addon_configurations(installed_addon_id);
CREATE INDEX IF NOT EXISTS idx_addon_configurations_key ON addon_configurations(key);

-- Addon event subscriptions
CREATE TABLE IF NOT EXISTS addon_event_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    installed_addon_id UUID NOT NULL REFERENCES installed_addons(id) ON DELETE CASCADE,
    event_hook_name VARCHAR(100) NOT NULL,
    subscribed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(installed_addon_id, event_hook_name)
);

CREATE INDEX IF NOT EXISTS idx_addon_event_subscriptions_addon_id ON addon_event_subscriptions(installed_addon_id);
CREATE INDEX IF NOT EXISTS idx_addon_event_subscriptions_event_hook ON addon_event_subscriptions(event_hook_name);

-- Addon API routes registry
CREATE TABLE IF NOT EXISTS addon_api_routes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    installed_addon_id UUID NOT NULL REFERENCES installed_addons(id) ON DELETE CASCADE,
    method VARCHAR(10) NOT NULL, -- GET, POST, PUT, DELETE, PATCH
    path VARCHAR(500) NOT NULL,
    description TEXT,
    requires_authentication BOOLEAN NOT NULL DEFAULT TRUE,
    required_roles JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_addon_api_routes_addon_id ON addon_api_routes(installed_addon_id);
CREATE INDEX IF NOT EXISTS idx_addon_api_routes_method_path ON addon_api_routes(method, path);

-- Addon usage statistics
CREATE TABLE IF NOT EXISTS addon_usage_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    installed_addon_id UUID NOT NULL REFERENCES installed_addons(id) ON DELETE CASCADE,
    metric_date DATE NOT NULL,
    api_calls_count INT NOT NULL DEFAULT 0,
    error_count INT NOT NULL DEFAULT 0,
    avg_response_time_ms INT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(installed_addon_id, metric_date)
);

CREATE INDEX IF NOT EXISTS idx_addon_usage_metrics_addon_id ON addon_usage_metrics(installed_addon_id);
CREATE INDEX IF NOT EXISTS idx_addon_usage_metrics_date ON addon_usage_metrics(metric_date DESC);

COMMIT;
