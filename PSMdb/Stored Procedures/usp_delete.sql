CREATE PROCEDURE [dbo].[usp_delete]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Start SQL_VARIANT = null,
	@End SQL_VARIANT = null,
	@Span BIGINT = null,
	@IndexColumn VARCHAR(MAX) = null
AS

	declare @basetype nvarchar(100), @q nvarchar(max);

	select @basetype = [Type] from [keys] where [Name] = @Key and [NamespaceId] = (select [Id] from [namespaces] where [Namespace] = @Namespace);

	if(@basetype is null)
	begin
		return 0;
	end	
	else if(@Span is not null)
	begin

		set @q = N'select top 1 @End = [Timestamp] from [' + @basetype + '] order by [Timestamp] desc;';
		exec sp_executesql @q, N'@End datetime output', @End output;

		set @End   = COALESCE(@End, GETDATE());
		set @Start = DATEADD(SECOND, 0 - @Span, @End);

	end

	if(@Start is null or @End is null)
	begin
		set @q = N'delete from [' + @basetype + '] where [NamespaceId] in (select Id from [namespaces] where [Namespace] = @Namespace) and [KeyId] in (select Id from [keys] where [Name] = @Key);';
		exec sp_executesql @q, N'@Namespace varchar(max), @Key varchar(max)', @Namespace = @Namespace,  @Key = @Key;
	end
	else
	begin
		set @q = N'delete from [' + @basetype + '] where [NamespaceId] in (select Id from [namespaces] where [Namespace] = @Namespace) and [KeyId] in (select Id from [keys] where [Name] = @Key) and [' + @IndexColumn + '] >= @Start and [' + @IndexColumn + '] <= @End;';
		exec sp_executesql @q, N'@Namespace varchar(max), @Key varchar(max), @Start datetime, @End datetime', @Namespace = @Namespace,  @Key = @Key, @Start = @Start, @End = @End;
	end

	

RETURN 0
