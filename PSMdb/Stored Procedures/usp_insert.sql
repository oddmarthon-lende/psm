CREATE PROCEDURE [dbo].[usp_insert_value]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Value SQL_VARIANT,
	@Timestamp DATETIME = null
AS 
	declare @nsid bigint, @keyid bigint, @basetype varchar(100);

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

		if(SQL_VARIANT_PROPERTY(@Value,'BaseType') != @basetype)
		begin
			;throw 50000, 'Invalid data type for this key, a key can only have one type. To change the data type for this key, first delete the key and all associated data.', 1;
		end

	end

	insert into [values] ([NamespaceId], [KeyId], [Value], [Timestamp]) values (@nsid, @keyid, @Value, coalesce(@Timestamp, GETDATE()));

RETURN 0
