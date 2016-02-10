CREATE PROCEDURE [dbo].[usp_insert_meta]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Value SQL_VARIANT
AS 

	declare @nsid bigint,  @metaid bigint, @keyid bigint;

	select @nsid = [Id] from [namespaces] where [Namespace] = @Namespace;
	
	if(@nsid is null)
	begin
		insert into [namespaces] ([Namespace]) values (@Namespace) ;
		select @nsid = [Id] from [namespaces] where [Namespace] = @Namespace;
	end

	select @keyid = [Id] from [keys] where [Name] = @Key and [NamespaceId] = @nsid;

	if(@keyid is null)
	begin
		insert into [keys] ([Name], [NamespaceId], [Visible]) values (@Key, @nsid, 0);
		select @keyid = [Id] from [keys] where [Name] = @Key and [NamespaceId] = @nsid;
	end
		
	select @metaid = [Id] from [meta] where [NamespaceId] = @nsid and [KeyId] = @keyid;

	if(@metaid is null)
	begin
		insert into [meta] ([KeyId], [NamespaceId], [Value]) values (@keyid, @nsid, @Value);
		select @metaid = [Id] from [meta] where [NamespaceId] = @nsid and [KeyId] = @keyid;
	end
	else
	begin
		update [meta] set [Value] = @Value where [Id] = @metaid;
	end	

RETURN 0
