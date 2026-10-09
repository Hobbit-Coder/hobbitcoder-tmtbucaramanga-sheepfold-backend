using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class MinistryMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TodoItems",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "TodoLists",
                schema: "Sheepfold");

            migrationBuilder.CreateTable(
                name: "Attendees",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Instagram = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EducationalStage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IsServer = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reference = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Before = table.Column<string>(type: "text", nullable: true),
                    After = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventTypes",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpTopics",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Generations",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Profile = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationChannelSettings",
                schema: "Sheepfold",
                columns: table => new
                {
                    Channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationChannelSettings", x => x.Channel);
                });

            migrationBuilder.CreateTable(
                name: "NotificationKindSettings",
                schema: "Sheepfold",
                columns: table => new
                {
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationKindSettings", x => x.Kind);
                });

            migrationBuilder.CreateTable(
                name: "OperationalSettings",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LowAttendanceThresholdPercent = table.Column<int>(type: "integer", nullable: true),
                    LowAttendanceWindowDays = table.Column<int>(type: "integer", nullable: true),
                    FrequentLatenessThreshold = table.Column<int>(type: "integer", nullable: true),
                    FrequentLatenessWindowDays = table.Column<int>(type: "integer", nullable: true),
                    UpcomingBirthdaysDays = table.Column<int>(type: "integer", nullable: true),
                    UpcomingEventNoticeHours = table.Column<int>(type: "integer", nullable: true),
                    PendingFollowUpReminderDays = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendeeContacts",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendeeContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendeeContacts_Attendees_AttendeeId",
                        column: x => x.AttendeeId,
                        principalSchema: "Sheepfold",
                        principalTable: "Attendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendeeCourses",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FinishedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendeeCourses", x => x.Id);
                    table.CheckConstraint("CK_AttendeeCourses_FinishedOn", "\"FinishedOn\" IS NULL OR \"Status\" = 'Finished'");
                    table.ForeignKey(
                        name: "FK_AttendeeCourses_Attendees_AttendeeId",
                        column: x => x.AttendeeId,
                        principalSchema: "Sheepfold",
                        principalTable: "Attendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendeeCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "Sheepfold",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EventTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Audience = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RequiresAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.CheckConstraint("CK_Events_Period", "\"EndsAt\" >= \"StartsAt\"");
                    table.ForeignKey(
                        name: "FK_Events_EventTypes_EventTypeId",
                        column: x => x.EventTypeId,
                        principalSchema: "Sheepfold",
                        principalTable: "EventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FollowUps",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectAttendeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ScheduledTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    MeetingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Commitments = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Observations = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsConfidential = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUps", x => x.Id);
                    table.CheckConstraint("CK_FollowUps_Done", "\"Status\" <> 'Done' OR (\"MeetingDate\" IS NOT NULL AND \"Location\" IS NOT NULL AND \"TopicId\" IS NOT NULL AND \"Description\" IS NOT NULL AND \"Commitments\" IS NOT NULL)");
                    table.CheckConstraint("CK_FollowUps_ScheduledTime", "\"ScheduledTime\" IS NULL OR \"ScheduledDate\" IS NOT NULL");
                    table.CheckConstraint("CK_FollowUps_Subject", "(\"SubjectAttendeeId\" IS NULL AND \"SubjectUserId\" IS NOT NULL) OR (\"SubjectAttendeeId\" IS NOT NULL AND \"SubjectUserId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_FollowUps_Attendees_SubjectAttendeeId",
                        column: x => x.SubjectAttendeeId,
                        principalSchema: "Sheepfold",
                        principalTable: "Attendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FollowUps_FollowUpTopics_TopicId",
                        column: x => x.TopicId,
                        principalSchema: "Sheepfold",
                        principalTable: "FollowUpTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    GenerationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Generations_GenerationId",
                        column: x => x.GenerationId,
                        principalSchema: "Sheepfold",
                        principalTable: "Generations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendeeAttendances",
                schema: "Sheepfold",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mark = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendeeAttendances", x => new { x.EventId, x.AttendeeId });
                    table.ForeignKey(
                        name: "FK_AttendeeAttendances_Attendees_AttendeeId",
                        column: x => x.AttendeeId,
                        principalSchema: "Sheepfold",
                        principalTable: "Attendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendeeAttendances_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "Sheepfold",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventAttachments",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAttachments_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "Sheepfold",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventComments",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventComments_EventComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalSchema: "Sheepfold",
                        principalTable: "EventComments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventComments_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "Sheepfold",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventGenerations",
                schema: "Sheepfold",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenerationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventGenerations", x => new { x.EventId, x.GenerationId });
                    table.ForeignKey(
                        name: "FK_EventGenerations_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "Sheepfold",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventGenerations_Generations_GenerationId",
                        column: x => x.GenerationId,
                        principalSchema: "Sheepfold",
                        principalTable: "Generations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventManagers",
                schema: "Sheepfold",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventManagers", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EventManagers_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "Sheepfold",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttendeeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Attendees_AttendeeId",
                        column: x => x.AttendeeId,
                        principalSchema: "Sheepfold",
                        principalTable: "Attendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "Sheepfold",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAttendances",
                schema: "Sheepfold",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mark = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAttendances", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserAttendances_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "Sheepfold",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventGroups",
                schema: "Sheepfold",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventGroups", x => new { x.EventId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_EventGroups_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "Sheepfold",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Sheepfold",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupAssignments",
                schema: "Sheepfold",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Responsibility = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupAssignments", x => new { x.GroupId, x.UserId, x.Responsibility });
                    table.ForeignKey(
                        name: "FK_GroupAssignments_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Sheepfold",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMemberships",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMemberships", x => x.Id);
                    table.CheckConstraint("CK_GroupMemberships_Period", "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\"");
                    table.ForeignKey(
                        name: "FK_GroupMemberships_Attendees_AttendeeId",
                        column: x => x.AttendeeId,
                        principalSchema: "Sheepfold",
                        principalTable: "Attendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMemberships_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Sheepfold",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventCommentAttachments",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCommentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventCommentAttachments_EventComments_CommentId",
                        column: x => x.CommentId,
                        principalSchema: "Sheepfold",
                        principalTable: "EventComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Sheepfold",
                table: "Courses",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "Name", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("0199c2a0-0000-7000-8000-000000000301"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "ADN", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000302"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Llaves del Poder", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000303"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Madurez Espiritual", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000304"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "IBLI", null, null }
                });

            migrationBuilder.InsertData(
                schema: "Sheepfold",
                table: "EventTypes",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "Name", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("0199c2a0-0000-7000-8000-000000000201"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Reunión general de los sábados", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000202"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Estudio TMT", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000203"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Reunión de planeación", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000204"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Entrenamiento de servidores", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000205"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Reunión de coordinadores", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000206"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Reunión de servidores", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000207"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Campamento", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000208"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Salida", null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000209"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "Actividad con venta de boletas", null, null }
                });

            migrationBuilder.InsertData(
                schema: "Sheepfold",
                table: "Generations",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "Name", "Profile", "SortOrder", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("0199c2a0-0000-7000-8000-000000000101"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "PULSO", "Pulso", 1, null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000102"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "ECO", "Eco", 2, null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000103"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "LEGADO 1-5", "Legado1To5", 3, null, null },
                    { new Guid("0199c2a0-0000-7000-8000-000000000104"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), true, "LEGADO 6-10", "Legado6To10", 4, null, null }
                });

            migrationBuilder.InsertData(
                schema: "Sheepfold",
                table: "NotificationChannelSettings",
                columns: new[] { "Channel", "IsEnabled" },
                values: new object[,]
                {
                    { "InApp", true },
                    { "WhatsApp", false }
                });

            migrationBuilder.InsertData(
                schema: "Sheepfold",
                table: "NotificationKindSettings",
                columns: new[] { "Kind", "IsEnabled" },
                values: new object[,]
                {
                    { "AttendeeAssigned", true },
                    { "Birthday", true },
                    { "DiscussionActivity", true },
                    { "EventCancelled", true },
                    { "EventChanged", true },
                    { "EventResponsibility", true },
                    { "PendingFollowUp", true },
                    { "UpcomingEvent", true }
                });

            migrationBuilder.InsertData(
                schema: "Sheepfold",
                table: "OperationalSettings",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "FrequentLatenessThreshold", "FrequentLatenessWindowDays", "LowAttendanceThresholdPercent", "LowAttendanceWindowDays", "PendingFollowUpReminderDays", "UpcomingBirthdaysDays", "UpcomingEventNoticeHours", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), "System", new DateTime(2026, 10, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AttendeeAttendances_AttendeeId",
                schema: "Sheepfold",
                table: "AttendeeAttendances",
                column: "AttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendeeContacts_AttendeeId",
                schema: "Sheepfold",
                table: "AttendeeContacts",
                column: "AttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendeeCourses_AttendeeId",
                schema: "Sheepfold",
                table: "AttendeeCourses",
                column: "AttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendeeCourses_CourseId",
                schema: "Sheepfold",
                table: "AttendeeCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendees_LastName_FirstName",
                schema: "Sheepfold",
                table: "Attendees",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EntityId",
                schema: "Sheepfold",
                table: "AuditEntries",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Module_OccurredAt",
                schema: "Sheepfold",
                table: "AuditEntries",
                columns: new[] { "Module", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OccurredAt",
                schema: "Sheepfold",
                table: "AuditEntries",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Name",
                schema: "Sheepfold",
                table: "Courses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttachments_EventId",
                schema: "Sheepfold",
                table: "EventAttachments",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttachments_ObjectKey",
                schema: "Sheepfold",
                table: "EventAttachments",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventCommentAttachments_CommentId",
                schema: "Sheepfold",
                table: "EventCommentAttachments",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventCommentAttachments_ObjectKey",
                schema: "Sheepfold",
                table: "EventCommentAttachments",
                column: "ObjectKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_EventId_CreatedDate",
                schema: "Sheepfold",
                table: "EventComments",
                columns: new[] { "EventId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_ParentCommentId",
                schema: "Sheepfold",
                table: "EventComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventGenerations_GenerationId",
                schema: "Sheepfold",
                table: "EventGenerations",
                column: "GenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventGroups_GroupId",
                schema: "Sheepfold",
                table: "EventGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EventManagers_UserId",
                schema: "Sheepfold",
                table: "EventManagers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedBy",
                schema: "Sheepfold",
                table: "Events",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventTypeId",
                schema: "Sheepfold",
                table: "Events",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartsAt_EndsAt",
                schema: "Sheepfold",
                table: "Events",
                columns: new[] { "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_Name",
                schema: "Sheepfold",
                table: "EventTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_CreatedBy",
                schema: "Sheepfold",
                table: "FollowUps",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_SubjectAttendeeId",
                schema: "Sheepfold",
                table: "FollowUps",
                column: "SubjectAttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_SubjectUserId",
                schema: "Sheepfold",
                table: "FollowUps",
                column: "SubjectUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_TopicId",
                schema: "Sheepfold",
                table: "FollowUps",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpTopics_Name",
                schema: "Sheepfold",
                table: "FollowUpTopics",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Generations_Name",
                schema: "Sheepfold",
                table: "Generations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupAssignments_UserId",
                schema: "Sheepfold",
                table: "GroupAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_AttendeeId_Current",
                schema: "Sheepfold",
                table: "GroupMemberships",
                column: "AttendeeId",
                unique: true,
                filter: "\"EndedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_AttendeeId_StartedAt",
                schema: "Sheepfold",
                table: "GroupMemberships",
                columns: new[] { "AttendeeId", "StartedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_GroupId",
                schema: "Sheepfold",
                table: "GroupMemberships",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_GenerationId",
                schema: "Sheepfold",
                table: "Groups",
                column: "GenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_IsActive",
                schema: "Sheepfold",
                table: "Groups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Year_GenerationId_Name",
                schema: "Sheepfold",
                table: "Groups",
                columns: new[] { "Year", "GenerationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AttendeeId",
                schema: "Sheepfold",
                table: "Notifications",
                column: "AttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EventId",
                schema: "Sheepfold",
                table: "Notifications",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_CreatedAt",
                schema: "Sheepfold",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAttendances_UserId",
                schema: "Sheepfold",
                table: "UserAttendances",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendeeAttendances",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "AttendeeContacts",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "AttendeeCourses",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "AuditEntries",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "EventAttachments",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "EventCommentAttachments",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "EventGenerations",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "EventGroups",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "EventManagers",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "FollowUps",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "GroupAssignments",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "GroupMemberships",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "NotificationChannelSettings",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "NotificationKindSettings",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "OperationalSettings",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "UserAttendances",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "Courses",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "EventComments",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "FollowUpTopics",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "Attendees",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "Generations",
                schema: "Sheepfold");

            migrationBuilder.DropTable(
                name: "EventTypes",
                schema: "Sheepfold");

            migrationBuilder.CreateTable(
                name: "TodoLists",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoItems",
                schema: "Sheepfold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TodoListId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoItems_TodoLists_TodoListId",
                        column: x => x.TodoListId,
                        principalSchema: "Sheepfold",
                        principalTable: "TodoLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoListId",
                schema: "Sheepfold",
                table: "TodoItems",
                column: "TodoListId");
        }
    }
}
