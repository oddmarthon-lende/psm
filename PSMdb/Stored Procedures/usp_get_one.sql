CREATE PROCEDURE [dbo].[usp_get_one]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX)
AS
	
	declare @basetype NVARCHAR(100), @q NVARCHAR(max);

	select @basetype = [Type] from [keys] where [Name] = @Key and [NamespaceId] = (select [Id] from [namespaces] where [Namespace] = @Namespace);

	if(@basetype is not null)
	begin

		exec usp_create_view @basetype;
		
		set @q = N'select top 1 [Value], [Timestamp], 0 as Id from [' + @basetype + '_data] where [Namespace] = @Namespace and [Key] = @Key order by [Timestamp] desc;';

		exec sp_executesql @q, N'@Namespace varchar(max), @Key varchar(max)', @Namespace = @Namespace, @Key = @Key;

	end

RETURN 0
