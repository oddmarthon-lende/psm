CREATE PROCEDURE [dbo].[usp_delete_path]
	@Path VARCHAR(MAX)
AS

	declare @namespaceId bigint, @namespace varchar(max), @keyName varchar(max);
	
	declare c cursor for
		select * from [namespaces] where [Namespace] = @Path or [Namespace] like @Path + '.%';

	declare @i int, @c varchar(max), @key varchar(max), @e int, @p varchar(max);

	select @i = -1, @namespace = @Path, @key = @namespace, @e = 0;

	while(@c is null or LEN(@c) > 0)
	begin
	
		select @i = CHARINDEX('.', @key) + 1;
		set @e = @e + @i - 1;
		select @c = SUBSTRING(@key, 0, @i - 1);
		select @key = SUBSTRING(@key, @i, Len(@key));

		if(LEN(@c) = 0)
		begin
			select @namespace = SUBSTRING(@namespace, 0, @e);
		end
	end

	exec usp_delete_key @namespace, @key;

	open c;
	fetch next from c into @namespaceId, @namespace;

	while(@@FETCH_STATUS = 0)
	begin
	
		declare c2 cursor for
			select [Name] from [keys] where [NamespaceId] = @namespaceId;
			
		open c2;
		fetch next from c2 into @keyName;

		while(@@FETCH_STATUS = 0)
		begin
			exec usp_delete_key @namespace, @keyName;
			fetch next from c2 into @keyName;
		end

		close c2;
		deallocate c2;

		delete from [namespaces] where [Id] = @namespaceId;
		
		fetch next from c into @namespaceId, @namespace;

	end

	close c;
	deallocate c;
	
RETURN 0
