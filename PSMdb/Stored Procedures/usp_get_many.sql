CREATE PROCEDURE [dbo].[usp_get_many]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Start SQL_VARIANT = null,
	@End SQL_VARIANT = null
AS
	
	if(SQL_VARIANT_PROPERTY(@Start,'BaseType') != 'datetime')
	begin
		
		select [Value], [Timestamp] from (select ROW_NUMBER() over (order by [Timestamp] desc) as RowNumber, *
			from data where [Namespace] = @Namespace and [Key] = @Key) as Result
			where (RowNumber - 1) >= cast(@Start as bigint) and (RowNumber - 1) <= cast(@End as bigint) order by RowNumber;

	end
	else
	begin

		select [Value], [Timestamp] from data where [Namespace] = @Namespace and [Key] = @Key and [Timestamp] >= cast(@Start as datetime) and [Timestamp] <= cast(@End as datetime) order by [Timestamp] desc;

	end

RETURN 0
