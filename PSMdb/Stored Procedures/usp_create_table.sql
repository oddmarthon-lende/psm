CREATE PROCEDURE [dbo].[usp_create_table]
	@basetype nvarchar(100)
AS

	if(object_id('tbl_'+@basetype, N'table') is null)
	begin
	
		declare @q nvarchar(max);
		
		set @q = N'CREATE TABLE [dbo].[tbl_'+@basetype+']
				(
					[Id] BIGINT IDENTITY(1,1) NOT NULL,
					[NamespaceId] BIGINT NOT NULL,
					[KeyId] BIGINT NOT NULL,
					[Value] ' + @basetype + (case when @basetype like '%var%' then '(max)' else '' end) + ' NOT NULL,
					[Timestamp] DATETIME NOT NULL,
					CONSTRAINT [PK_'+upper(@basetype)+'] PRIMARY KEY CLUSTERED
					(
						[Id] DESC
					) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON, FILLFACTOR = 1) ON [PRIMARY]

				) ON [PRIMARY];

				ALTER TABLE [dbo].[tbl_'+@basetype+']  WITH CHECK ADD  CONSTRAINT [FK_'+@basetype+'_namespaces] FOREIGN KEY([NamespaceId])
				REFERENCES [dbo].[namespaces] ([Id]);
				
				ALTER TABLE [dbo].[tbl_'+@basetype+']  WITH CHECK ADD  CONSTRAINT [FK_'+@basetype+'_keys] FOREIGN KEY([KeyId])
				REFERENCES [dbo].[keys] ([Id]);
				
				ALTER TABLE [dbo].[tbl_'+@basetype+'] CHECK CONSTRAINT [FK_'+@basetype+'_namespaces];
				
				ALTER TABLE [dbo].[tbl_'+@basetype+'] CHECK CONSTRAINT [FK_'+@basetype+'_keys];
				
				CREATE NONCLUSTERED INDEX IX_'+@basetype+'_NamespaceId ON [dbo].[tbl_'+@basetype+'] ([NamespaceId]);
				
				CREATE NONCLUSTERED INDEX IX_'+@basetype+'_KeyId ON [dbo].[tbl_'+@basetype+'] ([KeyId]);

				CREATE NONCLUSTERED INDEX IX_'+@basetype+'_Timestamp ON [dbo].[tbl_'+@basetype+'] ([Timestamp]);' +

				(case when @basetype like '%var%' then '' when @basetype like '%text' then '' when @basetype = 'image' then '' when @basetype = 'xml' then '' else 'CREATE NONCLUSTERED INDEX IX_'+@basetype+'_Value ON [dbo].[tbl_'+@basetype+'] ([Value]);' end);

		exec sp_executesql @q;

	end

RETURN 0
GO


