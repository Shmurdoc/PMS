using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SAFARIstack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRbacSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingRooms_RoomTypes_RoomTypeId",
                table: "BookingRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingRooms_Rooms_RoomId",
                table: "BookingRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingRooms_bookings_BookingId",
                table: "BookingRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_RoomTypes_RoomTypeId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_properties_PropertyId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomTypes_properties_PropertyId",
                table: "RoomTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rooms",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_PropertyId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_guests_property_id_email",
                table: "guests");

            migrationBuilder.DropIndex(
                name: "IX_bookings_property_id",
                table: "bookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomTypes",
                table: "RoomTypes");

            migrationBuilder.DropIndex(
                name: "IX_RoomTypes_PropertyId",
                table: "RoomTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingRooms",
                table: "BookingRooms");

            migrationBuilder.DropIndex(
                name: "IX_BookingRooms_BookingId",
                table: "BookingRooms");

            migrationBuilder.RenameTable(
                name: "Rooms",
                newName: "rooms");

            migrationBuilder.RenameTable(
                name: "RoomTypes",
                newName: "room_types");

            migrationBuilder.RenameTable(
                name: "BookingRooms",
                newName: "booking_rooms");

            migrationBuilder.RenameColumn(
                name: "Wing",
                table: "rooms",
                newName: "wing");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "rooms",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "rooms",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Floor",
                table: "rooms",
                newName: "floor");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "rooms",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "rooms",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RoomTypeId",
                table: "rooms",
                newName: "room_type_id");

            migrationBuilder.RenameColumn(
                name: "RoomNumber",
                table: "rooms",
                newName: "room_number");

            migrationBuilder.RenameColumn(
                name: "PropertyId",
                table: "rooms",
                newName: "property_id");

            migrationBuilder.RenameColumn(
                name: "NextMaintenanceDate",
                table: "rooms",
                newName: "next_maintenance_date");

            migrationBuilder.RenameColumn(
                name: "LastCleanedAt",
                table: "rooms",
                newName: "last_cleaned_at");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "rooms",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "rooms",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_RoomTypeId",
                table: "rooms",
                newName: "IX_rooms_room_type_id");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "guests",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Nationality",
                table: "guests",
                newName: "nationality");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "guests",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "guests",
                newName: "city");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "guests",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "bookings",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "SpecialRequests",
                table: "bookings",
                newName: "special_requests");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "bookings",
                newName: "created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "CheckedOutByUserId",
                table: "bookings",
                newName: "checked_out_by_user_id");

            migrationBuilder.RenameColumn(
                name: "CheckedInByUserId",
                table: "bookings",
                newName: "checked_in_by_user_id");

            migrationBuilder.RenameColumn(
                name: "CancelledByUserId",
                table: "bookings",
                newName: "cancelled_by_user_id");

            migrationBuilder.RenameColumn(
                name: "CancellationReason",
                table: "bookings",
                newName: "cancellation_reason");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "room_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "room_types",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "room_types",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "room_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "room_types",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "SizeInSquareMeters",
                table: "room_types",
                newName: "size_sqm");

            migrationBuilder.RenameColumn(
                name: "RoomCount",
                table: "room_types",
                newName: "room_count");

            migrationBuilder.RenameColumn(
                name: "PropertyId",
                table: "room_types",
                newName: "property_id");

            migrationBuilder.RenameColumn(
                name: "MaxGuests",
                table: "room_types",
                newName: "max_guests");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "room_types",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "room_types",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BedConfiguration",
                table: "room_types",
                newName: "bed_configuration");

            migrationBuilder.RenameColumn(
                name: "BasePrice",
                table: "room_types",
                newName: "base_price");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "booking_rooms",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "booking_rooms",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RoomTypeId",
                table: "booking_rooms",
                newName: "room_type_id");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "booking_rooms",
                newName: "room_id");

            migrationBuilder.RenameColumn(
                name: "RateApplied",
                table: "booking_rooms",
                newName: "rate_applied");

            migrationBuilder.RenameColumn(
                name: "GuestNames",
                table: "booking_rooms",
                newName: "guest_names");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "booking_rooms",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BookingId",
                table: "booking_rooms",
                newName: "booking_id");

            migrationBuilder.RenameIndex(
                name: "IX_BookingRooms_RoomTypeId",
                table: "booking_rooms",
                newName: "IX_booking_rooms_room_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_BookingRooms_RoomId",
                table: "booking_rooms",
                newName: "IX_booking_rooms_room_id");

            migrationBuilder.AlterColumn<string>(
                name: "wing",
                table: "rooms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "rooms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Available",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "rooms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "room_number",
                table: "rooms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "rooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hk_status",
                table: "rooms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Clean");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_user_id",
                table: "rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "blacklist_reason",
                table: "guests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "guests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nationality",
                table: "guests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country",
                table: "guests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "guests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "guests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                table: "guests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_vat_number",
                table: "guests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "guests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "guests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "guests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guest_type",
                table: "guests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "guests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_user_id",
                table: "guests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "guests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                table: "guests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Confirmed",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Confirmed");

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "bookings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Direct",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Direct");

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "bookings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "special_requests",
                table: "bookings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cancellation_reason",
                table: "bookings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "actual_check_in_time",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "actual_check_out_time",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_reference",
                table: "bookings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_user_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rate_plan_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "room_types",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "room_types",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "room_types",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "room_types",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "bed_configuration",
                table: "room_types",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "base_price",
                table: "room_types",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "room_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "room_types",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "room_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "room_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_user_id",
                table: "room_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_adults",
                table: "room_types",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "max_children",
                table: "room_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "room_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "view_type",
                table: "room_types",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "rate_applied",
                table: "booking_rooms",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "guest_names",
                table: "booking_rooms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "booking_rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "booking_rooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "booking_rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "booking_rooms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_user_id",
                table: "booking_rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rate_plan_id",
                table: "booking_rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_rooms",
                table: "rooms",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_room_types",
                table: "room_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_booking_rooms",
                table: "booking_rooms",
                column: "id");

            migrationBuilder.CreateTable(
                name: "amenities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amenities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "application_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefreshToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_application_users_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cancellation_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    free_cancellation_hours = table.Column<int>(type: "integer", nullable: false),
                    penalty_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    no_show_penalty_pct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cancellation_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "folios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    folio_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    total_charges = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    total_payments = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    total_refunds = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folios", x => x.id);
                    table.ForeignKey(
                        name: "FK_folios_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_folios_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_loyalty",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "None"),
                    total_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    available_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_stays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_nights = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_spend = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    last_stay_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tier_expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_loyalty", x => x.id);
                    table.ForeignKey(
                        name: "FK_guest_loyalty_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guest_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_preferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_guest_preferences_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "housekeeping_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    task_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Normal"),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false, defaultValue: "Pending"),
                    scheduled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    inspection_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    inspected_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    passed_inspection = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    linen_changed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    bathroom_cleaned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    floors_cleaned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    minibar_restocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    amenities_replenished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_housekeeping_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_housekeeping_tasks_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_guest_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    channel = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "Queued"),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    external_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "room_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_room_blocks_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    price_multiplier = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1.0m),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons", x => x.id);
                    table.ForeignKey(
                        name: "FK_seasons_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_type_amenities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amenity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_type_amenities", x => x.id);
                    table.ForeignKey(
                        name: "FK_room_type_amenities_amenities_amenity_id",
                        column: x => x.amenity_id,
                        principalTable: "amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_type_amenities_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rate_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    includes_breakfast = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_refundable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    min_nights = table.Column<int>(type: "integer", nullable: true),
                    max_nights = table.Column<int>(type: "integer", nullable: true),
                    min_advance_days = table.Column<int>(type: "integer", nullable: true),
                    max_advance_days = table.Column<int>(type: "integer", nullable: true),
                    cancellation_policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rate_plans", x => x.id);
                    table.ForeignKey(
                        name: "FK_rate_plans_cancellation_policies_cancellation_policy_id",
                        column: x => x.cancellation_policy_id,
                        principalTable: "cancellation_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_rate_plans_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "folio_line_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    folio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    service_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    charge_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folio_line_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_folio_line_items_folios_folio_id",
                        column: x => x.folio_id,
                        principalTable: "folios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    folio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    invoice_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tourism_levy_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    vat_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    company_vat_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoices_folios_folio_id",
                        column: x => x.folio_id,
                        principalTable: "folios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_guests_guest_id",
                        column: x => x.guest_id,
                        principalTable: "guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    folio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "ZAR"),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Completed"),
                    transaction_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gateway_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_refund = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    original_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_payments_folios_folio_id",
                        column: x => x.folio_id,
                        principalTable: "folios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_roles_application_users_UserId",
                        column: x => x.UserId,
                        principalTable: "application_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_per_night = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    single_occupancy_rate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    extra_adult_rate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    extra_child_rate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "ZAR"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rates", x => x.id);
                    table.ForeignKey(
                        name: "FK_rates_rate_plans_rate_plan_id",
                        column: x => x.rate_plan_id,
                        principalTable: "rate_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rates_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rates_seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "CreatedAt", "Description", "Module", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("022ddd44-f4ca-1345-bfaf-0383604363ff"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to financial manage", "Financial", "financial.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("1dbccf16-45bf-0e49-8766-c1aeed78e40f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to roles manage", "Roles", "roles.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("249551e3-00c9-444f-a552-aa7011a93871"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to guests create", "Guests", "guests.create", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30a6caa4-fd10-e44a-8d9a-c835f1375107"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to rooms view", "Rooms", "rooms.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3282ed35-f0b7-da4d-ad35-3c1965eaa798"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to guests edit", "Guests", "guests.edit", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("478dda5b-4c7e-7242-9a35-2ef30cc4f035"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to housekeeping view", "Housekeeping", "housekeeping.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("5a271463-a698-ff4a-8870-950426d957b7"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to bookings view", "Bookings", "bookings.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("5cf4a458-1e91-7b46-8b84-f975f3e0fd4f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to rates view", "Rates", "rates.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("5f2e9e1e-47df-8845-93b4-d436eb1f7c42"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to bookings cancel", "Bookings", "bookings.cancel", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("636809eb-0c22-cc46-8056-15f5d8a32d24"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to reports view", "Reports", "reports.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("7493031a-aa68-cb45-9f65-99ebadf501a1"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to staff manage", "Staff", "staff.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("7d56e943-e7be-c041-a5a4-8062d33fc63f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to bookings edit", "Bookings", "bookings.edit", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("84829ce4-8dc7-334d-a934-7cee12fb223e"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to financial view", "Financial", "financial.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8603dadf-03d2-174a-8bf8-c6a0e9bcf655"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to housekeeping inspect", "Housekeeping", "housekeeping.inspect", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("87aed298-c494-4d4d-9fc6-40368f50a26c"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to bookings checkin", "Bookings", "bookings.checkin", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("89faf1ec-f9ba-2741-97d7-ef7c51a29428"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to guests view", "Guests", "guests.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("918bb3cd-7191-2648-9e4b-a99c0368ea55"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to rooms manage", "Rooms", "rooms.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("92eb5dcb-b840-8545-945a-4dd8fa00becf"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to bookings create", "Bookings", "bookings.create", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a9a1aad4-ac81-9c40-968f-0add8663e1d8"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to invoices manage", "Invoices", "invoices.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ada3b145-24f7-b74e-a699-8934f58ad09f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to analytics view", "Analytics", "analytics.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b39eccb1-56c8-7c44-a6a3-deac70042d2c"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to users manage", "Users", "users.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b5ab61c0-372b-3e4c-badd-628fcea41431"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to rates manage", "Rates", "rates.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d0f732c9-7af5-2144-aed3-e54acb15e439"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to housekeeping manage", "Housekeeping", "housekeeping.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("daca611c-fbdf-9440-947b-12ff40fd71d1"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to rfid manage", "Rfid", "rfid.manage", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("edfe9554-7f03-3944-a0fa-0b4a56f39d9d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to guests blacklist", "Guests", "guests.blacklist", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ee7e3a98-447b-b54c-99e5-89dfc83366fe"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to property settings", "Property", "property.settings", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("efafc8aa-1284-1e4d-bc3a-4007037ab0a3"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to staff view", "Staff", "staff.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fa6a7083-552d-3e4d-b89c-3b0ae7d2ab5f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to auditlogs view", "Auditlogs", "auditlogs.view", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fb0ca8aa-6f5f-794b-a3d5-53c17d28610d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permission to bookings checkout", "Bookings", "bookings.checkout", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Description", "IsDeleted", "IsSystemRole", "LastModifiedByUserId", "Name", "NormalizedName", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0603b74a-979a-2747-9d1d-ed5ec8f5a2ce"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Housekeeping operations — task management, inspections", false, true, null, "Housekeeping", "HOUSEKEEPING", 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Operations management — duty manager", false, true, null, "Manager", "MANAGER", 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Full property access — establishment owner/GM", false, true, null, "PropertyAdmin", "PROPERTYADMIN", 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("385ef2ca-7d91-9d45-9b69-87bfcd817ab0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Financial operations — folios, invoices, payments, reports", false, true, null, "Finance", "FINANCE", 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Front desk operations — bookings, check-in/out, guests", false, true, null, "FrontDesk", "FRONTDESK", 3, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Full system access — platform owner/operator", false, true, null, "SuperAdmin", "SUPERADMIN", 0, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f5e255f8-86e6-6b41-bf22-685e8fe17231"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Maintenance tasks — room issues, repairs", false, true, null, "Maintenance", "MAINTENANCE", 6, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "Id", "CreatedAt", "PermissionId", "RoleId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("02051cf7-2116-d343-b45c-14e8a04871ba"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8603dadf-03d2-174a-8bf8-c6a0e9bcf655"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("05d79b5d-36ca-1649-aa57-503a7de9e377"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("022ddd44-f4ca-1345-bfaf-0383604363ff"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0ba0a580-2c50-9b4f-8b9f-da1d7c4987d7"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("918bb3cd-7191-2648-9e4b-a99c0368ea55"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0e8c6f50-3c7f-8e4e-8d89-c776a8254ae7"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("918bb3cd-7191-2648-9e4b-a99c0368ea55"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("12b19dce-30f9-0749-9e8a-2b89929577f0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5a271463-a698-ff4a-8870-950426d957b7"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("13f14a28-8a1f-604e-94e2-0e8a26ab7958"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("efafc8aa-1284-1e4d-bc3a-4007037ab0a3"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("1498eb57-c395-bc4f-abd2-79423f384cc4"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b5ab61c0-372b-3e4c-badd-628fcea41431"), new Guid("385ef2ca-7d91-9d45-9b69-87bfcd817ab0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("19752b97-2af8-9648-a4af-75858321e3e3"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("87aed298-c494-4d4d-9fc6-40368f50a26c"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("26536d26-26a8-4c49-bbc5-5a1bcc228f07"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5cf4a458-1e91-7b46-8b84-f975f3e0fd4f"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("28d3ca49-5189-3a47-b3b1-894d9bdd263c"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("636809eb-0c22-cc46-8056-15f5d8a32d24"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("28d5de0e-c0c4-b74a-8b69-48ef32e2b633"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d0f732c9-7af5-2144-aed3-e54acb15e439"), new Guid("0603b74a-979a-2747-9d1d-ed5ec8f5a2ce"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2bf14cc4-dae3-2048-97fa-43afd0910938"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("84829ce4-8dc7-334d-a934-7cee12fb223e"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("31a74d0c-7175-0e4f-8fc1-45e6f7ac03b6"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5f2e9e1e-47df-8845-93b4-d436eb1f7c42"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3322fc48-2ac9-b748-b2ab-a633b7dd08db"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5cf4a458-1e91-7b46-8b84-f975f3e0fd4f"), new Guid("385ef2ca-7d91-9d45-9b69-87bfcd817ab0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("351855f8-d5d2-1d4b-8c66-d74409cb1128"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7d56e943-e7be-c041-a5a4-8062d33fc63f"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3716a610-789f-b44e-858c-0a0481c61a4d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("84829ce4-8dc7-334d-a934-7cee12fb223e"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3899edd1-5f91-b94b-b059-66f52b0a8926"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("edfe9554-7f03-3944-a0fa-0b4a56f39d9d"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3b2d0238-2c13-2747-934c-25b2c5e2c127"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7493031a-aa68-cb45-9f65-99ebadf501a1"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3d337733-1c50-3143-9146-a2077e6341cf"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("249551e3-00c9-444f-a552-aa7011a93871"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3db5bf31-6b80-7643-b56c-3e259b13c940"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("ada3b145-24f7-b74e-a699-8934f58ad09f"), new Guid("385ef2ca-7d91-9d45-9b69-87bfcd817ab0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3e09e491-43b5-b347-8feb-bfbb1a6ce9fc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("ada3b145-24f7-b74e-a699-8934f58ad09f"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("43016807-572b-ed4a-955f-b912b41933f2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("918bb3cd-7191-2648-9e4b-a99c0368ea55"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("436f23f2-e9f5-e94f-929d-199b1f254ae3"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("daca611c-fbdf-9440-947b-12ff40fd71d1"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("43c2f065-cdb8-eb47-9426-fccd4d703f0a"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("ada3b145-24f7-b74e-a699-8934f58ad09f"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4687f13a-0a31-b240-9af1-b1b9003a9199"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5f2e9e1e-47df-8845-93b4-d436eb1f7c42"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("47213d39-3897-1f47-b2f7-379e1d805c45"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("1dbccf16-45bf-0e49-8766-c1aeed78e40f"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4be0e0fc-b3c7-aa47-bb81-b4a7a46a3a29"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8603dadf-03d2-174a-8bf8-c6a0e9bcf655"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("52873b09-ecf5-af49-b7d2-641f4b7272b5"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("478dda5b-4c7e-7242-9a35-2ef30cc4f035"), new Guid("0603b74a-979a-2747-9d1d-ed5ec8f5a2ce"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("560c2b67-0d1a-7a46-9454-83b62ab6ef59"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("022ddd44-f4ca-1345-bfaf-0383604363ff"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("58d28a4a-2faf-1642-8478-5f4b8e504116"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30a6caa4-fd10-e44a-8d9a-c835f1375107"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("59527674-1a47-2e4a-a781-0175eb83e64a"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a9a1aad4-ac81-9c40-968f-0add8663e1d8"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("5bb2a120-81a3-f843-beec-882df75ac6dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("92eb5dcb-b840-8545-945a-4dd8fa00becf"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("5e188424-dedf-ae43-bbfb-224218f193cd"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("87aed298-c494-4d4d-9fc6-40368f50a26c"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("5ebf0c36-e2f8-e144-9139-770f6296c65c"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fa6a7083-552d-3e4d-b89c-3b0ae7d2ab5f"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("5fd6b573-6ff7-9f49-aad4-68c9db49bdfd"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7d56e943-e7be-c041-a5a4-8062d33fc63f"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("60e5aece-815e-414a-a1e0-e0cd41779d89"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("edfe9554-7f03-3944-a0fa-0b4a56f39d9d"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("60f9bd56-49ea-b242-a514-2967c140992c"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30a6caa4-fd10-e44a-8d9a-c835f1375107"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("61d82e27-cecd-8c4f-8304-18eaa3e2262f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("84829ce4-8dc7-334d-a934-7cee12fb223e"), new Guid("385ef2ca-7d91-9d45-9b69-87bfcd817ab0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("623bb1de-0471-bd43-9144-b7801aa823f0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("478dda5b-4c7e-7242-9a35-2ef30cc4f035"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("68c30e57-c578-7448-b4e5-020b65856219"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("89faf1ec-f9ba-2741-97d7-ef7c51a29428"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("6cd55fd8-0729-974a-9a46-c4339ae45943"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d0f732c9-7af5-2144-aed3-e54acb15e439"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("6f97469a-2918-5d42-9827-c5d62cc4ed98"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fb0ca8aa-6f5f-794b-a3d5-53c17d28610d"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("701fff59-984d-984c-b366-a6e4a6d3ceb1"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("efafc8aa-1284-1e4d-bc3a-4007037ab0a3"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("72e216c7-7186-ca46-86c7-b57648d99b57"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("89faf1ec-f9ba-2741-97d7-ef7c51a29428"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("72fa8214-5090-b040-9608-4119e1f2fdf4"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("daca611c-fbdf-9440-947b-12ff40fd71d1"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("7437b4ea-c831-544d-967c-e4035934439d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("89faf1ec-f9ba-2741-97d7-ef7c51a29428"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("785332cd-d40a-f14c-bd8a-da3081d7de1d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5f2e9e1e-47df-8845-93b4-d436eb1f7c42"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("791ae227-8e0b-b34f-919a-785be2d36970"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("3282ed35-f0b7-da4d-ad35-3c1965eaa798"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("7995e7c0-4485-cf47-b47f-68b2a2dbb6b2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7d56e943-e7be-c041-a5a4-8062d33fc63f"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("81754ac1-9170-fb45-a0da-465fca137631"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d0f732c9-7af5-2144-aed3-e54acb15e439"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("82e08c8f-dedf-a847-a2cb-fb253faf5d2c"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("84829ce4-8dc7-334d-a934-7cee12fb223e"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("83056d50-5c9e-6142-80e2-cf53b6739ed0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("3282ed35-f0b7-da4d-ad35-3c1965eaa798"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("84b2db5b-66d9-994f-ba32-e7f554ac2501"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30a6caa4-fd10-e44a-8d9a-c835f1375107"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("866b0082-c40f-664d-b722-bd8ab803b41e"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("daca611c-fbdf-9440-947b-12ff40fd71d1"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("86903f97-b6c2-974f-ab1c-a3a12a33b376"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("249551e3-00c9-444f-a552-aa7011a93871"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("899f21bc-1efd-ff48-8ad5-acd702558972"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5a271463-a698-ff4a-8870-950426d957b7"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8b74ec69-b64b-3d43-8913-792dc0a8e6c7"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8603dadf-03d2-174a-8bf8-c6a0e9bcf655"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8daec168-ed5d-8f48-9391-ce848a3ce76f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7493031a-aa68-cb45-9f65-99ebadf501a1"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8f566421-7937-5647-ae41-9f44d9cd2077"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("efafc8aa-1284-1e4d-bc3a-4007037ab0a3"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8f6acf0a-4bc9-c84b-bb9f-d36fe1a36b1a"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("92eb5dcb-b840-8545-945a-4dd8fa00becf"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("91ec46f7-23ce-094c-9cba-288e3082ebc9"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("ada3b145-24f7-b74e-a699-8934f58ad09f"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("94f24a07-ad81-de49-a2ec-169b5f00cf9e"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("3282ed35-f0b7-da4d-ad35-3c1965eaa798"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("96b282ef-6dbd-3e48-b4ae-ab33178966af"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b39eccb1-56c8-7c44-a6a3-deac70042d2c"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("96bb91c4-4a5c-084b-abcb-7c81a3fb2dd6"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("249551e3-00c9-444f-a552-aa7011a93871"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("994e855d-fcd2-1541-ac2a-2e8861331c91"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("edfe9554-7f03-3944-a0fa-0b4a56f39d9d"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("9b266e17-1ca0-5949-be29-a2ab0a5d7f2b"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a9a1aad4-ac81-9c40-968f-0add8663e1d8"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a2dcd698-b5f7-fa4a-91cc-70d8e6d48a3b"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30a6caa4-fd10-e44a-8d9a-c835f1375107"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a31e19f8-4b70-174a-bde6-a662dfe6f005"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("636809eb-0c22-cc46-8056-15f5d8a32d24"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a6c96f49-7048-d14f-a641-c0679a3ff7e6"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("87aed298-c494-4d4d-9fc6-40368f50a26c"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a72cd701-b5fe-fc49-a7ba-b9e66b983bde"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fa6a7083-552d-3e4d-b89c-3b0ae7d2ab5f"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a81543ef-173d-0145-89e9-712664633d13"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7d56e943-e7be-c041-a5a4-8062d33fc63f"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a99b5928-720c-3a46-a7b9-0648517f92db"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("3282ed35-f0b7-da4d-ad35-3c1965eaa798"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ac4f9b55-545f-6344-a71d-6a5ddda67378"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("636809eb-0c22-cc46-8056-15f5d8a32d24"), new Guid("385ef2ca-7d91-9d45-9b69-87bfcd817ab0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ac9d52fa-f1cd-5a44-a61e-49022f3b9019"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30a6caa4-fd10-e44a-8d9a-c835f1375107"), new Guid("0603b74a-979a-2747-9d1d-ed5ec8f5a2ce"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("acff9f7d-4d95-874d-86b6-b646b7da4d11"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5cf4a458-1e91-7b46-8b84-f975f3e0fd4f"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ad35f7c5-b15a-234f-a448-87c07d964113"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b5ab61c0-372b-3e4c-badd-628fcea41431"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("add82dfd-124a-424d-a902-2d004cc53ac7"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5cf4a458-1e91-7b46-8b84-f975f3e0fd4f"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0d85473-cb63-c045-ba57-2dde5568361d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("92eb5dcb-b840-8545-945a-4dd8fa00becf"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b3d98994-be6c-ff4f-95e5-5953ad1c8ae3"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30a6caa4-fd10-e44a-8d9a-c835f1375107"), new Guid("f5e255f8-86e6-6b41-bf22-685e8fe17231"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b9d7b848-dbbf-f14a-97ec-5b74ef21aaf5"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a9a1aad4-ac81-9c40-968f-0add8663e1d8"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c05a6c9e-b96c-8b41-b17c-5b00c10dc873"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5a271463-a698-ff4a-8870-950426d957b7"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c14dab6f-3d67-e345-8453-48d9e2b5efb4"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5cf4a458-1e91-7b46-8b84-f975f3e0fd4f"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c2064c0e-f399-4847-ae55-ad2e4391c994"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("249551e3-00c9-444f-a552-aa7011a93871"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c2b0b476-a1b7-5245-a76d-be622eccaca5"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("ee7e3a98-447b-b54c-99e5-89dfc83366fe"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c3806863-1fec-aa49-8f92-6f1cd37f984a"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("92eb5dcb-b840-8545-945a-4dd8fa00becf"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c6f1fea4-3d4f-8643-ad9c-31bcca439041"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b39eccb1-56c8-7c44-a6a3-deac70042d2c"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c7958609-3fc2-5a42-a687-23a854ecdb8d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d0f732c9-7af5-2144-aed3-e54acb15e439"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c803bc74-94be-fa45-9ef2-153b1294a9cc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b5ab61c0-372b-3e4c-badd-628fcea41431"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c863b431-7316-b542-a02e-da464ebb512a"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b39eccb1-56c8-7c44-a6a3-deac70042d2c"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("c9593a32-1a70-314c-bd42-2cec89d6a26c"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("1dbccf16-45bf-0e49-8766-c1aeed78e40f"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("caf63c63-5fd3-474d-8b49-bd5159d0e1db"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("89faf1ec-f9ba-2741-97d7-ef7c51a29428"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("cb0f7b88-90c0-d044-88ef-73b9e9fb150e"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("8603dadf-03d2-174a-8bf8-c6a0e9bcf655"), new Guid("0603b74a-979a-2747-9d1d-ed5ec8f5a2ce"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("cbb1aacb-276a-6b41-ba3e-6eafdc9238dd"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fb0ca8aa-6f5f-794b-a3d5-53c17d28610d"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("cd6f8530-5dd1-3849-a519-152f71945995"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fb0ca8aa-6f5f-794b-a3d5-53c17d28610d"), new Guid("548327c0-5c00-3841-84a3-e04f2f7c9ed2"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d1e27d4d-871b-3444-8703-4483e7376068"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("7493031a-aa68-cb45-9f65-99ebadf501a1"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("db08a1b8-5149-ae48-98e7-c887e391869e"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("022ddd44-f4ca-1345-bfaf-0383604363ff"), new Guid("385ef2ca-7d91-9d45-9b69-87bfcd817ab0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e562af64-6a76-dc45-9eda-5fa8c8b5454d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("478dda5b-4c7e-7242-9a35-2ef30cc4f035"), new Guid("f5e255f8-86e6-6b41-bf22-685e8fe17231"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e57956fb-f58b-294a-b677-77e17203889f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a9a1aad4-ac81-9c40-968f-0add8663e1d8"), new Guid("385ef2ca-7d91-9d45-9b69-87bfcd817ab0"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e57f55f1-03e8-f445-957d-90cfbcdbcf0a"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b5ab61c0-372b-3e4c-badd-628fcea41431"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e61ff1f7-4636-2f47-bfc4-df4ff8c3f1ea"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("87aed298-c494-4d4d-9fc6-40368f50a26c"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e66e31b6-12b7-804f-b0c4-fbf9e242acbc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fa6a7083-552d-3e4d-b89c-3b0ae7d2ab5f"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e78d4b98-be60-7948-8f78-2834cb625058"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("ee7e3a98-447b-b54c-99e5-89dfc83366fe"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ecbf6708-bc04-e842-9b8c-8973678cd2cf"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("636809eb-0c22-cc46-8056-15f5d8a32d24"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("ed2d5ecc-4461-194f-a162-7357b447f7ec"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("478dda5b-4c7e-7242-9a35-2ef30cc4f035"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f07ef65b-b00f-a84f-94ac-b789955a3255"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("478dda5b-4c7e-7242-9a35-2ef30cc4f035"), new Guid("2a764379-898b-b24d-ad53-feea2420f0dc"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f3011801-d0a7-584d-9b8d-824ab226c4f5"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("84829ce4-8dc7-334d-a934-7cee12fb223e"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f4b7e2b1-1155-7344-9d27-4a1acab4080c"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("022ddd44-f4ca-1345-bfaf-0383604363ff"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f7d97f2e-cef2-724e-8ade-f5416024e006"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("5a271463-a698-ff4a-8870-950426d957b7"), new Guid("1562e4c9-e330-1a4b-9977-1ac1728d8e2f"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fd558476-42bf-f94c-92a2-2d2736866f70"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("fb0ca8aa-6f5f-794b-a3d5-53c17d28610d"), new Guid("ebb21e1f-751b-1e40-a210-c7fd3d83720d"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_rooms_hk_status",
                table: "rooms",
                column: "hk_status");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_property_id_room_number",
                table: "rooms",
                columns: new[] { "property_id", "room_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_status",
                table: "rooms",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_guests_id_number",
                table: "guests",
                column: "id_number");

            migrationBuilder.CreateIndex(
                name: "IX_guests_property_id_email",
                table: "guests",
                columns: new[] { "property_id", "email" },
                unique: true,
                filter: "email IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_check_out_date",
                table: "bookings",
                column: "check_out_date");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_external_reference",
                table: "bookings",
                column: "external_reference");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_property_id_check_in_date_check_out_date",
                table: "bookings",
                columns: new[] { "property_id", "check_in_date", "check_out_date" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_rate_plan_id",
                table: "bookings",
                column: "rate_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_room_types_property_id_code",
                table: "room_types",
                columns: new[] { "property_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_rooms_booking_id_room_id",
                table: "booking_rooms",
                columns: new[] { "booking_id", "room_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_rooms_rate_plan_id",
                table: "booking_rooms",
                column: "rate_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_amenities_property_id_name",
                table: "amenities",
                columns: new[] { "property_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_application_users_NormalizedEmail",
                table: "application_users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_application_users_PropertyId",
                table: "application_users",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_application_users_RefreshToken",
                table: "application_users",
                column: "RefreshToken");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_property_id",
                table: "audit_logs",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cancellation_policies_property_id_name",
                table: "cancellation_policies",
                columns: new[] { "property_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_folio_line_items_charge_date",
                table: "folio_line_items",
                column: "charge_date");

            migrationBuilder.CreateIndex(
                name: "IX_folio_line_items_folio_id",
                table: "folio_line_items",
                column: "folio_id");

            migrationBuilder.CreateIndex(
                name: "IX_folios_booking_id",
                table: "folios",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_folios_folio_number",
                table: "folios",
                column: "folio_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_folios_guest_id",
                table: "folios",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "IX_folios_property_id_status",
                table: "folios",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_guest_loyalty_guest_id",
                table: "guest_loyalty",
                column: "guest_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guest_loyalty_tier",
                table: "guest_loyalty",
                column: "tier");

            migrationBuilder.CreateIndex(
                name: "IX_guest_preferences_guest_id_category_key",
                table: "guest_preferences",
                columns: new[] { "guest_id", "category", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_housekeeping_tasks_assigned_to_staff_id",
                table: "housekeeping_tasks",
                column: "assigned_to_staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_housekeeping_tasks_property_id_scheduled_date_status",
                table: "housekeeping_tasks",
                columns: new[] { "property_id", "scheduled_date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_housekeeping_tasks_room_id",
                table: "housekeeping_tasks",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_due_date",
                table: "invoices",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_folio_id",
                table: "invoices",
                column: "folio_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_guest_id",
                table: "invoices",
                column: "guest_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_property_id_status",
                table: "invoices",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_property_id_status_created_at",
                table: "notifications",
                columns: new[] { "property_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_guest_id",
                table: "notifications",
                column: "recipient_guest_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_status",
                table: "notifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_payments_folio_id",
                table: "payments",
                column: "folio_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_property_id_payment_date",
                table: "payments",
                columns: new[] { "property_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_transaction_reference",
                table: "payments",
                column: "transaction_reference");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Name",
                table: "permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rate_plans_cancellation_policy_id",
                table: "rate_plans",
                column: "cancellation_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_rate_plans_property_id_code",
                table: "rate_plans",
                columns: new[] { "property_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rates_rate_plan_id",
                table: "rates",
                column: "rate_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_rates_room_type_id_rate_plan_id_effective_from_effective_to",
                table: "rates",
                columns: new[] { "room_type_id", "rate_plan_id", "effective_from", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "IX_rates_season_id",
                table: "rates",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionId",
                table: "role_permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_RoleId_PermissionId",
                table: "role_permissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_NormalizedName",
                table: "roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_blocks_room_id_start_date_end_date",
                table: "room_blocks",
                columns: new[] { "room_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_room_type_amenities_amenity_id",
                table: "room_type_amenities",
                column: "amenity_id");

            migrationBuilder.CreateIndex(
                name: "IX_room_type_amenities_room_type_id_amenity_id",
                table: "room_type_amenities",
                columns: new[] { "room_type_id", "amenity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seasons_property_id_code",
                table: "seasons",
                columns: new[] { "property_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seasons_property_id_start_date_end_date",
                table: "seasons",
                columns: new[] { "property_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId_RoleId",
                table: "user_roles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_rooms_bookings_booking_id",
                table: "booking_rooms",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_rooms_rate_plans_rate_plan_id",
                table: "booking_rooms",
                column: "rate_plan_id",
                principalTable: "rate_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_rooms_room_types_room_type_id",
                table: "booking_rooms",
                column: "room_type_id",
                principalTable: "room_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_rooms_rooms_room_id",
                table: "booking_rooms",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_rate_plans_rate_plan_id",
                table: "bookings",
                column: "rate_plan_id",
                principalTable: "rate_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_room_types_properties_property_id",
                table: "room_types",
                column: "property_id",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_properties_property_id",
                table: "rooms",
                column: "property_id",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_room_types_room_type_id",
                table: "rooms",
                column: "room_type_id",
                principalTable: "room_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_booking_rooms_bookings_booking_id",
                table: "booking_rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_booking_rooms_rate_plans_rate_plan_id",
                table: "booking_rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_booking_rooms_room_types_room_type_id",
                table: "booking_rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_booking_rooms_rooms_room_id",
                table: "booking_rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_bookings_rate_plans_rate_plan_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_room_types_properties_property_id",
                table: "room_types");

            migrationBuilder.DropForeignKey(
                name: "FK_rooms_properties_property_id",
                table: "rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_rooms_room_types_room_type_id",
                table: "rooms");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "folio_line_items");

            migrationBuilder.DropTable(
                name: "guest_loyalty");

            migrationBuilder.DropTable(
                name: "guest_preferences");

            migrationBuilder.DropTable(
                name: "housekeeping_tasks");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "rates");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "room_blocks");

            migrationBuilder.DropTable(
                name: "room_type_amenities");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "folios");

            migrationBuilder.DropTable(
                name: "rate_plans");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "amenities");

            migrationBuilder.DropTable(
                name: "application_users");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "cancellation_policies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_rooms",
                table: "rooms");

            migrationBuilder.DropIndex(
                name: "IX_rooms_hk_status",
                table: "rooms");

            migrationBuilder.DropIndex(
                name: "IX_rooms_property_id_room_number",
                table: "rooms");

            migrationBuilder.DropIndex(
                name: "IX_rooms_status",
                table: "rooms");

            migrationBuilder.DropIndex(
                name: "IX_guests_id_number",
                table: "guests");

            migrationBuilder.DropIndex(
                name: "IX_guests_property_id_email",
                table: "guests");

            migrationBuilder.DropIndex(
                name: "IX_bookings_check_out_date",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_external_reference",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_property_id_check_in_date_check_out_date",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_rate_plan_id",
                table: "bookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_room_types",
                table: "room_types");

            migrationBuilder.DropIndex(
                name: "IX_room_types_property_id_code",
                table: "room_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_booking_rooms",
                table: "booking_rooms");

            migrationBuilder.DropIndex(
                name: "IX_booking_rooms_booking_id_room_id",
                table: "booking_rooms");

            migrationBuilder.DropIndex(
                name: "IX_booking_rooms_rate_plan_id",
                table: "booking_rooms");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "hk_status",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "last_modified_by_user_id",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "company_name",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "company_vat_number",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "guest_type",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "last_modified_by_user_id",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "province",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "actual_check_in_time",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "actual_check_out_time",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "external_reference",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "last_modified_by_user_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "rate_plan_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "last_modified_by_user_id",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "max_adults",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "max_children",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "view_type",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "booking_rooms");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "booking_rooms");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "booking_rooms");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "booking_rooms");

            migrationBuilder.DropColumn(
                name: "last_modified_by_user_id",
                table: "booking_rooms");

            migrationBuilder.DropColumn(
                name: "rate_plan_id",
                table: "booking_rooms");

            migrationBuilder.RenameTable(
                name: "rooms",
                newName: "Rooms");

            migrationBuilder.RenameTable(
                name: "room_types",
                newName: "RoomTypes");

            migrationBuilder.RenameTable(
                name: "booking_rooms",
                newName: "BookingRooms");

            migrationBuilder.RenameColumn(
                name: "wing",
                table: "Rooms",
                newName: "Wing");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Rooms",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "Rooms",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "floor",
                table: "Rooms",
                newName: "Floor");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Rooms",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Rooms",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "room_type_id",
                table: "Rooms",
                newName: "RoomTypeId");

            migrationBuilder.RenameColumn(
                name: "room_number",
                table: "Rooms",
                newName: "RoomNumber");

            migrationBuilder.RenameColumn(
                name: "property_id",
                table: "Rooms",
                newName: "PropertyId");

            migrationBuilder.RenameColumn(
                name: "next_maintenance_date",
                table: "Rooms",
                newName: "NextMaintenanceDate");

            migrationBuilder.RenameColumn(
                name: "last_cleaned_at",
                table: "Rooms",
                newName: "LastCleanedAt");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Rooms",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Rooms",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_rooms_room_type_id",
                table: "Rooms",
                newName: "IX_Rooms_RoomTypeId");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "guests",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "nationality",
                table: "guests",
                newName: "Nationality");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "guests",
                newName: "Country");

            migrationBuilder.RenameColumn(
                name: "city",
                table: "guests",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "guests",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "bookings",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "special_requests",
                table: "bookings",
                newName: "SpecialRequests");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "bookings",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "checked_out_by_user_id",
                table: "bookings",
                newName: "CheckedOutByUserId");

            migrationBuilder.RenameColumn(
                name: "checked_in_by_user_id",
                table: "bookings",
                newName: "CheckedInByUserId");

            migrationBuilder.RenameColumn(
                name: "cancelled_by_user_id",
                table: "bookings",
                newName: "CancelledByUserId");

            migrationBuilder.RenameColumn(
                name: "cancellation_reason",
                table: "bookings",
                newName: "CancellationReason");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "RoomTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "RoomTypes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "RoomTypes",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "RoomTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "RoomTypes",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "size_sqm",
                table: "RoomTypes",
                newName: "SizeInSquareMeters");

            migrationBuilder.RenameColumn(
                name: "room_count",
                table: "RoomTypes",
                newName: "RoomCount");

            migrationBuilder.RenameColumn(
                name: "property_id",
                table: "RoomTypes",
                newName: "PropertyId");

            migrationBuilder.RenameColumn(
                name: "max_guests",
                table: "RoomTypes",
                newName: "MaxGuests");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "RoomTypes",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "RoomTypes",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "bed_configuration",
                table: "RoomTypes",
                newName: "BedConfiguration");

            migrationBuilder.RenameColumn(
                name: "base_price",
                table: "RoomTypes",
                newName: "BasePrice");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BookingRooms",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "BookingRooms",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "room_type_id",
                table: "BookingRooms",
                newName: "RoomTypeId");

            migrationBuilder.RenameColumn(
                name: "room_id",
                table: "BookingRooms",
                newName: "RoomId");

            migrationBuilder.RenameColumn(
                name: "rate_applied",
                table: "BookingRooms",
                newName: "RateApplied");

            migrationBuilder.RenameColumn(
                name: "guest_names",
                table: "BookingRooms",
                newName: "GuestNames");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "BookingRooms",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "booking_id",
                table: "BookingRooms",
                newName: "BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_booking_rooms_room_type_id",
                table: "BookingRooms",
                newName: "IX_BookingRooms_RoomTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_booking_rooms_room_id",
                table: "BookingRooms",
                newName: "IX_BookingRooms_RoomId");

            migrationBuilder.AlterColumn<string>(
                name: "Wing",
                table: "Rooms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Rooms",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Available");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Rooms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RoomNumber",
                table: "Rooms",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Rooms",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "guests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nationality",
                table: "guests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "guests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "guests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "blacklist_reason",
                table: "guests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "guests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "Confirmed",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Confirmed");

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "Direct",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Direct");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SpecialRequests",
                table: "bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "RoomTypes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "RoomTypes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "RoomTypes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "RoomTypes",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "BedConfiguration",
                table: "RoomTypes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BasePrice",
                table: "RoomTypes",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "RateApplied",
                table: "BookingRooms",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "GuestNames",
                table: "BookingRooms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rooms",
                table: "Rooms",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomTypes",
                table: "RoomTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingRooms",
                table: "BookingRooms",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_PropertyId",
                table: "Rooms",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_guests_property_id_email",
                table: "guests",
                columns: new[] { "property_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_property_id",
                table: "bookings",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_PropertyId",
                table: "RoomTypes",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRooms_BookingId",
                table: "BookingRooms",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRooms_RoomTypes_RoomTypeId",
                table: "BookingRooms",
                column: "RoomTypeId",
                principalTable: "RoomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRooms_Rooms_RoomId",
                table: "BookingRooms",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRooms_bookings_BookingId",
                table: "BookingRooms",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_RoomTypes_RoomTypeId",
                table: "Rooms",
                column: "RoomTypeId",
                principalTable: "RoomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_properties_PropertyId",
                table: "Rooms",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomTypes_properties_PropertyId",
                table: "RoomTypes",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
