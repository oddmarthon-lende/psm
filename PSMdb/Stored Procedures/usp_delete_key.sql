CREATE PROCEDURE [dbo].[usp_delete_key]
	@Namespace VARCHAR(MAX),
	@Key VARCHAR(MAX)
AS

	declare @basetype nvarchar(100), @q nvarchar(max), @namespaceId bigint, @keyId bigint;

	select @namespaceId = [Id] from [namespaces] where [Namespace] = @Namespace;
	select @basetype = [Type], @keyId = [Id] from [keys] where [Name] = @Key and [NamespaceId] = @namespaceId;

	if(@basetype is null)
	begin
		return 0;
	end

	set @q = N'delete from [tbl_' + @basetype + '] where [KeyId] = @keyId;';
	exec sp_executesql @q, N'@keyId bigint', @keyId = @keyId;
	set @q = N'delete from [keys] where [Id] = @keyId;';
	exec sp_executesql @q, N'@keyId bigint', @keyId = @keyId;	

RETURN 0
