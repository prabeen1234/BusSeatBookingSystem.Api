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
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [Gender] int NOT NULL,
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
GO

CREATE TABLE [BusDetails] (
    [BusId] uniqueidentifier NOT NULL,
    [BusName] nvarchar(max) NOT NULL,
    [BusNumber] nvarchar(max) NOT NULL,
    [BusType] nvarchar(max) NOT NULL,
    [BusRoute] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_BusDetails] PRIMARY KEY ([BusId])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260426043117_CreationofAppUser', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusDetails]') AND [c].[name] = N'BusType');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [BusDetails] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [BusDetails] ALTER COLUMN [BusType] nvarchar(50) NOT NULL;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusDetails]') AND [c].[name] = N'BusNumber');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [BusDetails] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [BusDetails] ALTER COLUMN [BusNumber] nvarchar(50) NOT NULL;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusDetails]') AND [c].[name] = N'BusName');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [BusDetails] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [BusDetails] ALTER COLUMN [BusName] nvarchar(120) NOT NULL;
GO

ALTER TABLE [BusDetails] ADD [ArrivalDateTime] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [BusDetails] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [BusDetails] ADD [DepartureDateTime] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [BusDetails] ADD [Fare] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [BusDetails] ADD [ImagePath] nvarchar(500) NULL;
GO

ALTER TABLE [BusDetails] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [BusDetails] ADD [RouteId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
GO

ALTER TABLE [BusDetails] ADD [TotalSeats] int NOT NULL DEFAULT 0;
GO

CREATE TABLE [Bookings] (
    [BookingId] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [BusId] uniqueidentifier NOT NULL,
    [BookingNumber] nvarchar(40) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [ReservedUntilUtc] datetime2 NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ConfirmedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([BookingId]),
    CONSTRAINT [FK_Bookings_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bookings_BusDetails_BusId] FOREIGN KEY ([BusId]) REFERENCES [BusDetails] ([BusId]) ON DELETE CASCADE
);
GO

CREATE TABLE [BusRoutes] (
    [RouteId] uniqueidentifier NOT NULL,
    [Source] nvarchar(100) NOT NULL,
    [Destination] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_BusRoutes] PRIMARY KEY ([RouteId])
);
GO

CREATE TABLE [BusSeats] (
    [SeatId] uniqueidentifier NOT NULL,
    [BusId] uniqueidentifier NOT NULL,
    [SeatNumber] nvarchar(10) NOT NULL,
    [Status] int NOT NULL,
    [ReservedUntilUtc] datetime2 NULL,
    CONSTRAINT [PK_BusSeats] PRIMARY KEY ([SeatId]),
    CONSTRAINT [FK_BusSeats_BusDetails_BusId] FOREIGN KEY ([BusId]) REFERENCES [BusDetails] ([BusId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Payments] (
    [PaymentId] uniqueidentifier NOT NULL,
    [BookingId] uniqueidentifier NOT NULL,
    [TransactionUuid] nvarchar(80) NOT NULL,
    [EsewaReferenceId] nvarchar(80) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [VerifiedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE
);
GO

CREATE TABLE [BookingSeats] (
    [BookingId] uniqueidentifier NOT NULL,
    [SeatId] uniqueidentifier NOT NULL,
    [Fare] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_BookingSeats] PRIMARY KEY ([BookingId], [SeatId]),
    CONSTRAINT [FK_BookingSeats_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookingSeats_BusSeats_SeatId] FOREIGN KEY ([SeatId]) REFERENCES [BusSeats] ([SeatId]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [IX_BusDetails_BusNumber] ON [BusDetails] ([BusNumber]);
GO

CREATE INDEX [IX_BusDetails_RouteId] ON [BusDetails] ([RouteId]);
GO

CREATE UNIQUE INDEX [IX_Bookings_BookingNumber] ON [Bookings] ([BookingNumber]);
GO

CREATE INDEX [IX_Bookings_BusId] ON [Bookings] ([BusId]);
GO

CREATE INDEX [IX_Bookings_UserId] ON [Bookings] ([UserId]);
GO

CREATE INDEX [IX_BookingSeats_SeatId] ON [BookingSeats] ([SeatId]);
GO

CREATE UNIQUE INDEX [IX_BusSeats_BusId_SeatNumber] ON [BusSeats] ([BusId], [SeatNumber]);
GO

CREATE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);
GO

CREATE UNIQUE INDEX [IX_Payments_TransactionUuid] ON [Payments] ([TransactionUuid]);
GO


                INSERT INTO BusRoutes (RouteId, Source, Destination, Description, IsActive, CreatedAtUtc)
                SELECT NEWID(), LEFT(BusRoute, 100), 'Unknown', 'Migrated from legacy BusRoute value', 1, SYSUTCDATETIME()
                FROM BusDetails
                GROUP BY BusRoute;

                UPDATE b
                SET b.RouteId = r.RouteId, b.IsActive = 1
                FROM BusDetails b
                INNER JOIN BusRoutes r ON r.Source = LEFT(b.BusRoute, 100) AND r.Destination = 'Unknown';
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusDetails]') AND [c].[name] = N'BusRoute');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [BusDetails] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [BusDetails] DROP COLUMN [BusRoute];
GO

ALTER TABLE [BusDetails] ADD CONSTRAINT [FK_BusDetails_BusRoutes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [BusRoutes] ([RouteId]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260625120803_AddBusBookingAndEsewa', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260625124530_update', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [PasswordOtps] (
    [PasswordOtpId] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [Purpose] int NOT NULL,
    [CodeHash] nvarchar(128) NOT NULL,
    [Salt] nvarchar(64) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [UsedAtUtc] datetime2 NULL,
    [FailedAttempts] int NOT NULL,
    CONSTRAINT [PK_PasswordOtps] PRIMARY KEY ([PasswordOtpId]),
    CONSTRAINT [FK_PasswordOtps_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PasswordOtps_UserId_Purpose_CreatedAtUtc] ON [PasswordOtps] ([UserId], [Purpose], [CreatedAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260625153158_AddPasswordOtpSecurity', N'8.0.26');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Bookings] ADD [PassengerEmail] nvarchar(256) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Bookings] ADD [PassengerName] nvarchar(120) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Bookings] ADD [PassengerPhone] nvarchar(20) NOT NULL DEFAULT N'';
GO

UPDATE b
SET b.PassengerName = LTRIM(RTRIM(CONCAT(u.FirstName, ' ', u.LastName))),
    b.PassengerEmail = COALESCE(u.Email, ''),
    b.PassengerPhone = COALESCE(NULLIF(u.PhoneNumber, ''), 'Not provided')
FROM Bookings b
INNER JOIN AspNetUsers u ON u.Id = b.UserId;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260625154613_AddBookingPassengerDetails', N'8.0.26');
GO

COMMIT;
GO

