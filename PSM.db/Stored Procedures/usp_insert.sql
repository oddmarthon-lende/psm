﻿CREATE PROCEDURE [dbo].[usp_insert_value]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Value SQL_VARIANT,
	@Timestamp DATETIME = null
AS 
	declare @nsid bigint, @keyid bigint, @basetype varchar(100), @q nvarchar(max);

	if(@Value is null)
	begin
		return 0;
	end

	select @nsid = [Id] from [namespaces] where [Namespace] = @Namespace;

	if(@nsid is null)
	begin
		insert into [namespaces] ([Namespace]) values (@Namespace) ;
		select @nsid = [Id] from [namespaces] where [Namespace] = @Namespace;
	end

	select @keyid = [Id], @basetype = [Type] from [keys] where [Name] = @Key and [NamespaceId] = @nsid;

	if(@keyid is null)
	begin
		select @basetype = cast(SQL_VARIANT_PROPERTY(@Value,'BaseType') as varchar(100));
		insert into [keys] ([Name], [Type], [NamespaceId]) values (@Key, @basetype, @nsid);
		select @keyid = [Id] from [keys] where [Name] = @Key and [NamespaceId] = @nsid;
	end
	else
	begin

		if(SQL_VARIANT_PROPERTY(@Value, 'BaseType') != @basetype)
		begin
			if(@basetype is null)
			begin
				select @basetype = cast(SQL_VARIANT_PROPERTY(@Value,'BaseType') as varchar(100));
				update [keys] set [Type] = @basetype;
			end
			else
			begin
				;throw 50000, 'Invalid data type for this key, a key can only have one type. To change the data type for this key, first delete the key and all associated data.', 1;
			end
		end

	end

	exec usp_create_table @basetype;

	set @q = N'insert into [tbl_'+@basetype+'] ([KeyId], [Value], [Timestamp]) output inserted.[Id], inserted.[KeyId], inserted.[Value], inserted.[Timestamp] values (@keyid, convert(' + @basetype + (case when @basetype like '%var%' then '(max)' else '' end) + ', @Value), coalesce(@Timestamp, GETDATE()));';

	exec sp_executesql
		@q,
		N'@keyid bigint, @Value sql_variant, @Timestamp datetime',
		@keyid = @keyid, @Value = @Value, @Timestamp = @Timestamp;

RETURN 0
