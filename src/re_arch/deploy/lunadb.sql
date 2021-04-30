/****** Object:  Schema [partner]    Script Date: 4/29/2021 11:07:14 AM ******/
CREATE SCHEMA [partner]
GO
/****** Object:  Schema [publish]    Script Date: 4/29/2021 11:07:14 AM ******/
CREATE SCHEMA [publish]
GO
/****** Object:  Schema [rbac]    Script Date: 4/29/2021 11:07:14 AM ******/
CREATE SCHEMA [rbac]
GO
/****** Object:  Schema [routing]    Script Date: 4/29/2021 11:07:14 AM ******/
CREATE SCHEMA [routing]
GO
/****** Object:  Table [partner].[PartnerServices]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [partner].[PartnerServices](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[uniqueName] [nvarchar](128) NOT NULL,
	[displayName] [nvarchar](128) NOT NULL,
	[type] [nvarchar](128) NOT NULL,
	[description] [nvarchar](1024) NOT NULL,
	[configurationSecretName] [nvarchar](64) NOT NULL,
	[tags] [nvarchar](1024) NOT NULL,
	[createdTime] [datetime2](7) NOT NULL,
	[lastUpdatedTime] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [routing].[PartnerServices]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [routing].[PartnerServices]
AS
SELECT uniqueName, configurationSecretName, id, type
FROM   partner.PartnerServices
GO
/****** Object:  Table [publish].[ApplicationSnapshots]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [publish].[ApplicationSnapshots](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[snapshotId] [uniqueidentifier] NOT NULL,
	[lastAppliedEventId] [bigint] NOT NULL,
	[applicationName] [nvarchar](128) NOT NULL,
	[snapshotContent] [nvarchar](max) NOT NULL,
	[status] [nvarchar](64) NOT NULL,
	[tags] [nvarchar](1024) NOT NULL,
	[createdTime] [datetime2](7) NOT NULL,
	[deletedTime] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [publish].[LunaAPIs]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [publish].[LunaAPIs](
	[applicationName] [nvarchar](128) NOT NULL,
	[apiName] [nvarchar](128) NOT NULL,
	[apiType] [nvarchar](64) NOT NULL,
	[createdTime] [datetime2](7) NOT NULL,
	[lastUpdatedTime] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[applicationName] ASC,
	[apiName] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [publish].[LunaAPIVersions]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [publish].[LunaAPIVersions](
	[applicationName] [nvarchar](128) NOT NULL,
	[apiName] [nvarchar](128) NOT NULL,
	[versionName] [nvarchar](128) NOT NULL,
	[versionType] [nvarchar](64) NOT NULL,
	[createdTime] [datetime2](7) NOT NULL,
	[lastUpdatedTime] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[applicationName] ASC,
	[apiName] ASC,
	[versionName] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [publish].[LunaApplications]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [publish].[LunaApplications](
	[applicationName] [nvarchar](128) NOT NULL,
	[status] [nvarchar](64) NOT NULL,
	[createdTime] [datetime2](7) NOT NULL,
	[lastUpdatedTime] [datetime2](7) NULL,
	[lastPublishedTime] [datetime2](7) NULL,
	[PrimaryMasterKeySecretName] [nvarchar](64) NOT NULL,
	[SecondaryMasterKeySecretName] [nvarchar](64) NOT NULL,
	[OwnerUserId] [nvarchar](256) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[applicationName] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [publish].[PublishingEvents]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [publish].[PublishingEvents](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[eventId] [uniqueidentifier] NOT NULL,
	[eventType] [nvarchar](64) NOT NULL,
	[resourceName] [nvarchar](128) NOT NULL,
	[eventContent] [nvarchar](max) NOT NULL,
	[createdBy] [nvarchar](128) NOT NULL,
	[tags] [nvarchar](1024) NOT NULL,
	[createdTime] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [rbac].[ownerships]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [rbac].[ownerships](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[uid] [nvarchar](128) NOT NULL,
	[resourceId] [nvarchar](1024) NOT NULL,
	[createdTime] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [rbac].[roleassignments]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [rbac].[roleassignments](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[uid] [nvarchar](128) NOT NULL,
	[userName] [nvarchar](128) NULL,
	[role] [nvarchar](64) NOT NULL,
	[createdTime] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [routing].[PublishedAPIVersions]    Script Date: 4/29/2021 11:07:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [routing].[PublishedAPIVersions](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ApplicationName] [nvarchar](128) NULL,
	[APIName] [nvarchar](128) NULL,
	[APIType] [nvarchar](64) NULL,
	[VersionName] [nvarchar](128) NULL,
	[VersionType] [nvarchar](64) NULL,
	[VersionProperties] [nvarchar](max) NULL,
	[LastAppliedEventId] [bigint] NULL,
	[PrimaryMasterKeySecretName] [nvarchar](64) NULL,
	[SecondaryMasterKeySecretName] [nvarchar](64) NULL,
	[IsEnabled] [bit] NULL,
	[CreatedTime] [datetime2](7) NULL,
	[LastUpdatedTime] [datetime2](7) NULL,
 CONSTRAINT [PK_routing.PublishedAPIVersions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

Declare @adminUserId nvarchar(128)
Declare @adminUserName nvarchar(128)
Declare @createdDate datetime2(7)

SET @adminUserId = '$(adminUserId)'
SET @adminUserName = '$(adminUserName)'
SET @createdDate = GETUTCDATE()

INSERT INTO [rbac].[roleassignments] VALUES (@adminUserId, @adminUserName, 'SystemAdmin', @createdDate)
GO
