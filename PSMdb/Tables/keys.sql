﻿CREATE TABLE [dbo].[keys]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
	[Name] VARCHAR(100) NOT NULL,
	[Type] VARCHAR(100) NOT NULL,
	[NamespaceId] BIGINT NOT NULL,	 
	CONSTRAINT [PK_KEYS] PRIMARY KEY CLUSTERED
	(
		[Id] DESC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 1) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[keys]  WITH CHECK ADD  CONSTRAINT [FK_namespaces_keys] FOREIGN KEY([NamespaceId])
REFERENCES [dbo].[namespaces] ([Id])
GO

ALTER TABLE [dbo].[keys] CHECK CONSTRAINT [FK_namespaces_keys]
GO

ALTER TABLE [dbo].[keys] WITH CHECK ADD CONSTRAINT u_KeyName UNIQUE ([Name])
GO

CREATE UNIQUE INDEX IX_keys_Name ON [dbo].[keys] ([Name]);
GO
