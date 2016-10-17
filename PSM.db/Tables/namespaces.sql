﻿CREATE TABLE [dbo].[namespaces]
(
	[Id] BIGINT IDENTITY(1,1) NOT NULL,
	[Namespace] VARCHAR(1000) NOT NULL,
	CONSTRAINT [PK_NAMESPACES] PRIMARY KEY CLUSTERED
	(
		[Id] DESC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 1) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[namespaces] WITH CHECK ADD CONSTRAINT u_Namespace UNIQUE ([Namespace])
GO

CREATE UNIQUE INDEX IX_namespaces_Namespace ON [dbo].[namespaces] ([Namespace]);
GO