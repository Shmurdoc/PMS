using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAFARIstack.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_interactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    query = table.Column<string>(type: "text", nullable: false),
                    response = table.Column<string>(type: "text", nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    intent_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    was_approved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    was_edited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    edited_response = table.Column<string>(type: "text", nullable: true),
                    reviewed_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_satisfaction = table.Column<int>(type: "integer", nullable: true),
                    processing_time_ms = table.Column<int>(type: "integer", nullable: false),
                    tokens_used = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cost = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false, defaultValue: 0m),
                    model_used = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_interactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_interactions_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ai_interactions_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "digital_check_ins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    token_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Invited"),
                    id_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    id_document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    id_document_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    id_verification_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    id_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    signature_data = table.Column<string>(type: "text", nullable: true),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    signed_from_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    consent_version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    popia_consent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    marketing_consent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    selected_room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    room_upgrade_selected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    upgrade_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    mobile_key_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mobile_key_valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    mobile_key_valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    mobile_key_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NotProvisioned"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_check_ins", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_check_ins_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_digital_check_ins_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experiences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    min_guests = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    max_guests = table.Column<int>(type: "integer", nullable: false),
                    min_age = table.Column<int>(type: "integer", nullable: true),
                    base_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    price_per_person = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    difficulty_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Easy"),
                    included_items = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    excluded_items = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    what_to_bring = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cancellation_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_third_party = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    third_party_operator = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gift_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    pin_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    initial_balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    current_balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "ZAR"),
                    recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sender_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sender_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    scheduled_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    design_template = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    personal_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_multi_property = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gift_cards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guest_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    primary_channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assigned_to_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    message_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_message_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "property_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    headquarters_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    primary_contact_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    billing_cycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "monthly"),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rate_copy_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_property_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    rate_plan_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false),
                    season_ids = table.Column<List<Guid>>(type: "jsonb", nullable: true),
                    rate_adjustment_pct = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false, defaultValue: 0m),
                    override_existing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    executed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    total_rates_copied = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rate_copy_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "upsell_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    original_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    offer_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    cost_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    inventory_total = table.Column<int>(type: "integer", nullable: true),
                    inventory_remaining = table.Column<int>(type: "integer", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    min_nights = table.Column<int>(type: "integer", nullable: true),
                    min_loyalty_tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    guest_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    booking_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    applicable_days = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    max_days_before_arrival = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upsell_offers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "experience_bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    scheduled_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    participant_count = table.Column<int>(type: "integer", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    commission_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Confirmed"),
                    special_requests = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    assigned_guide_id = table.Column<Guid>(type: "uuid", nullable: true),
                    check_in_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    feedback_score = table.Column<int>(type: "integer", nullable: true),
                    feedback_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    folio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    added_to_folio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_experience_bookings_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_experience_bookings_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_experience_bookings_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experience_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    days_of_week = table.Column<int[]>(type: "jsonb", nullable: false),
                    max_capacity = table.Column<int>(type: "integer", nullable: false),
                    guide_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_experience_schedules_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gift_card_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gift_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    folio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    remaining_balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    receipt_sent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gift_card_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_gift_card_redemptions_gift_cards_gift_card_id",
                        column: x => x.gift_card_id,
                        principalTable: "gift_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guest_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    sender_address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    sender_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Received"),
                    priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Normal"),
                    detected_intent = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    sentiment_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ai_suggested_reply = table.Column<string>(type: "text", nullable: true),
                    ai_confidence_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    ai_reply_approved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ai_reply_edited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    final_reply = table.Column<string>(type: "text", nullable: true),
                    replied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replied_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_guest_messages_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_guest_messages_guest_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "guest_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_guest_messages_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "group_inventory_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    allocated_rooms = table.Column<int>(type: "integer", nullable: false),
                    sell_limit_per_property = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_inventory_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_inventory_allocations_property_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "property_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_inventory_allocations_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "property_group_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_flagship = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    join_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_group_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_property_group_memberships_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_property_group_memberships_property_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "property_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "upsell_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    added_to_folio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    folio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    redeemed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    redemption_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Purchased"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upsell_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_upsell_transactions_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_upsell_transactions_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_upsell_transactions_upsell_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "upsell_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_interactions_booking_id",
                table: "ai_interactions",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_interactions_guest_id",
                table: "ai_interactions",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_interactions_intent",
                table: "ai_interactions",
                column: "intent_category");

            migrationBuilder.CreateIndex(
                name: "ix_ai_interactions_outcome",
                table: "ai_interactions",
                column: "outcome");

            migrationBuilder.CreateIndex(
                name: "ix_ai_interactions_property_date",
                table: "ai_interactions",
                columns: new[] { "property_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_interactions_property_id",
                table: "ai_interactions",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_checkins_booking_id",
                table: "digital_check_ins",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_checkins_guest_id",
                table: "digital_check_ins",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_digital_checkins_property_status",
                table: "digital_check_ins",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_digital_checkins_token",
                table: "digital_check_ins",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exp_bookings_date",
                table: "experience_bookings",
                columns: new[] { "experience_id", "scheduled_date" });

            migrationBuilder.CreateIndex(
                name: "ix_exp_bookings_experience_id",
                table: "experience_bookings",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "ix_exp_bookings_guest_id",
                table: "experience_bookings",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_exp_bookings_property_id",
                table: "experience_bookings",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_exp_bookings_status",
                table: "experience_bookings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_booking_id",
                table: "experience_bookings",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_exp_schedules_experience_id",
                table: "experience_schedules",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "ix_experiences_category",
                table: "experiences",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_experiences_property_active",
                table: "experiences",
                columns: new[] { "property_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_experiences_property_id",
                table: "experiences",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_gc_redemptions_card_id",
                table: "gift_card_redemptions",
                column: "gift_card_id");

            migrationBuilder.CreateIndex(
                name: "ix_gc_redemptions_property_id",
                table: "gift_card_redemptions",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_card_number",
                table: "gift_cards",
                column: "card_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_property_id",
                table: "gift_cards",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_recipient_email",
                table: "gift_cards",
                column: "recipient_email");

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_status",
                table: "gift_cards",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_group_inv_alloc_composite",
                table: "group_inventory_allocations",
                columns: new[] { "group_id", "room_type_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_group_inv_alloc_group_id",
                table: "group_inventory_allocations",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_inventory_allocations_room_type_id",
                table: "group_inventory_allocations",
                column: "room_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_conv_guest_id",
                table: "guest_conversations",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_conv_property_id",
                table: "guest_conversations",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_conv_property_status",
                table: "guest_conversations",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_guest_messages_booking_id",
                table: "guest_messages",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_messages_channel",
                table: "guest_messages",
                column: "channel");

            migrationBuilder.CreateIndex(
                name: "ix_guest_messages_conversation_id",
                table: "guest_messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_messages_guest_id",
                table: "guest_messages",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_messages_property_id",
                table: "guest_messages",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_messages_property_status",
                table: "guest_messages",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pg_memberships_group_property",
                table: "property_group_memberships",
                columns: new[] { "group_id", "property_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pg_memberships_property_id",
                table: "property_group_memberships",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_property_groups_name",
                table: "property_groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rate_copy_jobs_source",
                table: "rate_copy_jobs",
                column: "source_property_id");

            migrationBuilder.CreateIndex(
                name: "ix_rate_copy_jobs_status",
                table: "rate_copy_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_upsell_offers_property_active",
                table: "upsell_offers",
                columns: new[] { "property_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_upsell_offers_property_id",
                table: "upsell_offers",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_upsell_offers_type",
                table: "upsell_offers",
                column: "offer_type");

            migrationBuilder.CreateIndex(
                name: "ix_upsell_tx_booking_id",
                table: "upsell_transactions",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_upsell_tx_guest_id",
                table: "upsell_transactions",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_upsell_tx_offer_id",
                table: "upsell_transactions",
                column: "offer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_interactions");

            migrationBuilder.DropTable(
                name: "digital_check_ins");

            migrationBuilder.DropTable(
                name: "experience_bookings");

            migrationBuilder.DropTable(
                name: "experience_schedules");

            migrationBuilder.DropTable(
                name: "gift_card_redemptions");

            migrationBuilder.DropTable(
                name: "group_inventory_allocations");

            migrationBuilder.DropTable(
                name: "guest_messages");

            migrationBuilder.DropTable(
                name: "property_group_memberships");

            migrationBuilder.DropTable(
                name: "rate_copy_jobs");

            migrationBuilder.DropTable(
                name: "upsell_transactions");

            migrationBuilder.DropTable(
                name: "experiences");

            migrationBuilder.DropTable(
                name: "gift_cards");

            migrationBuilder.DropTable(
                name: "guest_conversations");

            migrationBuilder.DropTable(
                name: "property_groups");

            migrationBuilder.DropTable(
                name: "upsell_offers");
        }
    }
}
