﻿CREATE TABLE [dbo].[values]
(
	[Id] BIGINT IDENTITY(1,1) NOT NULL,
	[NamespaceId] BIGINT NOT NULL,
	[KeyId] BIGINT NOT NULL,
	[Value] sQL_VARIANT NOT NULL,
	[Timestamp] DATETIME NOT NULL,
	CONSTRAINT [PK_VALUES] PRIMARY KEY CLUSTERED
	(
		[Id] DESC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 1) ON [PRIMARY]

) ON [PRIMARY]
GO

ALTER TABLE [dbo].[values]  WITH CHECK ADD  CONSTRAINT [FK_values_namespaces] FOREIGN KEY([NamespaceId])
REFERENCES [dbo].[namespaces] ([Id])
GO

ALTER TABLE [dbo].[values]  WITH CHECK ADD  CONSTRAINT [FK_values_keys] FOREIGN KEY([KeyId])
REFERENCES [dbo].[keys] ([Id])
GO

ALTER TABLE [dbo].[values] CHECK CONSTRAINT [FK_values_namespaces]
GO

ALTER TABLE [dbo].[values] CHECK CONSTRAINT [FK_values_keys]
GO

CREATE NONCLUSTERED INDEX IX_values_NamespaceId ON [dbo].[values] ([NamespaceId]);
GO

CREATE NONCLUSTERED INDEX IX_values_KeyId ON [dbo].[values] ([KeyId]);
GO

CREATE NONCLUSTERED INDEX IX_values_Timestamp ON [dbo].[values] ([Timestamp]);
GO