IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [IsActive] bit NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Branch] (
    [Id] uniqueidentifier NOT NULL,
    [BranchName] nvarchar(100) NOT NULL,
    [BranchCode] char(1) NOT NULL,
    CONSTRAINT [PK__Branch__54205B04058BBB4F] PRIMARY KEY ([Id])
);

CREATE TABLE [ErrorLogs] (
    [Id] uniqueidentifier NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [StackTrace] nvarchar(max) NULL,
    [Source] nvarchar(255) NULL,
    [LoggedAt] datetime NOT NULL DEFAULT ((getutcdate())),
    CONSTRAINT [PK__ErrorLog__3214EC07A2529995] PRIMARY KEY ([Id])
);

CREATE TABLE [Gender] (
    [Id] uniqueidentifier NOT NULL,
    [TheType] nvarchar(7) NOT NULL,
    CONSTRAINT [PK__Gender__3214EC070A22819D] PRIMARY KEY ([Id])
);

CREATE TABLE [ProfileImage] (
    [Id] uniqueidentifier NOT NULL,
    [UserName] nvarchar(100) NULL,
    [Email] nvarchar(100) NULL,
    [ProfileImagePath] nvarchar(200) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK__ProfileI__3214EC07AD185F4E] PRIMARY KEY ([Id])
);

CREATE TABLE [StageClass] (
    [Id] uniqueidentifier NOT NULL,
    [Code] char(1) NOT NULL,
    [MinClass] int NOT NULL,
    [MaxClass] int NOT NULL,
    [NameStage] nvarchar(15) NOT NULL,
    CONSTRAINT [PK__StageCla__3214EC07B7F14D02] PRIMARY KEY ([Id])
);

CREATE TABLE [StatusSchool] (
    [Id] uniqueidentifier NOT NULL,
    [condition] bit NULL,
    [TheType] nvarchar(20) NULL,
    CONSTRAINT [PK__StatusSc__3214EC073DB2CBA3] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [School] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [IdStatusSchool] uniqueidentifier NULL,
    [IdGender] uniqueidentifier NULL,
    [IsDeleted] bit NOT NULL,
    [MinClass] int NULL,
    [MaxClass] int NULL,
    [IdStage] uniqueidentifier NULL,
    CONSTRAINT [PK__School__3214EC07F11AFDBA] PRIMARY KEY ([Id]),
    CONSTRAINT [FK__School__IdGender__28B808A7] FOREIGN KEY ([IdGender]) REFERENCES [Gender] ([Id]),
    CONSTRAINT [FK__School__IdStage__377B294A] FOREIGN KEY ([IdStage]) REFERENCES [StageClass] ([Id]),
    CONSTRAINT [FK__School__IdStatus__22FF2F51] FOREIGN KEY ([IdStatusSchool]) REFERENCES [StatusSchool] ([Id])
);

CREATE TABLE [Lectuer] (
    [id] uniqueidentifier NOT NULL,
    [Name] varchar(50) NOT NULL,
    [IdSchool] uniqueidentifier NULL,
    [IsDeleted] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    CONSTRAINT [PK__Lectuer__3213E83FBA6843F5] PRIMARY KEY ([id]),
    CONSTRAINT [FK__Lectuer__IdSchoo__6C6E1476] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id])
);

CREATE TABLE [Menegar] (
    [id] uniqueidentifier NOT NULL,
    [ApplicationUserId] uniqueidentifier NULL,
    [Name] nvarchar(100) NULL,
    [Phone] varchar(50) NULL,
    [Email] varchar(50) NULL,
    [IdSchool] uniqueidentifier NULL,
    [TheDate] date NULL,
    [City] nvarchar(50) NULL,
    [Area] nvarchar(50) NULL,
    [IdNumber] int NULL,
    [IsDeleted] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    CONSTRAINT [PK__Menegar__3213E83FE96BFA1F] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Menegar_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK__Menegar__IdSchoo__23F3538A] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id])
);

CREATE TABLE [Teacher] (
    [id] uniqueidentifier NOT NULL,
    [ApplicationUserId] uniqueidentifier NULL,
    [Name] nvarchar(100) NULL,
    [Phone] varchar(50) NULL,
    [Email] varchar(50) NULL,
    [IdSchool] uniqueidentifier NULL,
    [TheDate] date NULL,
    [City] nvarchar(50) NULL,
    [Area] nvarchar(50) NULL,
    [IdNumber] int NULL,
    [IsDeleted] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    CONSTRAINT [PK__Teacher__3213E83F92AB32EF] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Teacher_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK__Teacher__IdSchoo__25DB9BFC] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id])
);

