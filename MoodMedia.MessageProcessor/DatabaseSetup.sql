-- Create the database
CREATE DATABASE MoodMedia;
GO

USE MoodMedia;
GO

-- Create Company table
CREATE TABLE Company (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Licensing INT NOT NULL
);

-- Create Location table
CREATE TABLE Location (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Address NVARCHAR(MAX) NOT NULL,
    ParentId INT NOT NULL,
    CONSTRAINT FK_Location_Company FOREIGN KEY (ParentId) REFERENCES Company(Id)
);

-- Create Device table
CREATE TABLE Device (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SerialNumber NVARCHAR(255) NOT NULL UNIQUE,
    Type INT NOT NULL,
    LocationId INT NOT NULL,
    CONSTRAINT FK_Device_Location FOREIGN KEY (LocationId) REFERENCES Location(Id)
);

-- Create indexes
CREATE INDEX IX_Location_ParentId ON Location(ParentId);
CREATE INDEX IX_Device_LocationId ON Device(LocationId);
CREATE INDEX IX_Device_SerialNumber ON Device(SerialNumber); 