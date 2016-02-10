CREATE PROCEDURE [dbo].[usp_get_meta]
	@Namespace VARCHAR(MAX)
AS
	SELECT [Id], [Key], [Value] from metadata where [Namespace] = @Namespace;
RETURN 0