CREATE TABLE [TheClass] (
    [id] uniqueidentifier NOT NULL,
    [Name] nvarchar(20) NOT NULL,
    [IdSchool] uniqueidentifier NULL,
    [IsDeleted] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    [IdStage] uniqueidentifier NULL,
    [NumberClass] int NULL,
    [Section] int NULL,
    [IdBranch] uniqueidentifier NULL,
    CONSTRAINT [PK__TheClass__3213E83FD60CD186] PRIMARY KEY ([id]),
    CONSTRAINT [FK_TheClass_School] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id]),
    CONSTRAINT [FK__TheClass__IdBran__3592E0D8] FOREIGN KEY ([IdBranch]) REFERENCES [Branch] ([Id]),
    CONSTRAINT [FK__TheClass__IdStag__32B6742D] FOREIGN KEY ([IdStage]) REFERENCES [StageClass] ([Id])
);

CREATE TABLE [Student] (
    [id] uniqueidentifier NOT NULL,
    [ApplicationUserId] uniqueidentifier NULL,
    [Name] nvarchar(100) NULL,
    [Phone] varchar(50) NULL,
    [Email] varchar(50) NULL,
    [IdSchool] uniqueidentifier NULL,
    [TheDate] date NULL,
    [IdClass] uniqueidentifier NULL,
    [City] nvarchar(50) NULL,
    [Area] nvarchar(50) NULL,
    [IdNumber] int NULL,
    [IsDeletedStudent] bit NOT NULL,
    [IsDeletedClass] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    CONSTRAINT [PK__Student__3213E83F6F20DDDC] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Student_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK__Student__IdClass__1A1FD08D] FOREIGN KEY ([IdClass]) REFERENCES [TheClass] ([id]),
    CONSTRAINT [FK__Student__IdSchoo__24E777C3] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id])
);

CREATE TABLE [TeacherLectuerClass] (
    [id] uniqueidentifier NOT NULL,
    [IdTeacher] uniqueidentifier NULL,
    [IdLectuer] uniqueidentifier NULL,
    [IdSchool] uniqueidentifier NULL,
    [IdClass] uniqueidentifier NULL,
    [IsDeletedTeacherLectuerClass] bit NOT NULL,
    [IsDeletedClass] bit NOT NULL,
    [IsDeletedLectuer] bit NOT NULL,
    [IsDeletedTeacher] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    [IsTeacherRemovedFromClass] bit NOT NULL,
    [IsTeacherRemovedFromLectuer] bit NOT NULL,
    CONSTRAINT [PK__TeacherL__3213E83F5B2FD59A] PRIMARY KEY ([id]),
    CONSTRAINT [FK_TeacherLectuer_Lectuer] FOREIGN KEY ([IdLectuer]) REFERENCES [Lectuer] ([id]),
    CONSTRAINT [FK_TeacherLectuer_Teacher] FOREIGN KEY ([IdTeacher]) REFERENCES [Teacher] ([id]),
    CONSTRAINT [FK__TeacherLe__IdCla__74EE4BDE] FOREIGN KEY ([IdClass]) REFERENCES [TheClass] ([id]),
    CONSTRAINT [FK__TeacherLe__IdSch__7D2E8C24] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id])
);

CREATE TABLE [Attendance] (
    [id] uniqueidentifier NOT NULL,
    [AttendanceStatus] char(1) NOT NULL DEFAULT '0',
    [DateAndTime] date NULL,
    [Excuse] text NULL,
    [IdTeacher] uniqueidentifier NULL,
    [IdLectuer] uniqueidentifier NULL,
    [IdStudent] uniqueidentifier NULL,
    [IdClass] uniqueidentifier NULL,
    [IdSchool] uniqueidentifier NULL,
    [IsDeletedAttendance] bit NOT NULL,
    [IsDeletedClass] bit NOT NULL,
    [IsDeletedTeacher] bit NOT NULL,
    [IsDeletedStudent] bit NOT NULL,
    [IsDeletedLectuer] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    [IsTeacherRemovedFromClass] bit NOT NULL,
    [IsTeacherRemovedFromLectuer] bit NOT NULL,
    CONSTRAINT [PK__Attendan__3213E83FAD8350D2] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Attendance_Lectuer] FOREIGN KEY ([IdLectuer]) REFERENCES [Lectuer] ([id]),
    CONSTRAINT [FK_Attendance_School] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id]),
    CONSTRAINT [FK_Attendance_Student] FOREIGN KEY ([IdStudent]) REFERENCES [Student] ([id]),
    CONSTRAINT [FK_Attendance_Teacher] FOREIGN KEY ([IdTeacher]) REFERENCES [Teacher] ([id]),
    CONSTRAINT [FK_Attendance_TheClass] FOREIGN KEY ([IdClass]) REFERENCES [TheClass] ([id])
);

