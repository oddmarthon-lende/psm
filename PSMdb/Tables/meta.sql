﻿CREATE TABLE [dbo].[meta]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
	[Name] VARCHAR(100) NOT NULL,
	[Value] sQL_VARIANT NOT NULL,
	[NamespaceId] BIGINT NOT NULL,
	CONSTRAINT [PK_META] PRIMARY KEY CLUSTERED
	(
		[Id] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 1) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE INDEX IX_meta_Name ON [dbo].[meta] ([Name]);
GO
