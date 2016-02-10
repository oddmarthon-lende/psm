CREATE PROCEDURE [dbo].[usp_get_meta]
	@Namespace VARCHAR(MAX)
AS
	SELECT [Name], [Value] from [meta] where [NamespaceId] = (select [Id] from [namespaces] where [Namespace] = @Namespace);
RETURN 0