CREATE TABLE [Grades] (
    [GradesID] uniqueidentifier NOT NULL,
    [FirstMonth] int NULL DEFAULT 0,
    [Mid] int NULL DEFAULT 0,
    [SecondMonth] int NULL DEFAULT 0,
    [Activity] int NULL DEFAULT 0,
    [Final] int NULL DEFAULT 0,
    [IdStudent] uniqueidentifier NULL,
    [IdTeacher] uniqueidentifier NULL,
    [IdLectuer] uniqueidentifier NULL,
    [IdClass] uniqueidentifier NULL,
    [Total] AS (((([FirstMonth]+[Mid])+[SecondMonth])+[Activity])+[Final]),
    [IdSchool] uniqueidentifier NULL,
    [IsDeletedGrades] bit NOT NULL,
    [IsDeletedClass] bit NOT NULL,
    [IsDeletedLectuer] bit NOT NULL,
    [IsDeletedStudent] bit NOT NULL,
    [IsDeletedTeacher] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    [IsTeacherRemovedFromClass] bit NOT NULL,
    [IsTeacherRemovedFromLectuer] bit NOT NULL,
    CONSTRAINT [PK__Grades__931A40BF88D8CDCA] PRIMARY KEY ([GradesID]),
    CONSTRAINT [FK_Grades_Lectuer] FOREIGN KEY ([IdLectuer]) REFERENCES [Lectuer] ([id]),
    CONSTRAINT [FK_Grades_Student] FOREIGN KEY ([IdStudent]) REFERENCES [Student] ([id]),
    CONSTRAINT [FK_Grades_Teacher] FOREIGN KEY ([IdTeacher]) REFERENCES [Teacher] ([id]),
    CONSTRAINT [FK__Grades__IdClass__0D0FEE32] FOREIGN KEY ([IdClass]) REFERENCES [TheClass] ([id]),
    CONSTRAINT [FK__Grades__IdSchool__6D6238AF] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id])
);

CREATE TABLE [StudentLectuerTeacher] (
    [id] uniqueidentifier NOT NULL,
    [IdStudent] uniqueidentifier NULL,
    [IdLectuer] uniqueidentifier NULL,
    [IdSchool] uniqueidentifier NULL,
    [IdClass] uniqueidentifier NULL,
    [IdTeacher] uniqueidentifier NULL,
    [IsDeletedStudentLectuerTeacher] bit NOT NULL,
    [IsDeletedClass] bit NOT NULL,
    [IsDeletedStudent] bit NOT NULL,
    [IsDeletedTeacher] bit NOT NULL,
    [IsDeletedSchool] bit NOT NULL,
    [IsDeletedLectuer] bit NOT NULL,
    [IsTeacherRemovedFromClass] bit NOT NULL,
    [IsTeacherRemovedFromLectuer] bit NOT NULL,
    CONSTRAINT [PK__StudentL__3213E83F373E3AFF] PRIMARY KEY ([id]),
    CONSTRAINT [FK_StudentLectuer_Lectuer] FOREIGN KEY ([IdLectuer]) REFERENCES [Lectuer] ([id]),
    CONSTRAINT [FK_StudentLectuer_School] FOREIGN KEY ([IdSchool]) REFERENCES [School] ([Id]),
    CONSTRAINT [FK_StudentLectuer_Student] FOREIGN KEY ([IdStudent]) REFERENCES [Student] ([id]),
    CONSTRAINT [FK__StudentLe__IdCla__08F5448B] FOREIGN KEY ([IdClass]) REFERENCES [TheClass] ([id]),
    CONSTRAINT [FK__StudentLe__IdTea__09E968C4] FOREIGN KEY ([IdTeacher]) REFERENCES [Teacher] ([id])
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_Attendance_IdClass] ON [Attendance] ([IdClass]);

CREATE INDEX [IX_Attendance_IdLectuer] ON [Attendance] ([IdLectuer]);

CREATE INDEX [IX_Attendance_IdSchool] ON [Attendance] ([IdSchool]);

CREATE INDEX [IX_Attendance_IdStudent] ON [Attendance] ([IdStudent]);

