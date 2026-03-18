using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAFARIstack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsNotificationsAndMerchantConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "user_roles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "StaffMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StaffMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "StaffMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StaffMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "StaffMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "StaffMembers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "staff_attendance",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "staff_attendance",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "staff_attendance",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "staff_attendance",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "staff_attendance",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "staff_attendance",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "seasons",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "rooms",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "room_types",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "room_type_amenities",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "room_blocks",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "roles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "role_permissions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "RfidCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RfidCards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "RfidCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RfidCards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "RfidCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PropertyId",
                table: "RfidCards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "RfidCards",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "rfid_readers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "rfid_readers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "rfid_readers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "rfid_readers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "rfid_readers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "rfid_readers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "rates",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "rate_plans",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "properties",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "permissions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "payments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "notifications",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "invoices",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "housekeeping_tasks",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "guests",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "guest_preferences",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "guest_loyalty",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "folios",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "folio_line_items",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "cancellation_policies",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "bookings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "booking_rooms",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "audit_logs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "application_users",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "amenities",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body_html_template = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "merchant_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    merchant_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    api_secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pass_phrase = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    webhook_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    webhook_secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    return_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancel_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notify_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_live = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    additional_config_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "property_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    check_out_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    tourism_levy_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    max_advance_booking_days = table.Column<int>(type: "integer", nullable: false),
                    default_cancellation_hours = table.Column<int>(type: "integer", nullable: false),
                    late_cancellation_penalty_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    no_show_penalty_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_port = table.Column<int>(type: "integer", nullable: false),
                    smtp_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    smtp_use_ssl = table.Column<bool>(type: "boolean", nullable: false),
                    sender_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sender_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reply_to_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    send_booking_confirmation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    send_booking_cancellation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    send_check_in_reminder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    send_check_out_reminder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    send_payment_receipt = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    send_invoice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    send_review_request = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    check_in_reminder_hours_before = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    check_out_reminder_hours_before = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    brand_primary_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    email_footer_html = table.Column<string>(type: "text", nullable: true),
                    invoice_terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    booking_terms_and_conditions = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_property_settings_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_property_id",
                table: "email_templates",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_property_id_notification_type_is_active",
                table: "email_templates",
                columns: new[] { "property_id", "notification_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_merchant_configurations_property_id",
                table: "merchant_configurations",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_merchant_configurations_property_id_provider_type_is_active",
                table: "merchant_configurations",
                columns: new[] { "property_id", "provider_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_property_settings_property_id",
                table: "property_settings",
                column: "property_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "merchant_configurations");

            migrationBuilder.DropTable(
                name: "property_settings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "staff_attendance");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "staff_attendance");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "staff_attendance");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "staff_attendance");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "staff_attendance");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "staff_attendance");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "seasons");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "room_type_amenities");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "room_blocks");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "role_permissions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "RfidCards");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RfidCards");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RfidCards");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RfidCards");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "RfidCards");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "RfidCards");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "RfidCards");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "rfid_readers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "rfid_readers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "rfid_readers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "rfid_readers");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "rfid_readers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "rfid_readers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "rates");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "rate_plans");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "housekeeping_tasks");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "guest_preferences");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "guest_loyalty");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "folios");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "folio_line_items");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "cancellation_policies");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "booking_rooms");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "application_users");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "amenities");
        }
    }
}
