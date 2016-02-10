CREATE PROCEDURE [dbo].[usp_insert_meta]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX),
	@Value SQL_VARIANT
AS 

	declare @nsid bigint,  @metaid bigint;

	select @nsid = [Id] from [namespaces] where [Namespace] = @Namespace;

	if(@nsid is null)
	begin
		insert into [namespaces] ([Namespace]) values (@Namespace) ;
		select @nsid = [Id] from [namespaces] where [Namespace] = @Namespace;
	end
		
	select @metaid = [Id] from [meta] where [NamespaceId] = @nsid and [Name] = @Key;

	if(@metaid is null)
	begin
		insert into [meta] ([Name], [NamespaceId], [Value]) values (@Key, @nsid, @Value);
		select @metaid = [Id] from [meta] where [NamespaceId] = @nsid and [Name] = @Key;
	end
	else
	begin
		update [meta] set [Value] = @Value where [Id] = @metaid;
	end	

RETURN 0
