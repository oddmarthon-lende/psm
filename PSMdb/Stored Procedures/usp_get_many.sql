CREATE PROCEDURE [dbo].[usp_get_many]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Start SQL_VARIANT = null,
	@End SQL_VARIANT = null,
	@IndexColumn VARCHAR(100) = 'Index'
AS
	
	declare @q NVARCHAR(MAX);

	if(@IndexColumn = 'Index')
	begin
		
		set @q = 'select [Id], [Value], [Timestamp], (RowNumber - 1) as [Index] from (select ROW_NUMBER() over (order by [Timestamp] desc) as RowNumber, *
			from data where [Namespace] = @Namespace and [Key] = @Key) as Result
			where (RowNumber - 1) >= @Start and (RowNumber - 1) <= @End order by RowNumber;';

	end
	else
	begin

		set @q = 'select [Id], [Value], [Timestamp], [' + @IndexColumn + '] as [Index] from data where [Namespace] = @Namespace and [Key] = @Key and ([' + @IndexColumn + '] >= @Start and [' + @IndexColumn + '] <= @End) order by [' + @IndexColumn + '] desc;';

	end

	exec sp_executesql @q, N'@Namespace varchar(max), @Key varchar(max), @Start sql_variant, @End sql_variant', @Namespace = @Namespace, @Key = @Key, @Start = @Start, @End = @End;

RETURN 0
