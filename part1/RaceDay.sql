/*
 RaceDayDB - Part 1
 SQL Server database script
 NOTE: Organisers and Participants are separate tables as required.
*/

IF DB_ID('RaceDayDB') IS NULL
    CREATE DATABASE RaceDayDB;
GO
USE RaceDayDB;
GO

IF OBJECT_ID('dbo.Results','U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrollments','U') IS NOT NULL DROP TABLE dbo.Enrollments;
IF OBJECT_ID('dbo.Events','U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Routes','U') IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID('dbo.Categories','U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Participants','U') IS NOT NULL DROP TABLE dbo.Participants;
IF OBJECT_ID('dbo.Organisers','U') IS NOT NULL DROP TABLE dbo.Organisers;
GO

CREATE TABLE dbo.Organisers (
    OrganiserID INT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(120) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(20) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Organisers PRIMARY KEY (OrganiserID),
    CONSTRAINT UQ_Organisers_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.Participants (
    ParticipantID INT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(120) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(20) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Participants PRIMARY KEY (ParticipantID),
    CONSTRAINT UQ_Participants_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.Categories (
    CategoryID INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(80) NOT NULL,
    Description NVARCHAR(255) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT UQ_Categories_Name UNIQUE (CategoryName)
);
GO

CREATE TABLE dbo.Routes (
    RouteID INT IDENTITY(1,1) NOT NULL,
    RouteName NVARCHAR(100) NOT NULL,
    StartLocation NVARCHAR(150) NOT NULL,
    FinishLocation NVARCHAR(150) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    ElevationGainM INT NOT NULL DEFAULT 0,
    CONSTRAINT PK_Routes PRIMARY KEY (RouteID),
    CONSTRAINT CK_Routes_Distance CHECK (DistanceKm > 0),
    CONSTRAINT CK_Routes_Elevation CHECK (ElevationGainM >= 0)
);
GO

CREATE TABLE dbo.Events (
    EventID INT IDENTITY(1,1) NOT NULL,
    OrganizerID INT NOT NULL,
    CategoryID INT NOT NULL,
    RouteID INT NOT NULL,
    EventName NVARCHAR(120) NOT NULL,
    EventDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    Capacity INT NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Open',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (EventID),
    CONSTRAINT CK_Events_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_Events_Status CHECK (Status IN ('Open','Closed','Cancelled')),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganizerID)
        REFERENCES dbo.Organisers(OrganiserID),
    CONSTRAINT FK_Events_Category FOREIGN KEY (CategoryID)
        REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT FK_Events_Route FOREIGN KEY (RouteID)
        REFERENCES dbo.Routes(RouteID)
);
GO

CREATE TABLE dbo.Enrollments (
    EnrollmentID INT IDENTITY(1,1) NOT NULL,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    EnrollmentDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    BibNumber INT NULL,
    CONSTRAINT PK_Enrollments PRIMARY KEY (EnrollmentID),
    CONSTRAINT UQ_Enrollments_ParticipantEvent UNIQUE (ParticipantID, EventID),
    CONSTRAINT UQ_Enrollments_BibNumber UNIQUE (BibNumber),
    CONSTRAINT CK_Enrollments_Status CHECK (Status IN ('Active','Cancelled','Completed')),
    CONSTRAINT CK_Enrollments_Bib CHECK (BibNumber IS NULL OR BibNumber > 0),
    CONSTRAINT FK_Enrollments_Participant FOREIGN KEY (ParticipantID)
        REFERENCES dbo.Participants(ParticipantID),
    CONSTRAINT FK_Enrollments_Event FOREIGN KEY (EventID)
        REFERENCES dbo.Events(EventID)
);
GO

CREATE TABLE dbo.Results (
    ResultID INT IDENTITY(1,1) NOT NULL,
    EnrollmentID INT NOT NULL,
    FinishTime TIME NOT NULL,
    Position INT NOT NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultID),
    CONSTRAINT UQ_Results_Enrollment UNIQUE (EnrollmentID),
    CONSTRAINT CK_Results_Position CHECK (Position > 0),
    CONSTRAINT FK_Results_Enrollment FOREIGN KEY (EnrollmentID)
        REFERENCES dbo.Enrollments(EnrollmentID)
);
GO

INSERT INTO dbo.Organisers (FirstName, LastName, Email, PasswordHash, Phone)
VALUES
('Thabo','Mokoena','thabo.organiser@raceday.test','HASHED_PASSWORD_1','0710000001'),
('Lerato','Dlamini','lerato.organiser@raceday.test','HASHED_PASSWORD_2','0710000002');
GO

INSERT INTO dbo.Participants (FirstName, LastName, Email, PasswordHash, Phone)
VALUES
('Anele','Mokoena','anele.participant@raceday.test','HASHED_PASSWORD_3','0720000001'),
('Sipho','Nkosi','sipho.participant@raceday.test','HASHED_PASSWORD_4','0720000002');
GO

INSERT INTO dbo.Categories (CategoryName, Description)
VALUES
('5 KM Fun Run','Short community running event'),
('10 KM Road Race','Competitive 10 kilometre road race'),
('21 KM Half Marathon','Half marathon road running event');
GO

INSERT INTO dbo.Routes (RouteName, StartLocation, FinishLocation, DistanceKm, ElevationGainM)
VALUES
('Pretoria Central 5K','Church Square','Church Square',5.00,45),
('Pretoria East 10K','Menlyn Park','Menlyn Park',10.00,120),
('Tshwane Half Marathon','Union Buildings','Union Buildings',21.10,210);
GO

INSERT INTO dbo.Events
(OrganizerID, CategoryID, RouteID, EventName, EventDate, StartTime, Location, Capacity, Status)
VALUES
(1,1,1,'Pretoria Spring Fun Run','2026-10-10','07:00','Pretoria Central',500,'Open'),
(2,2,2,'Tshwane 10K Road Race','2026-10-24','06:30','Pretoria East',800,'Open'),
(1,3,3,'Capital City Half Marathon','2026-11-14','06:00','Union Buildings',1000,'Open');
GO

INSERT INTO dbo.Enrollments
(ParticipantID, EventID, EnrollmentDate, Status, BibNumber)
VALUES
(1,1,'2026-09-01T10:00:00','Active',101),
(1,2,'2026-09-01T10:15:00','Active',102),
(2,2,'2026-09-01T11:00:00','Active',103),
(2,3,'2026-09-01T11:20:00','Active',104);
GO

INSERT INTO dbo.Results (EnrollmentID, FinishTime, Position)
VALUES
(1,'00:31:42',25),
(2,'01:02:34',12);
GO

SELECT * FROM dbo.Organisers;
SELECT * FROM dbo.Participants;
SELECT * FROM dbo.Categories;
SELECT * FROM dbo.Routes;
SELECT * FROM dbo.Events;
SELECT * FROM dbo.Enrollments;
SELECT * FROM dbo.Results;
GO