CREATE INDEX [IX_Attendance_IdTeacher] ON [Attendance] ([IdTeacher]);

CREATE INDEX [IX_Grades_Id] ON [Grades] ([GradesID]);

CREATE INDEX [IX_Grades_IdClass] ON [Grades] ([IdClass]);

CREATE INDEX [IX_Grades_IdLectuer] ON [Grades] ([IdLectuer]);

CREATE INDEX [IX_Grades_IdSchool] ON [Grades] ([IdSchool]);

CREATE INDEX [IX_Grades_IdStudent] ON [Grades] ([IdStudent]);

CREATE INDEX [IX_Grades_IdTeacher] ON [Grades] ([IdTeacher]);

CREATE INDEX [IX_Lectuer_IdSchool] ON [Lectuer] ([IdSchool]);

CREATE INDEX [IX_Lectuer_Name] ON [Lectuer] ([Name]);

CREATE UNIQUE INDEX [IX_Menegar_ApplicationUserId] ON [Menegar] ([ApplicationUserId]) WHERE [ApplicationUserId] IS NOT NULL;

CREATE INDEX [IX_Menegar_IdSchool] ON [Menegar] ([IdSchool]);

CREATE INDEX [IX_Menegar_Name] ON [Menegar] ([Name]);

CREATE UNIQUE INDEX [UQ_Menegar_IdNumber] ON [Menegar] ([IdNumber]) WHERE [IdNumber] IS NOT NULL;

CREATE INDEX [IX_School_IdGender] ON [School] ([IdGender]);

CREATE INDEX [IX_School_IdStage] ON [School] ([IdStage]);

CREATE INDEX [IX_School_IdStatusSchool] ON [School] ([IdStatusSchool]);

CREATE UNIQUE INDEX [UQ__StageCla__A25C5AA7AAECF2F1] ON [StageClass] ([Code]);

CREATE UNIQUE INDEX [IX_Student_ApplicationUserId] ON [Student] ([ApplicationUserId]) WHERE [ApplicationUserId] IS NOT NULL;

CREATE INDEX [IX_Student_IdClass] ON [Student] ([IdClass]);

CREATE INDEX [IX_Student_IdSchool] ON [Student] ([IdSchool]);

CREATE INDEX [IX_Student_Name] ON [Student] ([Name]);

CREATE INDEX [IX_StudentLectuer_IdSchool] ON [StudentLectuerTeacher] ([IdSchool]);

CREATE INDEX [IX_StudentLectuerTeacher_IdClass] ON [StudentLectuerTeacher] ([IdClass]);

CREATE INDEX [IX_StudentLectuerTeacher_IdLectuer] ON [StudentLectuerTeacher] ([IdLectuer]);

CREATE INDEX [IX_StudentLectuerTeacher_IdStudent] ON [StudentLectuerTeacher] ([IdStudent]);

CREATE INDEX [IX_StudentLectuerTeacher_IdTeacher] ON [StudentLectuerTeacher] ([IdTeacher]);

CREATE UNIQUE INDEX [IX_Teacher_ApplicationUserId] ON [Teacher] ([ApplicationUserId]) WHERE [ApplicationUserId] IS NOT NULL;

CREATE INDEX [IX_Teacher_IdSchool] ON [Teacher] ([IdSchool]);

CREATE INDEX [IX_Teacher_Name] ON [Teacher] ([Name]);

CREATE UNIQUE INDEX [UQ_Teacher_IdNumber] ON [Teacher] ([IdNumber]) WHERE [IdNumber] IS NOT NULL;

CREATE INDEX [IX_TeacherLectuer_IdSchool] ON [TeacherLectuerClass] ([IdSchool]);

CREATE INDEX [IX_TeacherLectuerClass_IdClass] ON [TeacherLectuerClass] ([IdClass]);

CREATE INDEX [IX_TeacherLectuerClass_IdLectuer] ON [TeacherLectuerClass] ([IdLectuer]);

CREATE INDEX [IX_TeacherLectuerClass_IdTeacher] ON [TeacherLectuerClass] ([IdTeacher]);

CREATE INDEX [IX_TheClass_IdBranch] ON [TheClass] ([IdBranch]);

CREATE INDEX [IX_TheClass_IdSchool] ON [TheClass] ([IdSchool]);

CREATE INDEX [IX_TheClass_IdStage] ON [TheClass] ([IdStage]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260811105639_InitialGuidIdentity', N'9.0.2');

COMMIT;
GO

