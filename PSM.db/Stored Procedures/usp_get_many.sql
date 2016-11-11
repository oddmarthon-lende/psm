
CREATE PROCEDURE [dbo].[usp_get_many]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Start SQL_VARIANT = null,
	@End SQL_VARIANT = null,
	@IndexColumn VARCHAR(100) = 'Index'
AS
	
	declare @q NVARCHAR(MAX), @basetype NVARCHAR(100), @keyid bigint;

	select @basetype = [Type], @keyid = [Id] from [keys] where [Name] = @Key and [NamespaceId] = (select [Id] from [namespaces] where [Namespace] = @Namespace);

	set @q = ';with p as
				  (
					select [Id] from [tbl_' + @basetype + ']
						where [KeyId] = @keyid ';

	if(@basetype is null)
	begin
		return 0;
	end
	else if(@IndexColumn = 'Descending')
	begin		

		if(@End is not null)
		begin
			set @q = @q + 'order by [Id] desc
						offset cast(@Start as bigint) rows
						fetch next (cast(@End as bigint) - cast(@Start as bigint) + 1) rows only ';
		end
						
		set @q = @q + ')
				  select p.Id as [Index], p.Id, t.Value, t.Timestamp from [tbl_' + @basetype + '] t
						inner join p on (t.Id = p.Id) ';

		if(@End is null)
		begin
			set @q = @q + 'where  p.Id > @Start ';
		end


		set @q = @q + 'order by t.[Id] desc;';
						

		

	end
	else
	begin

		set @q = @q + ')
				select t.[' + @IndexColumn + '] as [Index], p.[Id], t.[Value], t.[Timestamp] from [tbl_' + @basetype + '] t
					inner join p on (t.Id = p.Id)  
					where [KeyId] = @keyid and (t.[' + @IndexColumn + '] ';
		
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

			set @q = @q + ' and t.[' + @IndexColumn + '] <= @End)'

		end

		set @q = @q + ' order by t.[' + @IndexColumn + '] desc';
		
	end

	exec sp_executesql @q, N'@keyid bigint, @Start sql_variant, @End sql_variant', @keyid = @keyid, @Start = @Start, @End = @End;

RETURN 0
GO