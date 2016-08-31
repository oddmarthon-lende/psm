
CREATE PROCEDURE [dbo].[usp_get_many]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Start SQL_VARIANT = null,
	@End SQL_VARIANT = null,
	@IndexColumn VARCHAR(100) = 'Index'
AS
	
	declare @q NVARCHAR(MAX), @basetype NVARCHAR(100), @keyid bigint;

	select @basetype = [Type], @keyid = [Id] from [keys] where [Name] = @Key and [NamespaceId] = (select [Id] from [namespaces] where [Namespace] = @Namespace);

	if(@basetype is null)
	begin
		return 0;
	end
	else if(@IndexColumn = 'Index')
	begin
		
		set @q = 'select  ([RowNumber_Reverse] - 1) as [Index], [Value], [Timestamp] from (select ROW_NUMBER() over (order by [Timestamp] desc) as RowNumber, ROW_NUMBER() over (order by [Timestamp] asc) as [RowNumber_Reverse], *
			from [tbl_' + @basetype + '] where [KeyId] = @keyid) as Result
			where ';

		if(@End is null)
		begin
			set @q = @q + '([RowNumber_Reverse] - 1)';
		end
		else
		begin
			set @q = @q + '(RowNumber - 1)';
		end

		set @q = @q + ' >';

		if(@End is not null)
		begin
			set @q = @q + '=';
		end

		set @q = @q + ' @Start';

		if(@End is not null)
		begin
			set @q = @q + ' and (RowNumber - 1) <= @End';
		end

		set @q = @q + ' order by RowNumber';

		

	end
	else
	begin

		set @q = 'select [' + @IndexColumn + '] as [Index], [Value], [Timestamp] from [tbl_' + @basetype + '] where [KeyId] = @keyid and ([' + @IndexColumn + '] ';
		
		set @q = @q + ' >';

		if(@End is not null)
		begin
			set @q = @q + '=';
		end

		set @q = @q + ' @Start';

		if(@End is null)
		begin
			set @q = @q + ')';
		end

		if(@End is not null)
		begin

			set @q = @q + ' and [' + @IndexColumn + '] <= @End)'

		end

		set @q = @q + ' order by [' + @IndexColumn + '] desc';
		print @q;
	end

	exec sp_executesql @q, N'@keyid bigint, @Start sql_variant, @End sql_variant', @keyid = @keyid, @Start = @Start, @End = @End;

RETURN 0
GO